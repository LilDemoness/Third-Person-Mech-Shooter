using UnityEditor;
using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    [CustomPropertyDrawer(typeof(IKIterateBaseSetting), true)]
    [CanEditMultipleObjects]
    public class IKIterateBaseSettingDrawer : PropertyDrawer
    {
        private bool _showJoints = true;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Target.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.Target)));
            
            // Root Bone.
            /*SerializedProperty rootBoneProp = property.FindPropertyRelative(nameof(IKChainBaseSetting.RootBone));
            if (rootBoneProp.GetTargetObjectOfProperty().TryCastToType<BoneJoint>(out BoneJoint rootBone))
            {
                Transform newValue = EditorGUILayout.ObjectField(rootBone.Bone, typeof(Transform), true) as Transform;

                if (newValue != null && newValue != rootBone.Bone)
                    rootBone.SetValues(newValue);
            }
            else
                EditorGUILayout.LabelField("Error retrieving RootBone instance");*/

            // Root & End Bone.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.RootBone)), true);
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.EndBone)), true);



            // Extend End Bone.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.ExtendEndBone)));
            if (property.FindPropertyRelative(nameof(IKChainBaseSetting.ExtendEndBone)).boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.EndBoneDirection)));
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.EndBoneLength)));
                --EditorGUI.indentLevel;
            }


            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseSetting.JointSettings)));


            // Joints Array.
            SerializedProperty jointsProp = property.FindPropertyRelative(nameof(IKChainBaseSetting.Joints));
            if (jointsProp.GetTargetObjectOfProperty().TryCastToType<BoneJoint[]>(out BoneJoint[] boneJoints))
            {
                if (property.FindPropertyRelative(nameof(IKChainBaseSetting.RootBone)).GetTargetObjectOfProperty().TryCastToType<BoneJoint>(out BoneJoint rootBone)
                    &&
                    property.FindPropertyRelative(nameof(IKChainBaseSetting.EndBone)).GetTargetObjectOfProperty().TryCastToType<BoneJoint>(out BoneJoint endBone))
                {
                    if (rootBone.Bone == null || endBone.Bone == null)
                        return;

                    int rootToEndLength = endBone.Bone.GetStepsToParent(rootBone.Bone);
                    if (rootToEndLength == -1)
                        // Root isn't the parent of End.
                    {
                        Debug.LogError("Root Bone isn't the parent of EndBone");
                        return;
                    }

                    if (boneJoints.Length != rootToEndLength + 1)
                    {
                        // Resize joints array.
                        boneJoints = new BoneJoint[rootToEndLength + 1];
                        Transform joint = endBone.Bone;
                        int i = 0;
                        do
                        {
                            boneJoints[i] = new BoneJoint(joint);
                            ++i;
                            joint = joint.parent;
                        } while(joint != rootBone.Bone);
                    }
                }


                _showJoints = EditorGUILayout.Foldout(_showJoints, "Joints");
                if (_showJoints)
                {
                    ++EditorGUI.indentLevel;
                    for (int i = 0; i < boneJoints.Length; ++i)
                    {

                    }
                    EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKChainBaseSetting.Joints)));
                    --EditorGUI.indentLevel;
                }
            }
            else
                EditorGUILayout.LabelField("Failed to access 'Joints' (BoneJoint[])");
        }
    }
}