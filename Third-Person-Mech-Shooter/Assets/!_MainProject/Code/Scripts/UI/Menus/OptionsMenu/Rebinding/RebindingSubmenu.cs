using UnityEngine;
using UnityEngine.UI;
using UserInput;

namespace Gameplay.UI.Menus.Options
{
    public class RebindingSubmenu : OptionsSubmenu
    {
        [SerializeField] private Transform _container;

        [SerializeField] private ClientInput.DeviceType _device;
        public ClientInput.DeviceType Device => _device;


        protected override void OnDestroy()
        {
            base.OnDestroy();
            ControlsRebindingValue.Instance.UnsubscribeFromOnValueChanged(OnAnyOptionChanged);
        }
        public override void Open(bool selectFirstElement = true)
        {
            ControlsRebindingValue.Instance.SubscribeToOnValueChanged(OnAnyOptionChanged);
            base.Open(selectFirstElement);
        }
        protected override void FinishClose()
        {
            ControlsRebindingValue.Instance.UnsubscribeFromOnValueChanged(OnAnyOptionChanged);
        }


        // Setup this menu's children's navigation.
        protected override void SetupNavigation()
        {
            int firstSelectableIndex = -1;
            int lastSelectableIndex = -1;
            {
                // Find the first active selectable.
                Selectable firstSelectable = null;
                for (int i = 0; i < _container.childCount; ++i)
                {
                    if (!_container.GetChild(i).gameObject.activeSelf)
                        continue;

                    if (_container.GetChild(i).TryGetComponentInChildren<Selectable>(out firstSelectable))
                    {
                        firstSelectableIndex = i;
                        break;
                    }
                }

                // Check: There are no selectables.
                if (firstSelectableIndex == -1)
                    return;

                SetFirstSelectedElement(firstSelectable);

                // Find the last active selectable.
                Selectable lastSelectable = null;
                for(int i = _container.childCount - 1; i >= 0; --i)
                {
                    if (!_container.GetChild(i).gameObject.activeSelf)
                        continue;

                    if (_container.GetChild(i).TryGetComponentInChildren<Selectable>(out lastSelectable))
                    {
                        lastSelectableIndex = i;
                        break;
                    }
                }

                // Check: There is only 1 selectable.
                if (firstSelectableIndex == lastSelectableIndex)
                    return;


                // Setup the Back & Forward navigations of the First & Last Selectable.
                firstSelectable.AddNavigation(onUp: lastSelectable);
                lastSelectable.AddNavigation(onDown: firstSelectable);
            }


            // Setup Navigation for our children.
            Selectable previousSelectable = null;
            for (int i = firstSelectableIndex; i <= lastSelectableIndex; ++i)
            {
                if (!_container.GetChild(i).gameObject.activeSelf)
                    continue;

                if (_container.GetChild(i).TryGetComponentInChildren<Selectable>(out Selectable selectable))
                {
                    if (previousSelectable != null)
                    {
                        // Setup the navigation of this and the previous selectable.
                        selectable.AddNavigation(onUp: previousSelectable);
                        previousSelectable.AddNavigation(onDown: selectable);
                    }

                    // Cache this selectable for the next.
                    previousSelectable = selectable;
                }
            }
        }


        public override void SaveAllOptionsToPrefs()
        {
            ControlsRebindingValue.Instance.SaveToPrefs();
            base.SaveAllOptionsToPrefs();
        }
        public override void LoadAllOptionsFromPrefs()
        {
            ControlsRebindingValue.Instance.LoadFromPrefs();
            base.LoadAllOptionsFromPrefs();
        }

        public override void ResetAllOptions()
        {
            ControlsRebindingValue.Instance.ResetValue();
            base.ResetAllOptions();
        }
    }
}