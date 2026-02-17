using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;

/*#if UNITY_EDITOR
using System.Security.Cryptography;
using System.Text;
#endif*/

namespace Utils
{
    /// <summary>
    ///     Manages the various Profiles of a client
    /// </summary>
    public class ProfileManager
    {
        public const string AUTH_PROFILE_COMMAND_LINE_ARGUMENT = "-AuthProfile";
        private const string EDITOR_PROFILE_NAME = "EDITOR";
        private static IEnumerable<string> PROTECTED_PROFILES = new HashSet<string>
        {
            EDITOR_PROFILE_NAME
        };


        private string _profile;
        public string Profile
        {
            get
            {
                if (_profile == null)
                    _profile = GetDefaultProfile();

                return _profile;
            }
            private set
            {
                _profile = value;
                OnProfileChanged?.Invoke();
            }
        }


        public event System.Action OnProfileChanged;

        private List<string> _availableProfiles;
        public ReadOnlyCollection<string> AvailableProfiles
        {
            get
            {
                if (_availableProfiles == null)
                    LoadProfiles();

                return _availableProfiles.AsReadOnly();
            }
        }


        public bool TryCreateProfile(string profile)
        {
            if (!IsValidNewProfileName(profile))
                return false;

            _availableProfiles.Add(profile);
            SaveProfiles();
            return true;
        }
        public void DeleteProfile(string profile)
        {
            if (PROTECTED_PROFILES.Contains(profile))
                return;

            _availableProfiles.Remove(profile);
            SaveProfiles();
        }
        public bool IsValidNewProfileName(string profileName) => !PROTECTED_PROFILES.Contains(profileName);


        private static string GetDefaultProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            for(int i = 0; i < arguments.Length; ++i)
            {
                if (arguments[i] == AUTH_PROFILE_COMMAND_LINE_ARGUMENT)
                {
                    var profileId = arguments[i + 1];
                    return profileId;
                }
            }

            #if UNITY_EDITOR

            /*// While running in the Editor, make a unique ID from the Application.dataPath.
            // This will work for cloning projects manually, or with Virtual projects.
            // "Since only a single instance of the Editor can be open for a specific DataPath, uniqueness is ensured."
            //      Note: This is actually incorrect, but we're not utilising the editor multi-play-mode functions yet so it's fine for now.
            var hashedBytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);

            // The authentication service allows profile names of a maximum 30 characters, but our FixedString32 has a limit of 29 (32 - 3) so we use that instead.
            //  We're generating a GUID based on the project's path.
            //  Truncating the first 29 characters of said GUID string will suffice for uniqueness.
            return new Guid(hashedBytes).ToString("N")[..29];*/
            // Default to the Editor Profile when in the Editor.
            return EDITOR_PROFILE_NAME;

            #else
            // No profile arguments found.
            return "";
            #endif
        }

        /// <summary>
        ///     Save the available user profiles using the <see cref="ClientPrefs"/> wrapper.
        /// </summary>
        private void LoadProfiles()
        {
            _availableProfiles = new List<string>();

            #if UNITY_EDITOR
            _availableProfiles.Add(EDITOR_PROFILE_NAME);
            #endif

            string loadedProfiles = ClientPrefs.GetAvailableProfiles();
            foreach(string profile in loadedProfiles.Split(','))    // This works as we're sanitizing our input strings.
            {
                if (profile.Length <= 0)
                    continue;
                if (profile == EDITOR_PROFILE_NAME)
                    continue;   // Prevent bypassing the editor restriction by not allowing it to be loaded from Prefs, even if the player edited them manually.

                _availableProfiles.Add(profile);
            }
        }
        /// <summary>
        ///     Save the available user profiles using the <see cref="ClientPrefs"/> wrapper.
        /// </summary>
        private void SaveProfiles()
        {
            string profilesToSave = "";
            foreach(string profile in _availableProfiles)
            {
                if (profile == EDITOR_PROFILE_NAME)
                    continue;   // Don't save the editor profile.

                profilesToSave += profile + ",";
            }

            ClientPrefs.SetAvailableProfiles(profilesToSave);
        }


        public bool TrySetProfile(string profileName)
        {
            #if UNITY_EDITOR
            #else
            if (profileName == EDITOR_PROFILE_NAME)
                return false;
            #endif

            Profile = profileName;
            return true;
        }
    }
}