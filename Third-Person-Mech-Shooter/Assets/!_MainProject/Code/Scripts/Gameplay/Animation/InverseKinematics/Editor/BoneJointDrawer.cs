using UnityEditor;
using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    [CustomPropertyDrawer(typeof(BoneJoint), true)]
    public class BoneJointDrawer : PropertyDrawer
    {
        const float BUTTON_HEIGHT = 20.0f;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.GetTargetObjectOfProperty().TryCastToType<BoneJoint>(out BoneJoint boneJoint))
                return;

            Rect originalPos = new Rect(position);
            boneJoint.Editor_Show = EditorGUI.Foldout(position, boneJoint.Editor_Show, label, true);
            if (!boneJoint.Editor_Show)
                return;

            // Account for indent.
            position.x += 15.0f;
            position.width -= 15.0f;
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Readonly Bone Property.
            position = InspectorUtils.DrawWithCallback(position, SerializedPropertyType.ObjectReference, (Rect propertyPos) => EditorGUI.ObjectField(propertyPos, GUIContent.none, boneJoint.Bone, typeof(Transform), true), new GUIContent("Bone"), true);

            // Draw button to update rest.
            Rect buttonRect = new Rect(position);
            buttonRect.height = BUTTON_HEIGHT;
            if (GUI.Button(buttonRect, "Update Rest Pose"))
                boneJoint.UpdateRest();
            
            position.y += buttonRect.height + EditorGUIUtility.standardVerticalSpacing;


            // Readonly Rest Properties.
            position = InspectorUtils.DrawWithCallback(position, SerializedPropertyType.Vector3, (Rect propertyPos) => EditorGUI.Vector3Field(propertyPos, GUIContent.none, boneJoint.RestPosition), new GUIContent("Rest Position"), true);
            position = InspectorUtils.DrawWithCallback(position, SerializedPropertyType.Quaternion, (Rect propertyPos) => EditorGUI.Vector3Field(propertyPos, GUIContent.none, boneJoint.RestRotation.eulerAngles), new GUIContent("Rest Rotation"), true);

            // Have the editor register the space our elements take up.
            EditorGUILayout.Space((position.y - originalPos.y) - position.height);
        }
    }
}