using TMPro;
using UI.Icons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UserInput;

public class ControlDisplayIcon : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private InputActionReference _inputAction;


    private void Awake() => InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateIcon;
    private void Start() => UpdateIcon();
    private void OnDestroy() => InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateIcon;
    

    private void UpdateIcon() => _image.sprite = InputIconManager.GetIconForAction(ClientInput.GetReferenceForAction(_inputAction));


#if UNITY_EDITOR

    private void Reset()
    {
        _image = GetComponentInChildren<Image>();
    }

#endif
}