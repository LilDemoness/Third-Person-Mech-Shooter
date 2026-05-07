using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;
using VContainer;

namespace Gameplay.UI.Menus
{
    public class ProfileListItemUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIProfileSelector _uiProfileSelector;

        [Space(10)]
        [SerializeField] private GameObject _selectionIndicator;
        [SerializeField] private TextMeshProUGUI _profileNameText;


        [Header("Navigation")]
        [SerializeField] private Button _selectProfileButton;
        [SerializeField] private Button _deleteProfileButton;


        public string ProfileName => _profileNameText.text;


        public void SetProfileName(string profileName)
        {
            _profileNameText.text = profileName;
        }


        public void OnSelectButtonPressed() => _uiProfileSelector.SelectProfile(ProfileName);
        public void OnDeleteButtonPressed() => _uiProfileSelector.DeleteProfile(ProfileName);


        public void MarkSelected() => _selectionIndicator.SetActive(true);
        public void MarkUnselected() => _selectionIndicator.SetActive(false);


        /// <summary>
        ///     Sets this Porifle List Item to be selected by the active EventSystem.
        /// </summary>
        public void SelectForNavigation() => EventSystem.current.SetSelectedGameObject(GetDefaultNavigationTarget());
        public GameObject GetDefaultNavigationTarget() => _selectProfileButton.gameObject;

        /// <summary>
        ///     Sets up the external navigation of the Profile List Item.
        /// </summary>
        /// <param name="up"> The list item above this element.</param>
        /// <param name="down"> The list item below this element.</param>
        public void SetupNavigation(ProfileListItemUI up, ProfileListItemUI down)
        {
            _selectProfileButton.SetNavigation(
                onRight: _deleteProfileButton,
                onUp: up._selectProfileButton,
                onDown: down._selectProfileButton 
            );

            _deleteProfileButton.SetNavigation(
                onLeft: _selectProfileButton,
                onUp: up._deleteProfileButton,
                onDown: down._deleteProfileButton
            );
        }
        /// <summary>
        ///     Clears the external navigation of the Profile List Item, maintaining its internal navigation.
        /// </summary>
        public void ClearNavigation()
        {
            _selectProfileButton.SetNavigation(onRight: _deleteProfileButton);
            _deleteProfileButton.SetNavigation(onLeft: _selectProfileButton);
        }
    }
}