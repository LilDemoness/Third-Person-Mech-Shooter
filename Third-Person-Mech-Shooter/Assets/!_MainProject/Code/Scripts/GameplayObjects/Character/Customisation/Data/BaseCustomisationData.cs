using UnityEngine;

namespace Gameplay.GameplayObjects.Character.Customisation.Data
{
    public abstract class BaseCustomisationData : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField][field: TextArea(2, 5)] public string Description { get; private set; }
    }
}