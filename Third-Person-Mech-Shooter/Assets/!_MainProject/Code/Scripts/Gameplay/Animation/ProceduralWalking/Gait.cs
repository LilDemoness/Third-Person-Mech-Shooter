using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay.Animations.ProceduralAnimations
{
    [System.Serializable]
    public class Gait
    {
        private MechBody _body;

        // Note: Keep an eye on the memory usage of these dictionaries, in case they start getting too high.
        // Leg Indicies point to the array '_body.Legs'.
        private Dictionary<int, int> _legIndiciesToGroupIndicies = new();   // Leg Index : Group Index.
        private Dictionary<int, List<int>> _legGroups = new();      // Group Index : Indicies of Legs in Group.
        private Dictionary<int, List<int>> _adjacentLegs = new();   // Group Index : Indicies of Legs in Adjacent Groups.


        [SerializeField] public GaitSettings Settings;


        public void Init(MechBody body)
        {
            _body = body;
            UpdateValues();
        }


        public List<MechLeg> GetLegsInGroup(int groupIndex)
        {
            List<MechLeg> legsToReturn = new List<MechLeg>(_legGroups[groupIndex].Count);
            foreach(int legIndex in _legGroups[groupIndex])
                legsToReturn.Add(_body.Legs[legIndex]);

            return legsToReturn;
        }
        public List<MechLeg> GetLegsInGroup(int groupIndex, params MechLeg[] excluding)
        {
            List<MechLeg> legsToReturn = new List<MechLeg>(_legGroups[groupIndex].Count - excluding.Length);
            foreach(int legIndex in _legGroups[groupIndex])
            {
                if (excluding.Any(excluded => _body.Legs[legIndex] == excluded))
                    continue;   // This leg is an excluded one.

                legsToReturn.Add(_body.Legs[legIndex]);
            }

            return legsToReturn;
        }
        public List<MechLeg> GetLegsInAdjacentGroups(int groupIndex) 
        {
            List<MechLeg> legsToReturn = new List<MechLeg>(_adjacentLegs[groupIndex].Count);
            foreach(int legIndex in _adjacentLegs[groupIndex])
                legsToReturn.Add(_body.Legs[legIndex]);

            return legsToReturn;
        }
        public int GetLegGroupIndex(int legIndex) => _legIndiciesToGroupIndicies[legIndex];


        public bool CanMoveLeg(MechLeg leg)
        {
            if (!_body.TryGetLegIndex(leg, out int legIndex))
            {
                Debug.LogError($"Leg (Target: {leg.IKTargetTransform.name}) was not found in the MechBody: {_body.name}");
                return false;
            }
            int groupIndex = GetLegGroupIndex(legIndex);

            // If the leg's target is off the ground, we can always move it.
            if (!leg.Target.IsGrounded)
                return true;

            // This leg is being tested.
            leg.IsPrimary = true;

            // We can only move if all adjacent pairs are: Grounded, Disabled, or Not targeting the Ground.
            List<MechLeg> crossPair = GetLegsInAdjacentGroups(groupIndex);
            if (crossPair.Any(leg2 => !leg2.IsGrounded() && !leg2.IsDisabled && leg2.Target.IsGrounded))
                return false;   // An adjacent pair is moving, enabled, and targeting the ground.


            // Check for leg movement cooldowns.
            if (crossPair.Any(leg2 => leg2.Target.IsGrounded && leg2.TimeSinceLastMoveCompleted < Settings.AdjacentPairCooldown))
                return false;   // On Cooldown: Blocking leg moved too recently.
            List<MechLeg> samePair = GetLegsInGroup(groupIndex, excluding: leg);
            if (samePair.Any(leg2 => leg2.Target.IsGrounded && leg2.TimeSinceLastMoveStarted < Settings.SamePairCooldown))
                return false;   // On Cooldown: Paired leg started too recently.


            // Final Checks.
            bool wantsToMove = leg.IsOutsideTriggerZone || !leg.IsTouchingGround;
            bool alreadyAtTarget = (leg.IKTargetPosition - leg.Target.Position).sqrMagnitude < 0.01f;
            bool onGround = _body.Legs.Any(bodyLeg => bodyLeg.IsGrounded()) || _body.IsGrounded;
            return wantsToMove && !alreadyAtTarget && onGround;
        }


        public List<GaitLegInfo> LegSetupInfo = new();
        public List<GaitGroupInfo> GroupSetupInfo = new();


        public void UpdateValues()
        {
            // Populate leg info.
            _legIndiciesToGroupIndicies = new();
            _legGroups = new();
            foreach(GaitLegInfo legInfo in LegSetupInfo)
            {
                // Add to the [leg index : group index] dict.
                _legIndiciesToGroupIndicies.Add(legInfo.LegIndex, legInfo.GroupIndex);

                // Add to the [group index : leg indicies] dict.
                if (!_legGroups.TryAdd(legInfo.GroupIndex, new List<int>() { legInfo.LegIndex }))
                    _legGroups[legInfo.GroupIndex].Add(legInfo.LegIndex);
            }

            // Populate the [group index : leg indicies in adjace groups] dict.
            /*_adjacentLegs = new();
            foreach(GaitGroupInfo groupInfo in GroupSetupInfo)
            {
                _adjacentLegs.Add(groupInfo.GroupIndex, new List<int>());
                foreach (int adjacentGroupIndex in groupInfo.AdjacentGroups)
                    _adjacentLegs[groupInfo.GroupIndex].AddRange(_legGroups[adjacentGroupIndex]);
            }*/
            // Temp: Populate the Adjacent Legs manually until we've setup the inspector for the GroupSetupInfo.
            _adjacentLegs = new();
            _adjacentLegs.Add(0, _legGroups[1]);
            _adjacentLegs.Add(1, _legGroups[0]);
        }

        [System.Serializable]
        public class GaitLegInfo
        {
            public MechLeg Reference;
            public int LegIndex;
            public int GroupIndex;

            public GaitLegInfo(MechLeg reference, int legIndex)
            {
                Reference = reference;
                LegIndex = legIndex;
                GroupIndex = -1;    // Assign to the 'invalid' group by default.
            }
        }
        [System.Serializable]
        public class GaitGroupInfo
        {
            public int GroupIndex;
            public List<int> AdjacentGroups;
        }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Gait))]
    [CanEditMultipleObjects]
    public class GaitDrawer : PropertyDrawer
    {
        private bool _foldout;

        // Editor Note: List of Legs with their group ID next to them, then list of groups with their adjacent groups next to them.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            _foldout = EditorGUI.Foldout(position, _foldout, label, true);
            if (_foldout)
            {
                EditorGUI.BeginChangeCheck();

                ++EditorGUI.indentLevel;
                DrawProperty(property);
                --EditorGUI.indentLevel;

                if (EditorGUI.EndChangeCheck())
                    // Modified.
                {
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    // Undo?
                }

            }
            EditorGUI.EndProperty();
        }
        private void DrawProperty(SerializedProperty property)
        {
            if (!property.GetTargetObjectOfProperty().TryCastToType<Gait>(out Gait gait))
            {
                EditorGUILayout.HelpBox($"Failed to cast to {nameof(Gait)}", MessageType.Error);
                return;
            }
            MechBody body = property.serializedObject.targetObject as MechBody;
            if (body == null)
            {
                EditorGUILayout.HelpBox("Body not set", MessageType.Warning);
                return;
            }


            // Draw Settings field.
            EditorGUILayout.PropertyField(property.FindPropertyRelative("Settings"));

            // Draw Legs & Group IDs.
            UpdateLegs(gait, body);

            // Draw Groups & Adjacent Group IDs.
            DrawGroupInfo(gait, body);
        }


        private void UpdateLegs(Gait gait, MechBody body)
        {
            UpdateGaitLegInfo(gait, body);
            DrawLegInfo(gait);

            gait.UpdateValues();
        }
        private void UpdateGaitLegInfo(Gait gait, MechBody body)
        {
            if (gait.LegSetupInfo != null)
            {
                // Check if our values are correct.
                bool changed = gait.LegSetupInfo.Count != body.Legs.Length;
                if (!changed)
                {
                    for (int i = 0; i < body.Legs.Length; ++i)
                    {
                        if (gait.LegSetupInfo[i].Reference != body.Legs[i])
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (!changed)
                    return; // No Changes needed.
            }
            else
                gait.LegSetupInfo = new();

            // Update our values.
            // Note: Leg References not in the body.Legs array will be pushed to the end of the list, and are removed in the next step.
            for (int i = 0; i < body.Legs.Length; ++i)
            {
                if (gait.LegSetupInfo.Count <= i)
                    // There is no LegInfo instances remaining.
                    // Add new LegInfo instances into our array with the correct info,
                {
                    Debug.Log("Adding Leg Info");
                    gait.LegSetupInfo.Add(new Gait.GaitLegInfo(body.Legs[i], i));
                    continue; // The info at this index is now valid, as we've just set it up.
                }

                if (!gait.LegSetupInfo[i].Reference.ShareTargets(body.Legs[i]))
                    // Wrong Reference.
                {
                    // Find the index of the LegInfo with the correct MechLeg reference.
                    int correctInfoIndex = gait.LegSetupInfo.FindIndex(info => info.Reference.ShareTargets(body.Legs[i]));
                    if (correctInfoIndex == -1)
                        // We don't have a LegInfo instance with this MechLeg reference. Therefore: This is a new leg.
                        // Insert into our LegInfo array with all the correct info.
                    {
                        Debug.Log("Inserting Leg Info");
                        gait.LegSetupInfo.Insert(i, new Gait.GaitLegInfo(body.Legs[i], i));
                        continue; // The info at this index is now valid, as we've just set it up.
                    }

                    // Swap the LegInfo references so we have the correct LegInfo.
                    Gait.GaitLegInfo tmp = gait.LegSetupInfo[i];
                    gait.LegSetupInfo[i] = gait.LegSetupInfo[correctInfoIndex];
                    gait.LegSetupInfo[correctInfoIndex] = tmp;
                } // Reference is now correct.


                if (gait.LegSetupInfo[i].LegIndex != i)
                    // Wrong Index.
                {
                    int correctInfoIndex = gait.LegSetupInfo.FindIndex(info => info.LegIndex == i);
                    if (correctInfoIndex == -1)
                    {
                        // No other LegInfo instances have the correct index.
                        // We can directly change the index value.
                        gait.LegSetupInfo[i].LegIndex = i;
                    }
                    else
                    {
                        // Swap indicies with the LegInfo at the correct index.
                        int tmp = gait.LegSetupInfo[correctInfoIndex].LegIndex;
                        gait.LegSetupInfo[correctInfoIndex].LegIndex = gait.LegSetupInfo[i].LegIndex;
                        gait.LegSetupInfo[i].LegIndex = tmp;
                    }
                } // Index is now correct.
            }

            // Clear removed legs, if any exist.
            int diff = gait.LegSetupInfo.Count - body.Legs.Length;
            if (diff > 0)
            {
                Debug.Log($"Removing {diff} elements");
                Debug.Log($"Desired Legs Count: {body.Legs.Length}");
                Debug.Log($"Current Legs Count: {gait.LegSetupInfo.Count}");
                gait.LegSetupInfo.RemoveRange(body.Legs.Length, diff);
            }
        }
        private void DrawLegInfo(Gait gait)
        {
            // For each leg, display their array index & target name (Both readonly) next to their group index (Editable).
            foreach(Gait.GaitLegInfo legInfo in gait.LegSetupInfo)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"{legInfo.GroupIndex}: {legInfo.Reference.IKTargetTransform.name}");
                legInfo.GroupIndex = EditorGUILayout.IntField(legInfo.GroupIndex);

                EditorGUILayout.EndHorizontal();
            }
        }



        private void DrawGroupInfo(Gait gait, MechBody body)
        {

        }
    }
#endif
}