using TMPro;
using UI.Icons;
using UnityEngine;
using UnityEngine.InputSystem;
using UserInput;

public class ControlDisplayText : MonoBehaviour
{
    [SerializeField] private TMP_Text _thisText;
    [SerializeField] private string _formattingText = "{0}";
    [SerializeField] private InputActionReference _inputAction;


    [Space(10)]
    [SerializeField] private bool _useDeviceOverride = false;
    [SerializeField] private ClientInput.DeviceType _deviceTypeOverride;


    private void Awake()
    {
        InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateText;
        InputIconManager.OnSpriteAssetChanged += UpdateSpriteAsset;
    }
    private void Start()
    {
        UpdateText();
        UpdateSpriteAsset();
    }
    private void OnDestroy()
    {
        InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateText;
        InputIconManager.OnSpriteAssetChanged -= UpdateSpriteAsset;
    }

    private void UpdateText() => _thisText.text = _useDeviceOverride ? InputIconManager.FormatTextForIconFromInputAction(_formattingText, _inputAction, _deviceTypeOverride) : InputIconManager.FormatTextForIconFromInputAction(_formattingText, _inputAction);
    private void UpdateSpriteAsset() => _thisText.spriteAsset = InputIconManager.GetSpriteAsset();


#if UNITY_EDITOR

    private void Reset()
    {
        _thisText = GetComponentInChildren<TMP_Text>(); 
    }

#endif
}