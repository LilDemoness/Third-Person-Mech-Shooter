using Gameplay.Configuration;
using Infrastructure;
using Netcode.ConnectionManagement;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Gameplay.UI.Menus
{
    /// <summary>
    ///     Handles
    /// </summary>
    public class DirectIPUI : MonoBehaviour
    {
        public const string DEFAULT_IP = "127.0.0.1";
        public const int DEFAULT_PORT = 9998;


        [SerializeField] private TMP_InputField _ipInputField;
        [SerializeField] private TMP_InputField _portInputField;
        [SerializeField] private Button[] _buttons;

        [Space(5)]
        [SerializeField] private GameObject _signInSpinner;
        [SerializeField] private IPConnectionWindow _ipConnectionWindow;



        private ConnectionManager _connectionManager;
        private ISubscriber<ConnectStatus> _connectStatusSubscriber;


        [Inject]
        private void InjectDependencies(ISubscriber<ConnectStatus> connectStatusSubscriber, ConnectionManager connectionManager)
        {
            if (_connectStatusSubscriber != null)
                return; // Bandaid Fix.

            this._connectStatusSubscriber = connectStatusSubscriber;
            this._connectionManager = connectionManager;

            _connectStatusSubscriber.Subscribe(OnConnectStatusMessage);
        }


        private void Awake()
        {
            // Ensure that the Sign-in Spinner starts hidden.
            // Note: Move to always occur on showing.
            DisableSignInSpinner();

            // Default to default values.
            _ipInputField.text = DEFAULT_IP;
            _portInputField.text = DEFAULT_PORT.ToString();
        }
        private void OnDestroy()
        {
            if (_connectStatusSubscriber != null)
                _connectStatusSubscriber.Unsubscribe(OnConnectStatusMessage);
        }


        #region UI Button Functions

        public void OnJoinButtonPressed() => JoinWithIP(_ipInputField.text, _portInputField.text);
        public void OnHostButtonPressed() => HostIPRequest(_ipInputField.text, _portInputField.text);

        #endregion


        private void OnConnectStatusMessage(ConnectStatus connectStatus)
        {
            DisableSignInSpinner();
        }

        public void HostIPRequest(string ip, string port)
        {
            ValidateIpAndPort(ref ip, port, out int portInteger);

            // Perform the Host attempt.
            EnableSignInSpinner();
            _connectionManager.StartHostIP(GetPlayerName(), ip, portInteger);
        }
        public void JoinWithIP(string ip, string port)
        {
            ValidateIpAndPort(ref ip, port, out int portInteger);

            // Perform the Join attempt.
            EnableSignInSpinner();
            _connectionManager.StartClientIP(GetPlayerName(), ip, portInteger);
            _ipConnectionWindow.ShowConnectionWindow();
        }


        private void ValidateIpAndPort(ref string ip, string port, out int portInteger)
        {
            // Port Validation.
            int.TryParse(port, out portInteger);
            if (portInteger <= 0)
            {
                // Invalid port. Set to default.
                portInteger = DEFAULT_PORT;
            }

            // IP Address Validation.
            ip = string.IsNullOrEmpty(ip) ? DEFAULT_IP : ip;
        }


        public void JoiningWindowCancelled()
        {
            DisableSignInSpinner();
            RequestShutdown();
        }

        public void EnableSignInSpinner() => _signInSpinner.SetActive(true);
        public void DisableSignInSpinner() => _signInSpinner.SetActive(false);

        private void RequestShutdown()
        {
            if (_connectionManager != null && _connectionManager.NetworkManager != null)
            {
                _connectionManager.RequestShutdown();
            }
        }



        // To be called from the back/close UI button.
        public void CancelConnectingWindow()
        {
            RequestShutdown();
            _ipConnectionWindow.CancelConnectionWindow();
        }



        /// <summary>
        ///     Added to the IP InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitizeIPInputText()
        {
            _ipInputField.text = SanitizeIP(_ipInputField.text);
            SetButtonInteractability(AreIPAddressAndPortValid(_ipInputField.text, _portInputField.text));
        }
        /// <summary>
        ///     Added to the Port InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitisePortText()
        {
            _portInputField.text = SanitizePort(_portInputField.text);
            SetButtonInteractability(AreIPAddressAndPortValid(_ipInputField.text, _portInputField.text));
        }
        private void SetButtonInteractability(bool isInteractable)
        {
            for(int i = 0; i < _buttons.Length; ++i)
                _buttons[i].interactable = isInteractable;
        }

        /// <summary>
        ///     Sanitize user IP address InputField box allowing only numbers and periods.<br/>
        ///     This also prevents undesirable invisible characters from being copy-pased accidentally.
        /// </summary>
        /// <param name="dirtyString"> The string to sanitize.</param>
        /// <returns> Sanitized text string.</returns>
        public static string SanitizeIP(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^0-9.]", "");
        }
        /// <summary>
        ///     Sanitize user port InputField box by allowing only numbers.<br/>
        ///     This also prevents undesirable invisible characters from being copy-pased accidentally.
        /// </summary>
        /// <param name="dirtyString"> The string to sanitize.</param>
        /// <returns> Sanitized text string.</returns>
        public static string SanitizePort(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^0-9]", "");
        }

        public static bool AreIPAddressAndPortValid(string ipAddress, string port)
        {
            bool portValid = ushort.TryParse(port, out ushort portNumber);
            return portValid && NetworkEndpoint.TryParse(ipAddress, portNumber, out var networkEndpoint);
        }


        private string GetPlayerName() { Debug.LogWarning("Player Names not Implemented"); return "Mighty Miku"; }
    }
}