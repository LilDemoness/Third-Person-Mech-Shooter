using UnityEngine;
using UnityServices.Sessions;
using VContainer;
using TMPro;
using System.Collections;

namespace Gameplay.UI
{
    public class LobbyJoinCodeDisplayUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _codeDisplayLabel;


        // VContainer Dependency Injection.
        private MultiplayerServicesFacade _multiplayerServicesFacade;

        [Inject]
        private void InjectAndInitialize(
            MultiplayerServicesFacade multiplayerServicesFacade)
        {
            this._multiplayerServicesFacade = multiplayerServicesFacade;
            this.gameObject.SetActive(false);

            if (_multiplayerServicesFacade.CurrentUnitySession != null)
                SubscribeToSession();
            else
                _multiplayerServicesFacade.OnCurrentSessionSet += SubscribeToSession;
        }
        private void SubscribeToSession()
        {
            _multiplayerServicesFacade.OnCurrentSessionSet -= SubscribeToSession;
            this.gameObject.SetActive(true);

            _multiplayerServicesFacade.CurrentUnitySession.Changed += UpdateJoinCode;
            UpdateJoinCode();
        }

        private void UpdateJoinCode()
        {
            _codeDisplayLabel.text = "Join Code: " + _multiplayerServicesFacade.CurrentUnitySession.Code;
        }
    }
}