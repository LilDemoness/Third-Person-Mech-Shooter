using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utils
{
    /// <summary>
    ///     Container for an Array with a custom drawer in the Unity Inspector, allowing for adding of elements without Unity resetting all their values on their first deserialization.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [System.Serializable]
    public class ClassArray<T>
    {
        [SerializeField] private T[] _array = new T[0];

        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }
        public int Length => _array.Length;
        public void Resize(int newSize) => System.Array.Resize(ref _array, newSize);
    }


    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ClassArray<>), true)]
    public class ClassArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //Debug.Log(property.FindPropertyRelative("_array").GetPropertyType().GenericTypeArguments[0]);
            EditorList.DrawList(position, property.FindPropertyRelative("_array"), label, EditorList.EditorListOptions.All);
        }
    }
    #endif
}