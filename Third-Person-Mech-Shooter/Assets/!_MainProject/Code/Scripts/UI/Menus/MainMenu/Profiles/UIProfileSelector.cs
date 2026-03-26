using Gameplay.UI.Popups;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using VContainer;

namespace Gameplay.UI.Menus
{
    public class UIProfileSelector : Menu
    {
        [Space(5)]
        [SerializeField] private ProfileListItemUI _profileListItemPrototype;
        [SerializeField] private Graphic _emptyProfileListLabel;

        [Space(5)]
        [SerializeField] private Menu _createProfileMenu;


        private List<ProfileListItemUI> _profileListItems = new List<ProfileListItemUI>();

        [Inject]
        private IObjectResolver _resolver;
        [Inject]
        private ProfileManager _profileManager;



        private void Awake()
        {
            _profileListItemPrototype.gameObject.SetActive(false);
            Hide();
        }
        public override void Show()
        {
            base.Show();
            UpdateUI();
        }


        public void UpdateUI()
        {
            // Create & Setup UI Slots, instantiating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(_profileManager.AvailableProfiles.Count);
            for(int i = 0; i < _profileManager.AvailableProfiles.Count; ++i)
            {
                string profileName = _profileManager.AvailableProfiles[i];
                _profileListItems[i].SetProfileName(profileName);
            }

            HighlightSelectedProfile();

            // Toggle empty list label as required.
            _emptyProfileListLabel.enabled = _profileManager.AvailableProfiles.Count == 0;
        }

        /// <summary>
        ///     Ensure that there are the required number of <see cref="ProfileListItemUI"/> instances.<br/>
        ///     Creates new instances to reach the required amount and disables those over the count.
        /// </summary>
        private void EnsureNumberOfActiveUISlots(int requiredNumber)
        {
            // Create required new instances.
            int delta = requiredNumber - _profileListItems.Count;
            for (int i = 0; i < delta; i++)
            {
                CreateProfileListItem();
            }

            // Enable/Disable instances as required.
            for (int i = 0; i < _profileListItems.Count; i++)
            {
                _profileListItems[i].gameObject.SetActive(i < requiredNumber);
            }
        }


        /// <summary>
        ///     Create a new Profile List item from our prototype.
        /// </summary>
        private void CreateProfileListItem()
        {
            ProfileListItemUI listItem = Instantiate(_profileListItemPrototype.gameObject, _profileListItemPrototype.transform.parent).GetComponent<ProfileListItemUI>();
            _profileListItems.Add(listItem);
            listItem.gameObject.SetActive(true);
            _resolver.Inject(listItem);
        }


        private void HighlightSelectedProfile()
        {
            for(int i = 0; i < _profileListItems.Count; ++i)
            {
                if (_profileListItems[i].ProfileName == _profileManager.Profile)
                {
                    _profileListItems[i].MarkSelected();
                }
                else
                {
                    _profileListItems[i].MarkUnselected();
                }
            }
        }

        public void SelectProfile(string profileName)
        {
            if (_profileManager.TrySetProfile(profileName))
            {
                HighlightSelectedProfile();
                Hide();
            }
            else
            {
                PopupManager.ShowDefaultPopup("Could not set Profile", $"{profileName} is an invalid profile for this build. Select another existing profile or create a new one.");
            }
        }
        public void DeleteProfile(string profileName)
        {
            PopupManager.ShowPopup($"Delete '{profileName}'?", "",
                new PopupButtonParameters("Cancel", null),
                new PopupButtonParameters("Delete", OnDelete));

            void OnDelete()
            {
                _profileManager.DeleteProfile(profileName);
                UpdateUI();
            }
        }

        public void OpenCreateProfileUI() => MenuManager.OpenChildMenu(_createProfileMenu, null, this);
    }
}