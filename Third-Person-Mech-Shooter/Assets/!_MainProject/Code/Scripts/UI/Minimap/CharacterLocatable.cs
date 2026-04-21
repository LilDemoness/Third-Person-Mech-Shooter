using Gameplay.GameplayObjects.Character;
using Gameplay.GameplayObjects.Players;
using UnityEngine;

namespace Gameplay.UI.Minimap
{
    [RequireComponent(typeof(ServerCharacter))]
    public class CharacterLocatable : BaseLocatable
    {
        private ServerCharacter _serverCharacter;

        [SerializeField] private TeamBasedLocatableIcon _iconPrefab;
        private LocatableType _locatableType;


        private void Awake()
        {
            _serverCharacter = GetComponent<ServerCharacter>();

            _locatableType = GetLocatableType();

            // If the LocalClientInstance is already set, our value for LocatableType is correct.
            // Otherwise, we need to update its value once the player is set.
            if (Player.LocalClientInstance == null)
                Player.OnLocalPlayerSet += Player_OnLocalPlayerSet;
            else if (Player.LocalClientInstance.ServerCharacter == _serverCharacter)
            {
                // Don't display for the local player's server character.
                Destroy(this);
                return;
            }

            // Update the icon's colour whenever the Server Character's team changes.
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
            if (Player.LocalClientInstance.ServerCharacter == _serverCharacter)
            {
                // Don't display for the local player's server character.
                Destroy(this);
                return;
            }

            UpdateLocatableType();

            // Update the icon's colour whenever the local player's team changes.
            // To-do: Implement.
        }
        private void ServerCharacter_OnTeamChanged() => TryUpdateLocatableType();

        private void TryUpdateLocatableType()
        {
            if (Player.LocalClientInstance == null)
                return;
            UpdateLocatableType();
        }
        private void UpdateLocatableType()
        {
            LocatableType oldType = _locatableType;
            _locatableType = GetLocatableType();

            InvokeOnLocatableTypeChanged(oldType);
        }



        public override bool ClampToRadarBorder { get => false; }
        public override LocatableType LocatableType { get => _locatableType; }
        private LocatableType GetLocatableType() => (Player.LocalClientInstance != null && _serverCharacter.IsSameTeam(Player.LocalClientInstance.ServerCharacter)) ? LocatableType.Friendly : LocatableType.Enemy;

        public override BaseLocatableIcon CreateIcon()
        {
            TeamBasedLocatableIcon iconInstance = Instantiate(_iconPrefab);
            iconInstance.Setup(_serverCharacter);

            return iconInstance;
        }
    }
}