using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Animations.InverseKinematics
{
    [CustomPropertyDrawer(typeof(IKIterateBaseSetting), true)]
    [CanEditMultipleObjects]
    public class IKIterateBaseSettingDrawer : PropertyDrawer
    {
        private bool _foldout = true;
        private bool _showJoints = true;

        private const string ROOT_BONE_EDITOR_PROPERTY_IDENTIFIER = "_rootBone";
        private const string END_BONE_EDITOR_PROPERTY_IDENTIFIER = "_endBone";


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.GetTargetObjectOfProperty().TryCastToType<IKIterateBaseSetting>(out IKIterateBaseSetting setting))
                return;

            // Draw Element Label.
            string targetName = setting.Target != null ? setting.Target.name : "No Target";
            int jointCount = setting.Joints.Length;
            _foldout = EditorGUI.Foldout(position, _foldout, $"{(jointCount == 1 ? "1 Joint" : $"{jointCount} Joints")} - Target: {targetName}", true);
            if (!_foldout)
                return;

            // Draw Underline.
            DrawHorizontalLine(thickness: 1, abovePadding: 0, belowPadding: 5);

            // Target.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.Target)));
            EditorGUILayout.Space(5);


            // Root & End Bone.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(ROOT_BONE_EDITOR_PROPERTY_IDENTIFIER));
            EditorGUILayout.PropertyField(property.FindPropertyRelative(END_BONE_EDITOR_PROPERTY_IDENTIFIER));
            EditorGUILayout.Space(5);


            // Extend End Bone.
            DisplayVirtualEndBoneSettings(property);
            EditorGUILayout.Space(5);


            // Joints Array.
            HandleJointsArray(property, setting);
        }

        private void DrawHorizontalLine(float thickness, float abovePadding, float belowPadding)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(abovePadding + belowPadding + thickness));
            r.height = thickness;
            r.y += abovePadding;
            r.x -= 2;
            //r.width += 6;
            EditorGUI.DrawRect(r, Color.grey);
        }


        private void DisplayVirtualEndBoneSettings(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.ExtendEndBone)));
            if (property.FindPropertyRelative(nameof(IKIterateBaseSetting.ExtendEndBone)).boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.EndBoneDirection)));
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.EndBoneLength)));
                --EditorGUI.indentLevel;
            }
        }


        private void HandleJointsArray(SerializedProperty property, IKIterateBaseSetting settings)
        {
            SerializedProperty jointsProp = property.FindPropertyRelative(nameof(IKIterateBaseSetting.Joints));

            PerformJointsArrayValidation(property, jointsProp, settings);

            // Display Bone Joints.
            DisplayJointsArray(jointsProp);
        }
        private void PerformJointsArrayValidation(SerializedProperty property, SerializedProperty jointsProp, IKIterateBaseSetting settings)
        {
            // Retrieve Joints Array.
            if (!jointsProp.GetTargetObjectOfProperty().TryCastToType<IKIterateBaseJoint[]>(out IKIterateBaseJoint[] boneJoints))
            {
                EditorGUILayout.LabelField("Failed to access 'Joints' (BoneJoint[])");
                return;
            }

            // Retrieve Root and End Bone Values.
            if (!property.FindPropertyRelative(ROOT_BONE_EDITOR_PROPERTY_IDENTIFIER).GetTargetObjectOfProperty().TryCastToType<Transform>(out Transform rootBone)
                || !property.FindPropertyRelative(END_BONE_EDITOR_PROPERTY_IDENTIFIER).GetTargetObjectOfProperty().TryCastToType<Transform>(out Transform endBone))
            {
                EditorGUILayout.LabelField("Failed to retrieve value for '_rootBone' or '_endBone'");
                return;
            }


            // Validation.
            if (rootBone == null)
            // Invalid: No Root Bone.
            {
                EditorGUILayout.HelpBox("Root Bone is unassigned", MessageType.Error);
                return;
            }
            if (endBone == null)
            // Invalid: No End Bone.
            {
                EditorGUILayout.HelpBox("End Bone is unassigned", MessageType.Error);
                return;
            }

            int desiredJointsCount = endBone.GetStepsToParent(rootBone);
            if (desiredJointsCount == -1)
            // Invalid: End is not a child of Root.
            {
                EditorGUILayout.HelpBox($"End Bone {endBone.name} must be a child of {rootBone.name}", MessageType.Error);
                return;
            }

            // If we are extending the end bone, then the 'tip' transform is a joint too.
            // Otherwise, it is just the end of our last bone.
            if (settings.ExtendEndBone)
                ++desiredJointsCount;
            else
                endBone = endBone.parent;


            // Update the BoneJoints array if our values no longer match.
            int currentJointsCount = boneJoints.Length;
            if (currentJointsCount != desiredJointsCount || rootBone != boneJoints[0].Bone || endBone != boneJoints[currentJointsCount - 1].Bone || GUILayout.Button("Reset Joints"))
            {
                // Update Bone Joints.
                EditorGUILayout.HelpBox($"Needing to update Joints", MessageType.Info);

                if (currentJointsCount > 0 && boneJoints[0].Bone == rootBone)
                {
                    System.Array.Resize(ref boneJoints, desiredJointsCount);

                    // Populate the array in reverse order, only overriding elements where the Bone doesn't match the intended one so as to preserve other aspects.
                    // Reverse order makes element 0 the root bone and element 'length-1' the end bone.
                    Transform currentBone = endBone;
                    for (int i = desiredJointsCount - 1; i >= 0; --i)
                    {
                        if (boneJoints[i] == null || boneJoints[i].Bone != currentBone)
                        {
                            boneJoints[i] = new IKIterateBaseJoint();
                            boneJoints[i].Initialise(currentBone);
                        }
                        else
                            boneJoints[i].Initialise(currentBone);

                        currentBone = currentBone.parent;
                    }
                }
                else
                {
                    boneJoints = new IKIterateBaseJoint[desiredJointsCount];

                    // Populate the array in reverse order, making 0 the root bone and 'length-1' the end bone.
                    Transform currentBone = endBone;
                    for (int i = desiredJointsCount - 1; i >= 0; --i)
                    {
                        boneJoints[i] = new IKIterateBaseJoint();
                        boneJoints[i].Initialise(currentBone);

                        currentBone = currentBone.parent;
                    }
                }

                settings.Joints = boneJoints;
            }
        }
        private void DisplayJointsArray(SerializedProperty jointsProp)
        {
            _showJoints = EditorGUILayout.Foldout(_showJoints, "Joints", true);
            if (_showJoints)
            {
                ++EditorGUI.indentLevel;
                for (int i = 0; i < jointsProp.arraySize; ++i)
                {
                    SerializedProperty jointProp = jointsProp.GetArrayElementAtIndex(i);
                    string boneName = jointProp.GetTargetObjectOfProperty().TryCastToType<IKIterateBaseJoint>(out var castResult) && castResult.Bone != null ? castResult.Bone.name : "Error";

                    EditorGUILayout.PropertyField(jointProp, new GUIContent($"{i}: {boneName}"));
                }
                --EditorGUI.indentLevel;
            }
        }
    }
}