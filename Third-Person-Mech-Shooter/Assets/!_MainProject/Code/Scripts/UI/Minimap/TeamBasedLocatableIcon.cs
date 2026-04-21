using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI.Minimap
{
    public class TeamBasedLocatableIcon : BaseLocatableIcon
    {
        [SerializeField] private Image _icon;

        [Space(5)]
        [SerializeField] private Color _friendlyColour = Color.cyan;
        [SerializeField] private Color _enemyColour = Color.red;
        private ServerCharacter _owningCharacter;

        public void Setup(ServerCharacter owningCharacter)
        {
            _owningCharacter = owningCharacter;

            // Update the icon's colour once we have a local player.
            if (Player.LocalClientInstance == null)
                Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
            else
                Player_OnLocalPlayerSet();

            // Update the icon's colour whenever our ServerCharacter's team changes.
            // To-do: Implement.
        }
        private void OnDestroy()
        {
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
            // ServerCharacter team change unsubscription.
        }


        private void Player_OnLocalPlayerSet()
        {
            Player.OnLocalPlayerSet -= Player_OnLocalPlayerSet;
            UpdateIconColor();

            // Update the icon's colour whenever the local player's team changes.
            // To-do: Implement.
        }


        private void TryUpdateIconColor()
        {
            if (Player.LocalClientInstance == null)
                return;
            UpdateIconColor();
        }
        private void UpdateIconColor() => _icon.color = _owningCharacter.IsSameTeam(Player.LocalClientInstance.ServerCharacter) ? _friendlyColour : _enemyColour;


        public override void SetVisible(bool isVisible) => _icon.CrossFadeAlpha(isVisible ? 1.0f : 0.0f, 0.0f, false);
        public override void SetAlpha(float alpha) => _icon.CrossFadeAlpha(alpha, 0.0f, false);
    }
}