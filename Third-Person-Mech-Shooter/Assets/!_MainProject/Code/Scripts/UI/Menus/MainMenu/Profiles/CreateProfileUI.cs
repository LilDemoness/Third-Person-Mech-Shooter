using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Gameplay.UI.Popups;
using UnityEngine;
using Utils;

namespace Gameplay.UI.Menus.Profile
{
    /// <summary>
    ///     Facilitates opening a popup to create a new profile.
    /// </summary>
    public static class CreateProfileUI
    {
        // Authentication service only accepts profile names of 30 characters or under.
        private const int AUTHENTICATION_MAX_PROFILE_LENGTH = 30;
        private const string SANITISATION_STRING = "[^a-zA-z0-9]";


        public static void ShowCreatePrompt() => ShowCreatePromptUniTask().Forget();
        public static async UniTask<bool> ShowCreatePromptUniTask()
        {
            bool? success = null;

            PopupManager.ShowInputPopup(
                titleText: "Create New Profile",
                bodyText: string.Empty,
                inputPlaceholderText: "New Profile Name",

                onCancelCallback: OnCancel,
                onSubmitCallback: OnSubmit,

                sanitiseTextFunc: SanitiseProfileName,
                isValidFunc: IsValidProfileName
            );

            await UniTask.WaitUntil(() => success.HasValue);

            return success.HasValue;


            void OnCancel() => success = false;
            void OnSubmit(string value) => success = CreateNewProfile(value);
        }


        /// <summary>
        ///     Returns true if the passed profile name is valid. Otherwise, false.
        /// </summary>
        private static bool IsValidProfileName(string profileName) => profileName.Length > 0 && !ProfileManager.AvailableProfiles.Contains(profileName) && ProfileManager.IsValidNewProfileName(profileName);
        /// <summary>
        ///     Sanitises the input string by removing invalid characters and limiting its length.
        /// </summary>
        private static string SanitiseProfileName(string dirtyString)
        {
            string output = Regex.Replace(dirtyString, SANITISATION_STRING, "");
            return output[..System.Math.Min(output.Length, AUTHENTICATION_MAX_PROFILE_LENGTH)];
        }


        private static bool CreateNewProfile(string newProfile)
        {
            if (ProfileManager.AvailableProfiles.Contains(newProfile))
            {
                // Failed to create profile - Already Exists.
                PopupManager.ShowDefaultPopup("Could not create new Profile", "A profile already exists with this same name. Select one of the already existing profiles or create a new one.");
                return false;
            }

            if (!ProfileManager.TryCreateProfile(newProfile))
            {
                // Failed to create profile - Invalid Profile.
                PopupManager.ShowDefaultPopup("Could not create new Profile", $"{newProfile} is an invalid profile name. Select one of the already existing profiles or create a new one.");
                return false;
            }

            if (!ProfileManager.TrySetActiveProfile(newProfile))
            {
                // Failed to set profile.
                PopupManager.ShowDefaultPopup("Could not set Profile", "Select another existing profile or create a new one.");
                return false;
            }

            return true;
        }
    }
}