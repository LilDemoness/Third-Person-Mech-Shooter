using System.Text.RegularExpressions;
using Gameplay.UI.Popups;
using TMPro;
using UI;
using UnityEngine;
using Utils;
using VContainer;

namespace Gameplay.UI.Menus.Profile
{
    public class CreateProfileUI : Menu
    {
        [SerializeField] private TMP_InputField _profileNameInputField;
        [SerializeField] private NonNavigableButton _createProfileButton;


        [Inject]
        private ProfileManager _profileManager;


        // Authentication service only accepts profile names of 30 characters or under.
        private const int AUTHENTICATION_MAX_PROFILE_LENGTH = 30;
        private const string SANITISATION_STRING = "[^a-zA-z0-9]";


        public override void Show()
        {
            _profileNameInputField.text = "";
            base.Show();
        }


        /// <summary>
        ///     Added to the Profile Name InputField component's OnValueChanged callback.
        /// </summary>
        public void SanitiseProfileNameInputText()
        {
            _profileNameInputField.text = SanitiseProfileName(_profileNameInputField.text);
            _createProfileButton.IsInteractable = IsValidProfileName(_profileNameInputField.text);
        }
        private bool IsValidProfileName(string profileName) => profileName.Length > 0 && !_profileManager.AvailableProfiles.Contains(profileName) && _profileManager.IsValidNewProfileName(profileName);


        /// <summary>
        ///     Sanitises the input string by removing invalid characters and limiting its length.
        /// </summary>
        private string SanitiseProfileName(string dirtyString)
        {
            string output = Regex.Replace(dirtyString, SANITISATION_STRING, "");
            return output[..System.Math.Min(output.Length, AUTHENTICATION_MAX_PROFILE_LENGTH)];
        }


        public void CreateNewProfile()
        {
            string newProfile = _profileNameInputField.text;
            if (_profileManager.AvailableProfiles.Contains(newProfile))
            {
                // Failed to create profile - Already Exists.
                PopupManager.ShowPopupPanel("Could not create new Profile", "A profile already exists with this same name. Select one of the already existing profiles or create a new one.");
                return;
            }

            if (!_profileManager.TryCreateProfile(newProfile))
            {
                // Failed to create profile - Invalid Profile.
                PopupManager.ShowPopupPanel("Could not create new Profile", $"{newProfile} is an invalid profile name. Select one of the already existing profiles or create a new one.");
                return;
            }

            if (!_profileManager.TrySetProfile(newProfile))
            {
                // Failed to set profile.
                PopupManager.ShowPopupPanel("Could not set Profile", "Select another existing profile or create a new one.");
                return;
            }

            MenuManager.CloseMenu(this);
        }
    }
}