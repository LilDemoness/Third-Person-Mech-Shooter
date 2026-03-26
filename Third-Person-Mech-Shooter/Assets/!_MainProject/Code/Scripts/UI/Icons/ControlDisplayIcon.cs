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


    [Space(10)]
    [SerializeField] private bool _useDeviceOverride = false;
    [SerializeField] private ClientInput.DeviceType _deviceTypeOverride;


    private void Awake() => InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateIcon;
    private void Start() => UpdateIcon();
    private void OnDestroy() => InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateIcon;
    

    private void UpdateIcon() => _image.sprite = _useDeviceOverride ? InputIconManager.GetIconForAction(ClientInput.GetReferenceForAction(_inputAction), _deviceTypeOverride) : InputIconManager.GetIconForAction(ClientInput.GetReferenceForAction(_inputAction));


#if UNITY_EDITOR

    private void Reset()
    {
        _image = GetComponentInChildren<Image>();
    }

#endif
}