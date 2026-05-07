using Cysharp.Threading.Tasks;
using Gameplay.UI.Menus.Profile;
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


        private List<ProfileListItemUI> _profileListItems = new List<ProfileListItemUI>();

        [Inject]
        private IObjectResolver _resolver;


        protected override GameObject FirstSelectedElement => _profileListItems.Count > 0 && _profileListItems[0].gameObject.activeSelf
            ? _profileListItems[0].GetDefaultNavigationTarget()
            : base.FirstSelectedElement;



        protected override void Awake()
        {
            base.Awake();
            _profileListItemPrototype.gameObject.SetActive(false);
            Hide();
        }
        public override void Show()
        {
            base.Show();
            UpdateUI();
        }
        public override async UniTask<bool> Close()
        {
            if (!ProfileManager.HasActiveProfile())
            {
                PopupManager.ShowDefaultPopup(titleText: "Select a Profile", contentText: "You must select a profile in order to close this menu");
                return false;
            }

            return await base.Close();
        }
        public override bool CanBeClosed() => ProfileManager.HasActiveProfile();


        public void UpdateUI()
        {
            int requiredSlotsCount = ProfileManager.AvailableProfiles.Count;

            // Create & Setup UI Slots, instantiating & enabling/disabling as necessary.
            EnsureNumberOfActiveUISlots(requiredSlotsCount);
            for(int i = 0; i < requiredSlotsCount; ++i)
            {
                string profileName = ProfileManager.AvailableProfiles[i];
                _profileListItems[i].SetProfileName(profileName);
            }

            // Indicate which profile is currently active.
            HighlightSelectedProfile();

            // Prepare navigation.
            SetupProfileListNavigation(requiredSlotsCount);


            // Toggle empty list label as required.
            _emptyProfileListLabel.enabled = requiredSlotsCount == 0;
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

        private void SetupProfileListNavigation(int requiredNumber)
        {
            if (requiredNumber == 0)
                return;
            if (requiredNumber == 1)
            {
                _profileListItems[0].ClearNavigation();
                return;
            }

            _profileListItems[0].SetupNavigation(_profileListItems[requiredNumber - 1], _profileListItems[1]);
            _profileListItems[requiredNumber - 1].SetupNavigation(_profileListItems[requiredNumber - 2], _profileListItems[0]);
            
            for (int i = 1; i < requiredNumber - 1; i++)
                _profileListItems[i].SetupNavigation(_profileListItems[i - 1], _profileListItems[i + 1]);
        }


        private void HighlightSelectedProfile()
        {
            for(int i = 0; i < _profileListItems.Count; ++i)
            {
                if (_profileListItems[i].ProfileName == ProfileManager.GetActiveProfile())
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
            if (ProfileManager.TrySetActiveProfile(profileName))
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
                ProfileManager.DeleteProfile(profileName);
                UpdateUI();
            }
        }

        public void OpenCreateProfileUI() => OpenCreateProfileUIUniTask().Forget();
        public async UniTaskVoid OpenCreateProfileUIUniTask()
        {
            await CreateProfileUI.ShowCreatePromptUniTask();
            UpdateUI();
        }
    }
}