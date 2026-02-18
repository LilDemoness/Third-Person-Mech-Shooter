using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI.CoroutineTween;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Multiselect Dropdown")]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    ///     A otherwise standard dropdown that presents a list of options when clicked, of which multiple can be chosen.
    /// </summary>
    /// <remarks>
    ///     The dropdown component is a Selectable. When an option is chosen, the label and/or image of the control changes to show the chosen option.</br>
    ///     When a dropdown event occurs a callback is sent to any registered listeners of onValueChanged.
    /// </remarks>
    // Adapted from: 'https://github.com/chriscow/UnityEngine.UI.DropdownEx/'.
    public class MultiselectDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
#if UNITY_EDITOR

        [MenuItem("GameObject/UI/Multiselect Dropdown")]
        public static void AddMultiselectDropdown()
        {
            const string MULTISELECT_DROPDOWN_RESOURCES_PATH = "UI/MultiselectDropdown";
            GameObject gameObject = Instantiate(Resources.Load<GameObject>(MULTISELECT_DROPDOWN_RESOURCES_PATH));
            gameObject.transform.SetParent(Selection.activeGameObject.transform, false);
        }

#endif



        /// <summary>
        ///     Visual representation of OptionData
        /// </summary>
        protected internal class DropdownItem : MonoBehaviour, IPointerEnterHandler, ICancelHandler
        {
            [SerializeField] private TMP_Text m_Text;
            [SerializeField] private Image m_Image;
            [SerializeField] private RectTransform m_RectTransform;
            [SerializeField] private Toggle m_Toggle;

            public TMP_Text Text { get { return m_Text; } set { m_Text = value; } }
            public Image Image { get { return m_Image; } set { m_Image = value; } }
            public RectTransform RectTransform { get { return m_RectTransform; } set { m_RectTransform = value; } }
            public Toggle Toggle { get { return m_Toggle; } set { m_Toggle = value; } }

            public virtual void OnPointerEnter(PointerEventData eventData)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }

            public virtual void OnCancel(BaseEventData eventData)
            {
                Dropdown dropdown = GetComponentInParent<Dropdown>();
                if (dropdown)
                    dropdown.Hide();
            }
        }

        /// <summary>
        ///     Class to store the text and/or image of a single option in the dropdown list.
        /// </summary>
        [System.Serializable]
        public class OptionData
        {
            [SerializeField]
            private string m_Text;
            [SerializeField]
            private Sprite m_Image;
            [SerializeField]
            private bool m_Selected;

            /// <summary>
            /// The text associated with the option.
            /// </summary>
            public string text { get { return m_Text; } internal set { m_Text = value; } }

            /// <summary>
            /// The image associated with the option.
            /// </summary>
            public Sprite image { get { return m_Image; } internal set { m_Image = value; } }

            public bool selected { get { return m_Selected; } internal set { m_Selected = value; } }

            public OptionData() { }

            public OptionData(string text)
            {
                this.text = text;
            }

            public OptionData(Sprite image)
            {
                this.image = image;
            }

            /// <summary>
            /// Create an object representing a single option for the dropdown list.
            /// </summary>
            /// <param name="text">Optional text for the option.</param>
            /// <param name="image">Optional image for the option.</param>
            public OptionData(string text, Sprite image)
            {
                this.text = text;
                this.image = image;
            }

            public OptionData(string text, bool selected)
            {
                this.text = text;
                this.selected = selected;
            }

            public OptionData(string text, Sprite image, bool selected)
            {
                this.text = text;
                this.image = image;
                this.selected = selected;
            }
        }


        /// <summary>
        ///     UnityEvent callback for when a dropdown current option is changed.
        /// </summary>
        [System.Serializable]
        public class DropdownEvent : UnityEvent<uint> { }

        // Template used to create the dropdown.
        [SerializeField] private RectTransform m_template;

        /// <summary>
        /// The Rect Transform of the template for the dropdown list.
        /// </summary>
        public RectTransform Template { get { return m_template; } set { m_template = value; RefreshShownValue(); } }

        // Text to be used as a caption for the current value. It's not required, but it's kept here for convenience.
        [SerializeField] private TMP_Text m_captionText;

        /// <summary>
        /// The Text component to hold the text of the currently selected option.
        /// </summary>
        public TMP_Text CaptionText { get { return m_captionText; } set { m_captionText = value; RefreshShownValue(); } }

        [SerializeField] private Image m_captionImage;

        /// <summary>
        /// The Image component to hold the image of the currently selected option.
        /// </summary>
        public Image CaptionImage { get { return m_captionImage; } set { m_captionImage = value; RefreshShownValue(); } }


        [Space]
        [SerializeField] private TMP_Text m_itemText;

        /// <summary>
        /// The Text component to hold the text of the item.
        /// </summary>
        public TMP_Text ItemText { get { return m_itemText; } set { m_itemText = value; RefreshShownValue(); } }

        [SerializeField] private Image m_itemImage;

        /// <summary>
        /// The Image component to hold the image of the item
        /// </summary>
        public Image ItemImage { get { return m_itemImage; } set { m_itemImage = value; RefreshShownValue(); } }


        [Space]
        [SerializeField] private uint m_value;


        [Header("Multi-Select Support")]
        public string NothingSelectedText = "Nothing Selected";


        [Space]
        // Items that will be visible when the dropdown is shown.
        // We box this into its own class so we can use a Property Drawer for it.
        [SerializeField] private List<OptionData> m_options = new List<OptionData>();

        /// <summary>
        ///     The list of possible options. A text string and an image can be specified for each option.
        /// </summary>
        /// <remarks>
        ///     This is the list of options within the Dropdown. Each option contains Text and/or image data that you can specify using UI.Dropdown.OptionData before adding to the Dropdown list.
        ///     This also unlocks the ability to edit the Dropdown, including the insertion, removal, and finding of options, as well as other useful tools
        /// </remarks>
        public IReadOnlyList<OptionData> Options
        {
            get { return m_options; }
            // protected set { m_Options = value; RefreshShownValue(); }
        }

        // Notification triggered when the dropdown changes.
        [Space]
        [SerializeField]
        private DropdownEvent m_onValueChanged = new DropdownEvent();

        [SerializeField]
        private DropdownEvent m_onItemSelected = new DropdownEvent();
        [SerializeField]
        private DropdownEvent m_onItemDeselected = new DropdownEvent();

        /// <summary>
        ///     A UnityEvent that is invoked when when a user has clicked one of the options in the dropdown list.
        /// </summary>
        /// <remarks>
        ///     Use this to detect when a user selects one or more options in the Dropdown. Add a listener to perform an action when this UnityEvent detects a selection by the user. See https://unity3d.com/learn/tutorials/topics/scripting/delegates for more information on delegates.
        /// </remarks>
        public DropdownEvent onValueChanged { get { return m_onValueChanged; } set { m_onValueChanged = value; } }
        public DropdownEvent onItemSelected { get { return m_onItemSelected; } set { m_onItemSelected = value; } }
        public DropdownEvent onItemDeselected { get { return m_onItemDeselected; } set { m_onItemDeselected = value; } }

        private GameObject m_dropdown;
        private GameObject m_blocker;
        private List<DropdownItem> m_items = new List<DropdownItem>();
        private TweenRunner<FloatTween> m_alphaTweenRunner;
        private bool _validTemplate = false;

        private static OptionData s_noOptionData = new OptionData();

        /// <summary>
        /// The Value is the index number of the current selection in the Dropdown. 0 is the first option in the Dropdown, 1 is the second, and so on.
        /// 
        /// In Multi-Select mode, value will contain a bitmask of the selected items.  
        /// A value of zero  means nothing is selected.
        /// A value of 1 means the first item is selected.
        /// A value of 3 means the first AND second items are selected.
        /// </summary>
        public uint Value
        {
            get
            {
                return m_value;
            }

            set
            {
                if (Application.isPlaying && (value == m_value || Options.Count == 0))
                    return;

                // If we invert m_Value and mask it with 'value'
                // we will have the bits that have changed
                //
                // Here's how this works:
                //
                // so lets say m_Value = 5  00000101
                // and lets say value is 6  00000110
                // so options[1] is added and options[0] is removed

                // ~m_Value & value == 11111010 &
                //                     00000110
                //                     --------
                //                     00000010 <-- index 1 (option #2)

                // m_Value & ~value == 00000101
                //                     11111001
                //                     --------
                //                     00000001 <-- index 0 (option #1)

                uint added_mask = ~m_value & value;
                uint removed_mask = m_value & ~value;
                UpdateOptionsState(added_mask, removed_mask);

                m_value = value;
                RefreshShownValue();

                // Notify all listeners
                UISystemProfilerApi.AddMarker("DropdownEx.value", this);
                m_onValueChanged.Invoke(m_value);
            }
        }

        protected virtual void UpdateOptionsState(uint added, uint removed)
        {
            uint index = 0;
            while (added > 0)
            {
                if ((added & 0x01) == 0x01)
                {
                    Options[(int)index].selected = true;
                    m_onItemSelected.Invoke(index);
                }

                index++;
                added >>= 0x01;
            }

            index = 0;
            while (removed > 0)
            {
                if ((removed & 0x01) == 0x01)
                {
                    Options[(int)index].selected = false;
                    m_onItemDeselected.Invoke(index);
                }

                index++;
                removed >>= 0x01;
            }
        }

        public IEnumerable<OptionData> SelectedOptions
        {
            get
            {
                foreach (var option in Options)
                    if (option.selected)
                        yield return option;
            }
        }

        public uint SelectedCount
        {
            get
            {
                return CountBits(m_value);
            }
        }

        private uint IndexOfBit(uint src)
        {
            var i = 0u;
            while (src > 1)
            {
                src >>= 1;
                i++;
            }
            return i;
        }

        private uint CountBits(uint v)
        {
            uint c; // c accumulates the total bits set in v
            for (c = 0; v > 0; c++)
            {
                v &= v - 1; // clear the least significant bit set
            }
            return c;
        }

        protected Toggle GetToggleForIndex(int i)
        {
            Show();
            string caption = string.IsNullOrEmpty(Options[i].text) ? "" : Options[i].text;

            var go = GameObject.Find(string.Format("Item {0}: {1}", i, caption));
            return go.GetComponent<Toggle>();
        }

        protected MultiselectDropdown() { }

        protected override void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            m_alphaTweenRunner = new TweenRunner<FloatTween>();
            m_alphaTweenRunner.Init(this);

            if (m_captionImage)
                m_captionImage.enabled = (m_captionImage.sprite != null);

            if (m_template)
                m_template.gameObject.SetActive(false);
        }

        protected override void Start()
        {
            base.Start();

            RefreshShownValue();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (!IsActive())
                return;

            // clamp at zero in case there are no options in the list
            uint maxValue = Math.Max(0, ((uint)Options.Count) - 1);
            maxValue = (1u << Options.Count) - 1;

            m_value = m_value > maxValue ? maxValue : m_value;

            RefreshShownValue();
        }

#endif

        /// <summary>
        ///     Refreshes the text and image (if available) of the currently selected option.
        /// </summary>
        /// <remarks>
        ///     If you have modified the list of options, you should call this method afterwards to ensure that the visual state of the dropdown corresponds to the updated options.
        /// </remarks>
        public void RefreshShownValue()
        {
            if (Options.Count == 0)
            {
                // No available options.

                if (m_captionText != null)
                    m_captionText.text = !string.IsNullOrEmpty(s_noOptionData.text) ? s_noOptionData.text : "";

                if (m_captionImage != null)
                {
                    m_captionImage.sprite = s_noOptionData.image;
                    m_captionImage.enabled = (m_captionImage.sprite != null);
                }
            }
            else
            {
                OptionData data = null;

                uint itemCount = 1;

                // clear out selections
                for (int i = 0; i < Options.Count; i++)
                    Options[i].selected = false;

                // bitcount is the number of bits set in 'value', which
                // represents the number of options selected
                itemCount = this.SelectedCount;

                //
                // If a single option is selected, figure out the index
                // in the options array it is associated with
                //
                if (1 == itemCount) // only one option selected
                {
                    // Shift the single bit over to determine the option index
                    var i = (int)IndexOfBit(m_value);

                    data = Options[i];
                    data.selected = true;
                }
                else
                {
                    for (int i = 0; i < Options.Count; i++)
                    {
                        // In case you aren't familiar ...
                        // The mask has one bit set, moving from least to most significant.
                        // i == 0,  mask == 00000001
                        // i == 1,  mask == 00000010
                        // i == 2,  mask == 00000100
                        //
                        // Logical AND m_Value with mask will equal the mask value if that
                        // bit is set.  So if:
                        // m_Value == 5      00000101
                        // and mask == 4     00000100
                        // Then ANDing them: 00000100
                        // Notice: mask == (m_Value & mask) 
                        int mask = 1 << i;
                        Options[i].selected = ((m_value & mask) == mask);
                    }
                }

                //
                // Depending on the number of options selected, set the
                // displayed caption text and image
                //
                if (m_captionText != null)
                {
                    if (itemCount == 0)
                    {
                        m_captionText.text = !string.IsNullOrEmpty(NothingSelectedText) ? NothingSelectedText : "";
                    }
                    else
                    {
                        // Note: We could separate this for a single selected option check for improved performance in that scenario, but to reduce code duplication we're not.
                        // Compose the display text.
                        string displayText = null;
                        for (int i = 0; i < Options.Count; ++i)
                        {
                            if (Options[i].selected)
                            {
                                if (displayText == null)
                                    displayText = Options[i].text;  // First Selected Option.
                                else
                                    displayText += ", " + Options[i].text;  // Not the first selected option. (Include a separator).
                            }
                        }

                        // Update the display text.
                        m_captionText.text = displayText;
                    }
                }

                if (m_captionImage != null)
                {
                    if (0 == itemCount)
                    {
                        m_captionImage.sprite = null;
                        m_captionImage.enabled = false;
                    }
                    else if (1 == itemCount)
                    {
                        m_captionImage.sprite = data.image;
                        m_captionImage.enabled = (m_captionImage.sprite != null);
                    }
                    else
                    {
                        m_captionImage.sprite = null;
                        m_captionImage.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        ///     Add multiple options to the options of the Dropdown based on a list of OptionData objects.
        /// </summary>
        /// <param name="options"> The list of OptionData to add.</param>
        public void AddOptions(IEnumerable<OptionData> options)
        {
            this.m_options.AddRange(options);
            RefreshShownValue();
        }

        /// <summary>
        ///     Add multiple text-only options to the options of the Dropdown based on a list of strings.
        /// </summary>
        /// <remarks>
        ///     Add a List of string messages to the Dropdown. The Dropdown shows each member of the list as a separate option.
        /// </remarks>
        /// <param name="options"> The list of text strings to add.</param>
        public void AddOptions(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
                this.m_options.Add(new OptionData(options[i]));
            RefreshShownValue();
        }

        /// <summary>
        ///     Add multiple image-only options to the options of the Dropdown based on a list of Sprites.
        /// </summary>
        /// <param name="options"> The list of Sprites to add.</param>
        public void AddOptions(List<Sprite> options)
        {
            for (int i = 0; i < options.Count; i++)
                this.m_options.Add(new OptionData(options[i]));
            RefreshShownValue();
        }

        /// <summary>
        ///     Clear the list of options in the Dropdown.
        /// </summary>
        public void ClearOptions()
        {
            m_options.Clear();
            RefreshShownValue();
        }

        private void SetupTemplate()
        {
            _validTemplate = false;

            if (!m_template)
            {
                Debug.LogError("The dropdown template is not assigned. The template needs to be assigned and must have a child GameObject with a Toggle component serving as the item.", this);
                return;
            }

            GameObject templateGo = m_template.gameObject;
            templateGo.SetActive(true);
            Toggle itemToggle = m_template.GetComponentInChildren<Toggle>();

            _validTemplate = true;
            if (!itemToggle || itemToggle.transform == Template)
            {
                _validTemplate = false;
                Debug.LogError("The dropdown template is not valid. The template must have a child GameObject with a Toggle component serving as the item.", Template);
            }
            else if (!(itemToggle.transform.parent is RectTransform))
            {
                _validTemplate = false;
                Debug.LogError("The dropdown template is not valid. The child GameObject with a Toggle component (the item) must have a RectTransform on its parent.", Template);
            }
            else if (ItemText != null && !ItemText.transform.IsChildOf(itemToggle.transform))
            {
                _validTemplate = false;
                Debug.LogError("The dropdown template is not valid. The Item Text must be on the item GameObject or children of it.", Template);
            }
            else if (ItemImage != null && !ItemImage.transform.IsChildOf(itemToggle.transform))
            {
                _validTemplate = false;
                Debug.LogError("The dropdown template is not valid. The Item Image must be on the item GameObject or children of it.", Template);
            }

            if (!_validTemplate)
            {
                templateGo.SetActive(false);
                return;
            }

            DropdownItem item = itemToggle.gameObject.AddComponent<DropdownItem>();
            item.Text = m_itemText;
            item.Image = m_itemImage;
            item.Toggle = itemToggle;
            item.RectTransform = (RectTransform)itemToggle.transform;

            Canvas popupCanvas = GetOrAddComponent<Canvas>(templateGo);
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 30000;

            GetOrAddComponent<GraphicRaycaster>(templateGo);
            GetOrAddComponent<CanvasGroup>(templateGo);
            templateGo.SetActive(false);

            _validTemplate = true;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (!comp)
                comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>
        ///     Handling for when the dropdown is initially 'clicked'. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData"> The asocciated event data.</param>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            Show();
        }

        /// <summary>
        ///     Handling for when the dropdown is selected and a submit event is processed. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData"> The asocciated event data.</param>
        public virtual void OnSubmit(BaseEventData eventData)
        {
            Show();
        }

        /// <summary>
        ///     This will hide the dropdown list.
        /// </summary>
        /// <remarks>
        ///     Called by a BaseInputModule when a Cancel event occurs.
        /// </remarks>
        /// <param name="eventData"> The asocciated event data.</param>
        public virtual void OnCancel(BaseEventData eventData)
        {
            Hide();
        }

        /// <summary>
        ///     Show the dropdown.
        ///
        ///     Plan for dropdown scrolling to ensure dropdown is contained within screen.
        ///
        ///     We assume the Canvas is the screen that the dropdown must be kept inside.
        ///     This is always valid for screen space canvas modes.
        ///     For world space canvases we don't know how it's used, but it could be e.g. for an in-game monitor.
        ///     We consider it a fair constraint that the canvas must be big enough to contain dropdowns.
        /// </summary>
        public void Show()
        {
            if (!IsActive() || !IsInteractable() || m_dropdown != null)
                return;

            if (!_validTemplate)
            {
                SetupTemplate();
                if (!_validTemplate)
                    return;
            }

            // Get root Canvas.
            var list = ListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count == 0)
                return;
            Canvas rootCanvas = list[0];
            ListPool<Canvas>.Release(list);

            m_template.gameObject.SetActive(true);

            // Instantiate the drop-down template
            m_dropdown = CreateDropdownList(m_template.gameObject);
            m_dropdown.name = "Dropdown List";
            m_dropdown.SetActive(true);

            // Make drop-down RectTransform have same values as original.
            RectTransform dropdownRectTransform = m_dropdown.transform as RectTransform;
            dropdownRectTransform.SetParent(m_template.transform.parent, false);

            // Instantiate the drop-down list items

            // Find the dropdown item and disable it.
            DropdownItem itemTemplate = m_dropdown.GetComponentInChildren<DropdownItem>();

            GameObject content = itemTemplate.RectTransform.parent.gameObject;
            RectTransform contentRectTransform = content.transform as RectTransform;
            itemTemplate.RectTransform.gameObject.SetActive(true);

            // Get the rects of the dropdown and item
            Rect dropdownContentRect = contentRectTransform.rect;
            Rect itemTemplateRect = itemTemplate.RectTransform.rect;

            // Calculate the visual offset between the item's edges and the background's edges
            Vector2 offsetMin = itemTemplateRect.min - dropdownContentRect.min + (Vector2)itemTemplate.RectTransform.localPosition;
            Vector2 offsetMax = itemTemplateRect.max - dropdownContentRect.max + (Vector2)itemTemplate.RectTransform.localPosition;
            Vector2 itemSize = itemTemplateRect.size;

            m_items.Clear();

            Toggle prev = null;
            for (int i = 0; i < Options.Count; ++i)
            {
                OptionData data = Options[i];
                // var selected = (this.value & (1 << i)) == (1 << i);
                DropdownItem item = AddItem(data, itemTemplate, m_items);
                if (item == null)
                    continue;

                // Automatically set up a toggle state change listener
                item.Toggle.isOn = data.selected;
                item.Toggle.onValueChanged.AddListener(x => OnSelectItem(item.Toggle));

                // Select current option
                if (item.Toggle.isOn)
                    item.Toggle.Select();

                // Automatically set up explicit navigation
                if (prev != null)
                {
                    Navigation prevNav = prev.navigation;
                    Navigation toggleNav = item.Toggle.navigation;
                    prevNav.mode = Navigation.Mode.Explicit;
                    toggleNav.mode = Navigation.Mode.Explicit;

                    prevNav.selectOnDown = item.Toggle;
                    prevNav.selectOnRight = item.Toggle;
                    toggleNav.selectOnLeft = prev;
                    toggleNav.selectOnUp = prev;

                    prev.navigation = prevNav;
                    item.Toggle.navigation = toggleNav;
                }
                prev = item.Toggle;
            }

            // Reposition all items now that all of them have been added
            Vector2 sizeDelta = contentRectTransform.sizeDelta;
            sizeDelta.y = itemSize.y * m_items.Count + offsetMin.y - offsetMax.y;
            contentRectTransform.sizeDelta = sizeDelta;

            float extraSpace = dropdownRectTransform.rect.height - contentRectTransform.rect.height;
            if (extraSpace > 0)
                dropdownRectTransform.sizeDelta = new Vector2(dropdownRectTransform.sizeDelta.x, dropdownRectTransform.sizeDelta.y - extraSpace);

            // Invert anchoring and position if dropdown is partially or fully outside of canvas rect.
            // Typically this will have the effect of placing the dropdown above the button instead of below,
            // but it works as inversion regardless of initial setup.
            Vector3[] corners = new Vector3[4];
            dropdownRectTransform.GetWorldCorners(corners);

            RectTransform rootCanvasRectTransform = rootCanvas.transform as RectTransform;
            Rect rootCanvasRect = rootCanvasRectTransform.rect;
            for (int axis = 0; axis < 2; axis++)
            {
                bool outside = false;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = rootCanvasRectTransform.InverseTransformPoint(corners[i]);
                    if (corner[axis] < rootCanvasRect.min[axis] || corner[axis] > rootCanvasRect.max[axis])
                    {
                        outside = true;
                        break;
                    }
                }
                if (outside)
                    RectTransformUtility.FlipLayoutOnAxis(dropdownRectTransform, axis, false, false);
            }

            for (int i = 0; i < m_items.Count; i++)
            {
                RectTransform itemRect = m_items[i].RectTransform;
                itemRect.anchorMin = new Vector2(itemRect.anchorMin.x, 0);
                itemRect.anchorMax = new Vector2(itemRect.anchorMax.x, 0);
                itemRect.anchoredPosition = new Vector2(itemRect.anchoredPosition.x, offsetMin.y + itemSize.y * (m_items.Count - 1 - i) + itemSize.y * itemRect.pivot.y);
                itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemSize.y);
            }

            // Fade in the popup
            AlphaFadeList(0.15f, 0f, 1f);

            // Make drop-down template and item template inactive
            m_template.gameObject.SetActive(false);
            itemTemplate.gameObject.SetActive(false);

            m_blocker = CreateBlocker(rootCanvas);
        }

        /// <summary>
        ///     Create a blocker that blocks clicks to other controls while the dropdown list is open.
        /// </summary>
        /// <remarks>
        ///     Override this method to implement a different way to obtain a blocker GameObject.
        /// </remarks>
        /// <param name="rootCanvas">The root canvas the dropdown is under.</param>
        /// <returns> The created blocker object</returns>
        protected virtual GameObject CreateBlocker(Canvas rootCanvas)
        {
            // Create blocker GameObject.
            GameObject blocker = new GameObject("Blocker");

            // Setup blocker RectTransform to cover entire root canvas area.
            RectTransform blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.SetParent(rootCanvas.transform, false);
            blockerRect.anchorMin = Vector3.zero;
            blockerRect.anchorMax = Vector3.one;
            blockerRect.sizeDelta = Vector2.zero;

            // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
            Canvas blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            Canvas dropdownCanvas = m_dropdown.GetComponent<Canvas>();
            blockerCanvas.sortingLayerID = dropdownCanvas.sortingLayerID;
            blockerCanvas.sortingOrder = dropdownCanvas.sortingOrder - 1;

            // Add raycaster since it's needed to block.
            blocker.AddComponent<GraphicRaycaster>();

            // Add image since it's needed to block, but make it clear.
            Image blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = Color.clear;

            // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
            Button blockerButton = blocker.AddComponent<Button>();
            blockerButton.onClick.AddListener(Hide);

            return blocker;
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated blocker object
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a blocker GameObject that blocks clicks to other controls while the dropdown list is open.
        /// </remarks>
        /// <param name="blocker">The blocker object to destroy.</param>
        protected virtual void DestroyBlocker(GameObject blocker)
        {
            Destroy(blocker);
        }

        /// <summary>
        /// Create the dropdown list to be shown when the dropdown is clicked. 
        /// The dropdown list should correspond to the provided template GameObject, 
        /// equivalent to instantiating a copy of it.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain a dropdown 
        /// list GameObject.
        /// </remarks>
        /// <param name="template">The template to create the dropdown list from.</param>
        /// <returns>The created drop down list gameobject.</returns>
        protected virtual GameObject CreateDropdownList(GameObject template)
        {
            return (GameObject)Instantiate(template);
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated dropdown list
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a dropdown list GameObject.
        /// </remarks>
        /// <param name="dropdownList">The dropdown list GameObject to destroy</param>
        protected virtual void DestroyDropdownList(GameObject dropdownList)
        {
            Destroy(dropdownList);
        }

        /// <summary>
        /// Create a dropdown item based upon the item template.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain an option item.
        /// The option item should correspond to the provided template DropdownItem and its GameObject, equivalent to instantiating a copy of it.
        /// </remarks>
        /// <param name="itemTemplate">e template to create the option item from.</param>
        /// <returns>The created dropdown item component</returns>
        protected virtual DropdownItem CreateItem(DropdownItem itemTemplate)
        {
            return (DropdownItem)Instantiate(itemTemplate);
        }

        /// <summary>
        ///  Convenience method to explicitly destroy the previously generated Items.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of an option item.
        /// Likely no action needed since destroying the dropdown list destroys all contained items as well.
        /// </remarks>
        /// <param name="item">The Item to destroy.</param>
        protected virtual void DestroyItem(DropdownItem item) { }

        // Add a new drop-down list item with the specified values.
        private DropdownItem AddItem(OptionData data, DropdownItem itemTemplate, List<DropdownItem> items)
        {
            // Add a new item to the dropdown.
            DropdownItem item = CreateItem(itemTemplate);
            item.RectTransform.SetParent(itemTemplate.RectTransform.parent, false);

            item.gameObject.SetActive(true);
            item.gameObject.name = "Item " + items.Count + (data.text != null ? ": " + data.text : "");

            if (item.Toggle != null)
            {
                item.Toggle.isOn = data.selected;
            }

            // Set the item's data
            if (item.Text)
                item.Text.text = data.text;
            if (item.Image)
            {
                item.Image.sprite = data.image;
                item.Image.enabled = (item.Image.sprite != null);
            }

            items.Add(item);
            return item;
        }

        private void AlphaFadeList(float duration, float alpha)
        {
            CanvasGroup group = m_dropdown.GetComponent<CanvasGroup>();
            AlphaFadeList(duration, group.alpha, alpha);
        }

        private void AlphaFadeList(float duration, float start, float end)
        {
            if (end.Equals(start))
                return;

            FloatTween tween = new FloatTween { duration = duration, startValue = start, targetValue = end };
            tween.AddOnChangedCallback(SetAlpha);
            tween.ignoreTimeScale = true;
            m_alphaTweenRunner.StartTween(tween);
        }

        private void SetAlpha(float alpha)
        {
            if (!m_dropdown)
                return;
            CanvasGroup group = m_dropdown.GetComponent<CanvasGroup>();
            group.alpha = alpha;
        }

        /// <summary>
        ///     Hide the dropdown list. I.e. close it.
        /// </summary>
        public void Hide()
        {
            if (m_dropdown != null)
            {
                AlphaFadeList(0.15f, 0f);

                // User could have disabled the dropdown during the OnValueChanged call.
                if (IsActive())
                    StartCoroutine(DelayedDestroyDropdownList(0.15f));
            }
            if (m_blocker != null)
                DestroyBlocker(m_blocker);
            m_blocker = null;
            Select();
        }

        private IEnumerator DelayedDestroyDropdownList(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            for (int i = 0; i < m_items.Count; i++)
            {
                if (m_items[i] != null)
                    DestroyItem(m_items[i]);
            }
            m_items.Clear();
            if (m_dropdown != null)
                DestroyDropdownList(m_dropdown);
            m_dropdown = null;
        }

        // Change the value and hide the dropdown.
        private void OnSelectItem(Toggle toggle)
        {
            int selectedIndex = -1;
            Transform tr = toggle.transform;
            Transform parent = tr.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) == tr)
                {
                    // Subtract one to account for template child.
                    selectedIndex = i - 1;
                    break;
                }
            }

            if (selectedIndex < 0)
                return;

            // Update our Value (Options[n].Selected is set in 'RefreshShownValue', which is called when 'Value' is changed).
            if (toggle.isOn)
            {
                Value |= 1u << selectedIndex;
            }
            else
            {
                Value &= ~(1u << selectedIndex);
            }
        }

        public void DeselectAll() => Value = 0;
    }
}
