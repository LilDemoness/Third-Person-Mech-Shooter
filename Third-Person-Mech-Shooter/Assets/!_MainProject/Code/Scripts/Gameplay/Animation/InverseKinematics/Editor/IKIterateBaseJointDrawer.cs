using UnityEditor;
using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    [CustomPropertyDrawer(typeof(IKIterateBaseJoint), true)]
    public class IKIterateBaseJointDrawer : BoneJointDrawer
    {
        MackySoft.SerializeReferenceExtensions.Editor.SubclassSelectorDrawer _drawer = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.GetTargetObjectOfProperty().TryCastToType<IKIterateBaseJoint>(out IKIterateBaseJoint boneJoint))
                return;

            // Draw default GUI.
            base.OnGUI(position, property, label);

            // We're not wishing to show the element.
            if (!boneJoint.Editor_Show)
                return;

            ++EditorGUI.indentLevel;

            // Rotation Axis.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.RotationAxis)));
            // Rotation Axis Vector (Only if Custom).
            if (boneJoint.RotationAxis == RotationAxis.Custom)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.RotationAxisVector)));
                --EditorGUI.indentLevel;
            }


            EditorGUILayout.Space(5);

            // Limitation Reference.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.Limitation)));

            EditorGUILayout.Space(5);

            // LimitationRightAxis.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.LimitationRightAxis)));
            // LimitationRightAxisVector (Only if Custom).
            if (boneJoint.LimitationRightAxis == SecondaryDirection.Custom)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.LimitationRightAxisVector)));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Space(5);

            // Limitation Rotation Offset.
            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(IKIterateBaseJoint.LimitationRotationOffset)));

            --EditorGUI.indentLevel;
        }
    }
}