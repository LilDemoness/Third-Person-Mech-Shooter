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
            _adjacentLegs = new();
            foreach(GaitGroupInfo groupInfo in GroupSetupInfo)
            {
                _adjacentLegs.Add(groupInfo.GroupIndex, new List<int>());
                foreach (int adjacentGroupIndex in groupInfo.AdjacentGroups)
                    _adjacentLegs[groupInfo.GroupIndex].AddRange(_legGroups[adjacentGroupIndex]);
            }
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

            public GaitGroupInfo(int groupIndex)
            {
                GroupIndex = groupIndex;
                AdjacentGroups = new();
            }
        }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Gait), true)]
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
            EditorGUILayout.Space(2.5f);

            // Draw Legs & Group IDs.
            UpdateLegs(gait, body);
            EditorGUILayout.Space(2.5f);

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
            // Draw a header for the leg info section.
            EditorGUILayout.LabelField("Leg Groups", new GUIStyle("HeaderLabel"));
            EditorGUIUtils.DrawHorizontalLine(thickness: 1.0f, leftPadding: 10.0f);

            // For each leg, display their array index & target name (Both readonly) next to their group index (Editable).
            foreach (Gait.GaitLegInfo legInfo in gait.LegSetupInfo)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"{legInfo.GroupIndex}: {legInfo.Reference.IKTargetTransform.name}");
                legInfo.GroupIndex = EditorGUILayout.IntField(legInfo.GroupIndex);

                EditorGUILayout.EndHorizontal();
            }
        }



        private void DrawGroupInfo(Gait gait, MechBody body)
        {
            EnsureGroupInfoIsCorrect(gait);
            int groupCount = gait.GroupSetupInfo.Count;

            // Draw a header for the group adjacency section.
            EditorGUILayout.LabelField("Group Adjacency Matrix", new GUIStyle("HeaderLabel"));
            EditorGUIUtils.DrawHorizontalLine(thickness: 1.0f, leftPadding: 10.0f);


            const float GROUP_LABEL_WIDTH = 60.0f;
            const float GROUP_BUTTON_WIDTH = 30.0f;
            float tableRowHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            GUIStyle labelStyle = new GUIStyle("AM HeaderStyle") { alignment = TextAnchor.MiddleCenter };


            Rect fullRect = EditorGUILayout.BeginVertical();
            const float PER_INDENT_SIZE = 10.0f;
            fullRect.x += EditorGUI.indentLevel * PER_INDENT_SIZE;
            fullRect.width -= EditorGUI.indentLevel * PER_INDENT_SIZE;

            // Draw a background.
            const float BACKGROUND_COLOR = 0.3f;
            EditorGUI.DrawRect(fullRect, new Color(BACKGROUND_COLOR, BACKGROUND_COLOR, BACKGROUND_COLOR));

            // Draw table headers.
            {
                Rect rect = EditorGUILayout.BeginHorizontal();
                
                // Draw 'Group' header.
                rect.width = GROUP_LABEL_WIDTH;
                EditorGUI.LabelField(rect, "Group", labelStyle);
                rect.x += GROUP_LABEL_WIDTH;

                // Draw group index headers.
                for (int i = 0; i < groupCount; ++i)
                {
                    rect.width = GROUP_BUTTON_WIDTH;
                    EditorGUI.LabelField(rect, gait.GroupSetupInfo[i].GroupIndex.ToString(), labelStyle);

                    rect.x += GROUP_BUTTON_WIDTH;
                }

                EditorGUILayout.Space(tableRowHeight);
                EditorGUILayout.EndHorizontal();
            }


            // Draw table elements (Group Index | Other Group Adjacency Toggles).
            for (int i = 0; i < groupCount; ++i)
            {
                Rect rect = EditorGUILayout.BeginHorizontal();

                rect.width = GROUP_LABEL_WIDTH;
                EditorGUI.LabelField(rect, gait.GroupSetupInfo[i].GroupIndex.ToString(), labelStyle);
                rect.x += GROUP_LABEL_WIDTH;

                for (int j = 0; j < groupCount; ++j)
                {
                    rect.width = GROUP_BUTTON_WIDTH;

                    if (i == j)
                    {
                        // Groups cannot be adjacent to themselves.
                        // Display a non-editable, always-off toggle box.
                        bool previousEnabledState = GUI.enabled;
                        GUI.enabled = false;
                        EditorGUI.Toggle(rect, false);
                        GUI.enabled = previousEnabledState;
                    }
                    else
                    {
                        bool oldValue = gait.GroupSetupInfo[i].AdjacentGroups.Contains(j);
                        bool toggle = EditorGUI.Toggle(rect, oldValue);

                        if (toggle != oldValue)
                        {
                            if (oldValue == true)
                            {
                                gait.GroupSetupInfo[i].AdjacentGroups.Remove(j);
                                gait.GroupSetupInfo[j].AdjacentGroups.Remove(i);
                            }
                            else
                            {
                                gait.GroupSetupInfo[i].AdjacentGroups.Add(j);
                                gait.GroupSetupInfo[j].AdjacentGroups.Add(i);
                            }
                        }
                    }

                    rect.x += GROUP_BUTTON_WIDTH;
                }

                EditorGUILayout.Space(tableRowHeight);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void EnsureGroupInfoIsCorrect(Gait gait)
        {
            List<int> legGroups = GetLegGroups(gait);

            // Check already existing groups to ensure that all are valid.
            for(int i = 0; i < gait.GroupSetupInfo.Count; ++i)
            {
                int legGroupIndex = legGroups.IndexOf(gait.GroupSetupInfo[i].GroupIndex);

                if (legGroupIndex == -1)
                {
                    // This Leg Group no longer exists. Clear it.

                    // Remove references to this group in other GroupSetupInfo instances.
                    for (int j = 0; j < gait.GroupSetupInfo.Count; ++j)
                    {
                        int indexToRemove = gait.GroupSetupInfo[j].AdjacentGroups.IndexOf(gait.GroupSetupInfo[i].GroupIndex);
                        if (indexToRemove != -1)
                            gait.GroupSetupInfo[j].AdjacentGroups.RemoveAt(indexToRemove);
                    }

                    // Remove the group setup info instance for this group.
                    gait.GroupSetupInfo.RemoveAt(i);

                    --i;
                    continue;
                }

                // Mark this group index as having been validated by removing it from the list so we don't check it again.
                legGroups.RemoveAt(legGroupIndex);
            }

            // Add new groups.
            for(int i = 0; i < legGroups.Count; ++i)
                gait.GroupSetupInfo.Add(new Gait.GaitGroupInfo(legGroups[i]));
        }

        /// <summary>
        ///     Returns all the Leg Groups present in this Gait, in ascending index order.
        /// </summary>
        private List<int> GetLegGroups(Gait gait)
        {
            List<int> groups = new List<int>();
            int largestValue = -1;
            foreach(var legSetup in gait.LegSetupInfo)
            {
                if (legSetup.GroupIndex < 0)
                    continue; // Group indicies below 0 are used to represent 'no group'.

                if (legSetup.GroupIndex > largestValue)
                {
                    groups.Add(legSetup.GroupIndex);
                    largestValue = legSetup.GroupIndex;
                }
                else
                {
                    for(int i = 0; i < groups.Count; ++i)
                    {
                        if (groups[i] == legSetup.GroupIndex)
                            break;  // We've already got this group in the list.

                        if (groups[i] > legSetup.GroupIndex)
                        {
                            // We've passed where the group should be and haven't yet found it.
                            // All future elements will be larger than the group index,
                            //  so insert it before this one and stop searching.
                            groups.Insert(i, legSetup.GroupIndex);   
                            break;
                        }
                    }
                }
            }

            return groups;
        }
    }
#endif
}