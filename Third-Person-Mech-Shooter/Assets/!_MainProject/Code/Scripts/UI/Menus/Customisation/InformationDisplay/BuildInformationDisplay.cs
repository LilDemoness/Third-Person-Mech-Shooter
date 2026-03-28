using UnityEngine;
using TMPro;

namespace Gameplay.UI.Menus.Customisation
{
    /// <summary>
    ///     Displays an overview of a player's current build.
    /// </summary>
    public class BuildInformationDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text _healthLabel;
        [SerializeField] private TMP_Text _speedLabel;
        [SerializeField] private TMP_Text _sizeLabel;
        [SerializeField] private TMP_Text _heatCapLabel;
    }
}