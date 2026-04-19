using Gameplay.GameplayObjects.Character.Customisation.Data;
using Gameplay.GameplayObjects.Players;
using TMPro;
using UI.Icons;
using UnityEngine;
using UserInput;
using UnityEngine.UI;
using Gameplay.Actions;
using Unity.Netcode;

public class PlayerSlotDisplayUI : MonoBehaviour
{
    [SerializeField] private AttachmentSlotIndex _slotIndex = AttachmentSlotIndex.Primary;
    [SerializeField] private TextMeshProUGUI _slotNameLabel;

    [Space(5)]
    [SerializeField] private Image[] _activationIcons;
    [SerializeField] private Image _activationIconMask;

    private float _cooldownStartTime;   // Server Time.
    private float _cooldownEndTime;     // Server Time.


    private void Awake()
    {
        if (_slotIndex == AttachmentSlotIndex.Unset)
        {
            Debug.LogError($"Error: {this.name} has an invalid Slot Index", this);
            return;
        }

        Player.OnLocalPlayerBuildUpdated += Player_OnLocalPlayerBuildUpdated;

        InputIconManager.OnShouldUpdateSpriteIdentifiers += UpdateSpriteIdentifiers;
        UpdateSpriteIdentifiers();
    }
    private void OnDestroy()
    {
        Player.OnLocalPlayerBuildUpdated -= Player_OnLocalPlayerBuildUpdated;

        InputIconManager.OnShouldUpdateSpriteIdentifiers -= UpdateSpriteIdentifiers;
    }


    /*private void Action_OnClientCooldownStarted(Action.CooldownStartedEventArgs e)
    {
        if (e.AttachmentSlotIndex != _slotIndex)
            return;
        if (Player.LocalClientInstance == null || e.Client != Player.LocalClientInstance.ServerCharacter.ClientCharacter)
            return;
        if (_cooldownStartTime < _cooldownEndTime)
            return; // Catching the current issue with action antitipation ignoring cooldowns.

        Debug.Log($"Cooldown Started at '{e.CooldownStartedTime}' for '{e.CooldownDuration}'");

        _cooldownStartTime = e.CooldownStartedTime;
        _cooldownEndTime = e.CooldownStartedTime + e.CooldownDuration;
    }*/
    /*private void LateUpdate()
    {
        if (NetworkManager.Singleton == null || _cooldownEndTime <= 0.0f)
            return;

        float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        UpdateActionCooldown(Mathf.InverseLerp(_cooldownStartTime, _cooldownEndTime, currentTime));
    }*/


    private void Player_OnLocalPlayerBuildUpdated(BuildData buildData)
    {
        if (buildData.GetFrameData().AttachmentPoints.Length < (int)_slotIndex)
        {
            this.gameObject.SetActive(false);
        }
        else
        {
            this.gameObject.SetActive(true);
            SetName(buildData.GetSlottableData(_slotIndex).Name);
        }
    }
    private void SetName(string name) => _slotNameLabel.text = name;


    private void UpdateSpriteIdentifiers()
    {
        foreach(Image activationIcon in _activationIcons)
            activationIcon.sprite = InputIconManager.GetIconForAction(ClientInput.GetSlotActivationAction(_slotIndex));
    }
    private void UpdateActionCooldown(float cooldownPercentage) => _activationIconMask.fillAmount = cooldownPercentage;
}
