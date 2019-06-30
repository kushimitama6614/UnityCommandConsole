using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UnityCommandConsole : MonoBehaviour
{
    public delegate void CommandDelegate(params object[] args);
    public List<Command> CommandsList;
    public Dictionary<string, CommandDelegate> Commands;
    public Dictionary<KeyCode, CommandDelegate> Hotkeys;
    public bool Active { get; private set; }

    public class Command
    {
        public string command;
        public CommandDelegate function;
        public KeyCode hotkey;
        public string description;

        public Command(string cmd, CommandDelegate func, KeyCode key = KeyCode.None, string desc = "")
        {
            command = cmd;
            function = func;
            hotkey = key;
            description = desc;
        }
    }

    public static UnityCommandConsole Instance { get; private set; }

    private static UnityCommandConsole.Resources s_StandardResources;

    #region GUIHelperMethods
    public InputField consoleInputField;
    public Text inputText;
    public Text consoleText;
    public Canvas consoleCanvas;
    public GameObject consolePanel;

    private const string kUILayerName = "UI";

    private const string kStandardSpritePath = "UI/Skin/UISprite.psd";
    private const string kBackgroundSpritePath = "UI/Skin/Background.psd";
    private const string kInputFieldBackgroundPath = "UI/Skin/InputFieldBackground.psd";
    private const string kKnobPath = "UI/Skin/Knob.psd";
    private const string kCheckmarkPath = "UI/Skin/Checkmark.psd";
    private const string kDropdownArrowPath = "UI/Skin/DropdownArrow.psd";
    private const string kMaskPath = "UI/Skin/UIMask.psd";

    private const float kWidth = 160f;
    private const float kThickHeight = 30f;
    private const float kThinHeight = 20f;

    private static Vector2 s_ThickElementSize = new Vector2(kWidth, kThickHeight);
    private static Vector2 s_ThinElementSize = new Vector2(kWidth, kThinHeight);
    private static Vector2 s_ImageElementSize = new Vector2(100f, 100f);
    private static Color s_DefaultSelectableColor = new Color(1f, 1f, 1f, 1f);
    private static Color s_PanelColor = new Color(1f, 1f, 1f, 0.392f);
    private static Color s_TextColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);

    public struct Resources
    {
        /// The primary sprite to be used for graphical UI elements, used by the button, toggle, and dropdown controls, among others.
        public Sprite standard;

        /// Sprite used for background elements.
        public Sprite background;

        /// Sprite used as background for input fields.
        public Sprite inputField;

        /// Sprite used for knobs that can be dragged, such as on a slider.
        public Sprite knob;

        /// Sprite used for representation of an "on" state when present, such as a checkmark.
        public Sprite checkmark;

        /// Sprite used to indicate that a button will open a dropdown when clicked.
        public Sprite dropdown;

        /// Sprite used for masking purposes, for example to be used for the viewport of a scroll view.
        public Sprite mask;
    }

    private static GameObject CreateUIElementRoot(string name, Vector2 size)
    {
        GameObject child = new GameObject(name);
        RectTransform rectTransform = child.AddComponent<RectTransform>();
        rectTransform.sizeDelta = size;
        return child;
    }

    static GameObject CreateUIObject(string name, GameObject parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        SetParentAndAlign(go, parent);
        return go;
    }

    private static void SetDefaultTextValues(Text lbl)
    {
        lbl.color = s_TextColor;
        lbl.font = UnityEngine.Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void SetDefaultColorTransitionValues(Selectable slider)
    {
        ColorBlock colors = slider.colors;
        colors.highlightedColor = new Color(0.882f, 0.882f, 0.882f);
        colors.pressedColor = new Color(0.698f, 0.698f, 0.698f);
        colors.disabledColor = new Color(0.521f, 0.521f, 0.521f);
    }

    private static void SetParentAndAlign(GameObject child, GameObject parent)
    {
        if (parent == null)
            return;

        child.transform.SetParent(parent.transform, false);
        SetLayerRecursively(child, parent.layer);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }

    static private UnityCommandConsole.Resources GetStandardResources()
    {

        if (s_StandardResources.standard == null)
        {
            //s_StandardResources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>(kStandardSpritePath);
            //s_StandardResources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>(kBackgroundSpritePath);
            //s_StandardResources.inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>(kInputFieldBackgroundPath);
            //s_StandardResources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(kKnobPath);
            //s_StandardResources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>(kCheckmarkPath);
            //s_StandardResources.dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>(kDropdownArrowPath);
            //s_StandardResources.mask = AssetDatabase.GetBuiltinExtraResource<Sprite>(kMaskPath);

            s_StandardResources.standard = null;
            s_StandardResources.background = null;
            s_StandardResources.inputField = null;
            s_StandardResources.knob = null;
            s_StandardResources.checkmark = null;
            s_StandardResources.dropdown = null;
            s_StandardResources.mask = null;
        }
        
        return s_StandardResources;
    }

    /// <summary>
    /// Create the basic UI Panel.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Image
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreatePanel(Resources resources)
    {
        GameObject panelRoot = CreateUIElementRoot("UCC_Panel", s_ThickElementSize);

        // Set RectTransform to stretch
        RectTransform rectTransform = panelRoot.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = panelRoot.AddComponent<Image>();
        image.sprite = resources.background;
        image.type = Image.Type.Sliced;
        image.color = s_PanelColor;

        return panelRoot;
    }

    /// <summary>
    /// Create the basic UI button.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Button
    ///         -Text
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateButton(Resources resources)
    {
        GameObject buttonRoot = CreateUIElementRoot("UCC_Button", s_ThickElementSize);

        GameObject childText = new GameObject("UCC_Text");
        childText.AddComponent<RectTransform>();
        SetParentAndAlign(childText, buttonRoot);

        Image image = buttonRoot.AddComponent<Image>();
        image.sprite = resources.standard;
        image.type = Image.Type.Sliced;
        image.color = s_DefaultSelectableColor;

        Button bt = buttonRoot.AddComponent<Button>();
        SetDefaultColorTransitionValues(bt);

        Text text = childText.AddComponent<Text>();
        text.text = "Button";
        text.alignment = TextAnchor.MiddleCenter;
        SetDefaultTextValues(text);

        RectTransform textRectTransform = childText.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.sizeDelta = Vector2.zero;

        return buttonRoot;
    }

    /// <summary>
    /// Create the basic UI Text.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Text
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateText(Resources resources)
    {
        GameObject go = CreateUIElementRoot("UCC_Text", s_ThickElementSize);

        Text lbl = go.AddComponent<Text>();
        lbl.text = "New Text";
        SetDefaultTextValues(lbl);

        return go;
    }

    /// <summary>
    /// Create the basic UI Image.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Image
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateImage(Resources resources)
    {
        GameObject go = CreateUIElementRoot("UCC_Image", s_ImageElementSize);
        go.AddComponent<Image>();
        return go;
    }

    /// <summary>
    /// Create the basic UI RawImage.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     RawImage
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateRawImage(Resources resources)
    {
        GameObject go = CreateUIElementRoot("UCC_RawImage", s_ImageElementSize);
        go.AddComponent<RawImage>();
        return go;
    }

    /// <summary>
    /// Create the basic UI Slider.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Slider
    ///         - Background
    ///         - Fill Area
    ///             - Fill
    ///         - Handle Slide Area
    ///             - Handle
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateSlider(Resources resources)
    {
        // Create GOs Hierarchy
        GameObject root = CreateUIElementRoot("UCC_Slider", s_ThinElementSize);

        GameObject background = CreateUIObject("UCC_Background", root);
        GameObject fillArea = CreateUIObject("UCC_Fill_Area", root);
        GameObject fill = CreateUIObject("UCC_Fill", fillArea);
        GameObject handleArea = CreateUIObject("UCC_Handle_Slide_Area", root);
        GameObject handle = CreateUIObject("UCC_Handle", handleArea);

        // Background
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = resources.background;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = s_DefaultSelectableColor;
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0.25f);
        backgroundRect.anchorMax = new Vector2(1, 0.75f);
        backgroundRect.sizeDelta = new Vector2(0, 0);

        // Fill Area
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.anchoredPosition = new Vector2(-5, 0);
        fillAreaRect.sizeDelta = new Vector2(-20, 0);

        // Fill
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = resources.standard;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = s_DefaultSelectableColor;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(10, 0);

        // Handle Area
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.sizeDelta = new Vector2(-20, 0);
        handleAreaRect.anchorMin = new Vector2(0, 0);
        handleAreaRect.anchorMax = new Vector2(1, 1);

        // Handle
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = resources.knob;
        handleImage.color = s_DefaultSelectableColor;

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);

        // Setup slider component
        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        SetDefaultColorTransitionValues(slider);

        return root;
    }

    /// <summary>
    /// Create the basic UI Scrollbar.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Scrollbar
    ///         - Sliding Area
    ///             - Handle
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateScrollbar(Resources resources)
    {
        // Create GOs Hierarchy
        GameObject scrollbarRoot = CreateUIElementRoot("UCC_Scrollbar", s_ThinElementSize);

        GameObject sliderArea = CreateUIObject("UCC_Sliding_Area", scrollbarRoot);
        GameObject handle = CreateUIObject("UCC_Handle", sliderArea);

        Image bgImage = scrollbarRoot.AddComponent<Image>();
        bgImage.sprite = resources.background;
        bgImage.type = Image.Type.Sliced;
        bgImage.color = s_DefaultSelectableColor;

        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = resources.standard;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0f, 0f, 0f, 0.4f);

        RectTransform sliderAreaRect = sliderArea.GetComponent<RectTransform>();
        sliderAreaRect.sizeDelta = new Vector2(-20, -20);
        sliderAreaRect.anchorMin = Vector2.zero;
        sliderAreaRect.anchorMax = Vector2.one;

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);

        Scrollbar scrollbar = scrollbarRoot.AddComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;
        SetDefaultColorTransitionValues(scrollbar);

        return scrollbarRoot;
    }

    /// <summary>
    /// Create the basic UI Toggle.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Toggle
    ///         - Background
    ///             - Checkmark
    ///         - Label
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateToggle(Resources resources)
    {
        // Set up hierarchy
        GameObject toggleRoot = CreateUIElementRoot("UCC_Toggle", s_ThinElementSize);

        GameObject background = CreateUIObject("UCC_Background", toggleRoot);
        GameObject checkmark = CreateUIObject("UCC_Checkmark", background);
        GameObject childLabel = CreateUIObject("UCC_Label", toggleRoot);

        // Set up components
        Toggle toggle = toggleRoot.AddComponent<Toggle>();
        toggle.isOn = true;

        Image bgImage = background.AddComponent<Image>();
        bgImage.sprite = resources.standard;
        bgImage.type = Image.Type.Sliced;
        bgImage.color = s_DefaultSelectableColor;

        Image checkmarkImage = checkmark.AddComponent<Image>();
        checkmarkImage.sprite = resources.checkmark;

        Text label = childLabel.AddComponent<Text>();
        label.text = "Toggle";
        SetDefaultTextValues(label);

        toggle.graphic = checkmarkImage;
        toggle.targetGraphic = bgImage;
        SetDefaultColorTransitionValues(toggle);

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.anchoredPosition = new Vector2(10f, -10f);
        bgRect.sizeDelta = new Vector2(kThinHeight, kThinHeight);

        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchoredPosition = Vector2.zero;
        checkmarkRect.sizeDelta = new Vector2(20f, 20f);

        RectTransform labelRect = childLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(23f, 1f);
        labelRect.offsetMax = new Vector2(-5f, -2f);

        return toggleRoot;
    }

    /// <summary>
    /// Create the basic UI input field.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     InputField
    ///         - PlaceHolder
    ///         - Text
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateInputField(Resources resources)
    {
        GameObject root = CreateUIElementRoot("UCC_InputField", s_ThickElementSize);

        GameObject childPlaceholder = CreateUIObject("UCC_Placeholder", root);
        GameObject childText = CreateUIObject("UCC_Text", root);

        Image image = root.AddComponent<Image>();
        image.sprite = resources.inputField;
        image.type = Image.Type.Sliced;
        image.color = s_DefaultSelectableColor;

        InputField inputField = root.AddComponent<InputField>();
        SetDefaultColorTransitionValues(inputField);

        Text text = childText.AddComponent<Text>();
        text.text = "";
        text.supportRichText = false;
        SetDefaultTextValues(text);

        Text placeholder = childPlaceholder.AddComponent<Text>();
        placeholder.text = "Enter text...";
        placeholder.fontStyle = FontStyle.Italic;
        // Make placeholder color half as opaque as normal text color.
        Color placeholderColor = text.color;
        placeholderColor.a *= 0.5f;
        placeholder.color = placeholderColor;

        RectTransform textRectTransform = childText.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.sizeDelta = Vector2.zero;
        textRectTransform.offsetMin = new Vector2(10, 6);
        textRectTransform.offsetMax = new Vector2(-10, -7);

        RectTransform placeholderRectTransform = childPlaceholder.GetComponent<RectTransform>();
        placeholderRectTransform.anchorMin = Vector2.zero;
        placeholderRectTransform.anchorMax = Vector2.one;
        placeholderRectTransform.sizeDelta = Vector2.zero;
        placeholderRectTransform.offsetMin = new Vector2(10, 6);
        placeholderRectTransform.offsetMax = new Vector2(-10, -7);

        inputField.textComponent = text;
        inputField.placeholder = placeholder;

        return root;
    }

    /// <summary>
    /// Create the basic UI dropdown.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Dropdown
    ///         - Label
    ///         - Arrow
    ///         - Template
    ///             - Viewport
    ///                 - Content
    ///                     - Item
    ///                         - Item Background
    ///                         - Item Checkmark
    ///                         - Item Label
    ///             - Scrollbar
    ///                 - Sliding Area
    ///                     - Handle
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateDropdown(Resources resources)
    {
        GameObject root = CreateUIElementRoot("UCC_Dropdown", s_ThickElementSize);

        GameObject label = CreateUIObject("UCC_Label", root);
        GameObject arrow = CreateUIObject("UCC_Arrow", root);
        GameObject template = CreateUIObject("UCC_Template", root);
        GameObject viewport = CreateUIObject("UCC_Viewport", template);
        GameObject content = CreateUIObject("UCC_Content", viewport);
        GameObject item = CreateUIObject("UCC_Item", content);
        GameObject itemBackground = CreateUIObject("UCC_Item_Background", item);
        GameObject itemCheckmark = CreateUIObject("UCC_Item_Checkmark", item);
        GameObject itemLabel = CreateUIObject("UCC_Item_Label", item);

        // Sub controls.

        GameObject scrollbar = CreateScrollbar(resources);
        scrollbar.name = "UCC_Scrollbar";
        SetParentAndAlign(scrollbar, template);

        Scrollbar scrollbarScrollbar = scrollbar.GetComponent<Scrollbar>();
        scrollbarScrollbar.SetDirection(Scrollbar.Direction.BottomToTop, true);

        RectTransform vScrollbarRT = scrollbar.GetComponent<RectTransform>();
        vScrollbarRT.anchorMin = Vector2.right;
        vScrollbarRT.anchorMax = Vector2.one;
        vScrollbarRT.pivot = Vector2.one;
        vScrollbarRT.sizeDelta = new Vector2(vScrollbarRT.sizeDelta.x, 0);

        // Setup item UI components.

        Text itemLabelText = itemLabel.AddComponent<Text>();
        SetDefaultTextValues(itemLabelText);
        itemLabelText.alignment = TextAnchor.MiddleLeft;

        Image itemBackgroundImage = itemBackground.AddComponent<Image>();
        itemBackgroundImage.color = new Color32(245, 245, 245, 255);

        Image itemCheckmarkImage = itemCheckmark.AddComponent<Image>();
        itemCheckmarkImage.sprite = resources.checkmark;

        Toggle itemToggle = item.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBackgroundImage;
        itemToggle.graphic = itemCheckmarkImage;
        itemToggle.isOn = true;

        // Setup template UI components.

        Image templateImage = template.AddComponent<Image>();
        templateImage.sprite = resources.standard;
        templateImage.type = Image.Type.Sliced;

        ScrollRect templateScrollRect = template.AddComponent<ScrollRect>();
        templateScrollRect.content = (RectTransform)content.transform;
        templateScrollRect.viewport = (RectTransform)viewport.transform;
        templateScrollRect.horizontal = false;
        templateScrollRect.movementType = ScrollRect.MovementType.Clamped;
        templateScrollRect.verticalScrollbar = scrollbarScrollbar;
        templateScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        templateScrollRect.verticalScrollbarSpacing = -3;

        Mask scrollRectMask = viewport.AddComponent<Mask>();
        scrollRectMask.showMaskGraphic = false;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.sprite = resources.mask;
        viewportImage.type = Image.Type.Sliced;

        // Setup dropdown UI components.

        Text labelText = label.AddComponent<Text>();
        SetDefaultTextValues(labelText);
        labelText.alignment = TextAnchor.MiddleLeft;

        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.sprite = resources.dropdown;

        Image backgroundImage = root.AddComponent<Image>();
        backgroundImage.sprite = resources.standard;
        backgroundImage.color = s_DefaultSelectableColor;
        backgroundImage.type = Image.Type.Sliced;

        Dropdown dropdown = root.AddComponent<Dropdown>();
        dropdown.targetGraphic = backgroundImage;
        SetDefaultColorTransitionValues(dropdown);
        dropdown.template = template.GetComponent<RectTransform>();
        dropdown.captionText = labelText;
        dropdown.itemText = itemLabelText;

        // Setting default Item list.
        itemLabelText.text = "Option A";
        dropdown.options.Add(new Dropdown.OptionData { text = "Option A" });
        dropdown.options.Add(new Dropdown.OptionData { text = "Option B" });
        dropdown.options.Add(new Dropdown.OptionData { text = "Option C" });
        dropdown.RefreshShownValue();

        // Set up RectTransforms.

        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 6);
        labelRT.offsetMax = new Vector2(-25, -7);

        RectTransform arrowRT = arrow.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.sizeDelta = new Vector2(20, 20);
        arrowRT.anchoredPosition = new Vector2(-15, 0);

        RectTransform templateRT = template.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, 150);

        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = new Vector2(0, 0);
        viewportRT.anchorMax = new Vector2(1, 1);
        viewportRT.sizeDelta = new Vector2(-18, 0);
        viewportRT.pivot = new Vector2(0, 1);

        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1);
        contentRT.anchorMax = new Vector2(1f, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.anchoredPosition = new Vector2(0, 0);
        contentRT.sizeDelta = new Vector2(0, 28);

        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 20);

        RectTransform itemBackgroundRT = itemBackground.GetComponent<RectTransform>();
        itemBackgroundRT.anchorMin = Vector2.zero;
        itemBackgroundRT.anchorMax = Vector2.one;
        itemBackgroundRT.sizeDelta = Vector2.zero;

        RectTransform itemCheckmarkRT = itemCheckmark.GetComponent<RectTransform>();
        itemCheckmarkRT.anchorMin = new Vector2(0, 0.5f);
        itemCheckmarkRT.anchorMax = new Vector2(0, 0.5f);
        itemCheckmarkRT.sizeDelta = new Vector2(20, 20);
        itemCheckmarkRT.anchoredPosition = new Vector2(10, 0);

        RectTransform itemLabelRT = itemLabel.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(20, 1);
        itemLabelRT.offsetMax = new Vector2(-10, -2);

        template.SetActive(false);

        return root;
    }

    /// <summary>
    /// Create the basic UI Scrollview.
    /// </summary>
    /// <remarks>
    /// Hierarchy:
    /// (root)
    ///     Scrollview
    ///         - Viewport
    ///             - Content
    ///         - Scrollbar Horizontal
    ///             - Sliding Area
    ///                 - Handle
    ///         - Scrollbar Vertical
    ///             - Sliding Area
    ///                 - Handle
    /// </remarks>
    /// <param name="resources">The resources to use for creation.</param>
    /// <returns>The root GameObject of the created element.</returns>
    public static GameObject CreateScrollView(Resources resources)
    {
        GameObject root = CreateUIElementRoot("UCC_Scroll_View", new Vector2(200, 200));

        GameObject viewport = CreateUIObject("UCC_Viewport", root);
        GameObject content = CreateUIObject("UCC_Content", viewport);

        // Sub controls.

        GameObject hScrollbar = CreateScrollbar(resources);
        hScrollbar.name = "UCC_Scrollbar_Horizontal";
        SetParentAndAlign(hScrollbar, root);
        RectTransform hScrollbarRT = hScrollbar.GetComponent<RectTransform>();
        hScrollbarRT.anchorMin = Vector2.zero;
        hScrollbarRT.anchorMax = Vector2.right;
        hScrollbarRT.pivot = Vector2.zero;
        hScrollbarRT.sizeDelta = new Vector2(0, hScrollbarRT.sizeDelta.y);

        GameObject vScrollbar = CreateScrollbar(resources);
        vScrollbar.name = "UCC_Scrollbar_Vertical";
        SetParentAndAlign(vScrollbar, root);
        vScrollbar.GetComponent<Scrollbar>().SetDirection(Scrollbar.Direction.BottomToTop, true);
        RectTransform vScrollbarRT = vScrollbar.GetComponent<RectTransform>();
        vScrollbarRT.anchorMin = Vector2.right;
        vScrollbarRT.anchorMax = Vector2.one;
        vScrollbarRT.pivot = Vector2.one;
        vScrollbarRT.sizeDelta = new Vector2(vScrollbarRT.sizeDelta.x, 0);

        // Setup RectTransforms.

        // Make viewport fill entire scroll view.
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.pivot = Vector2.up;

        // Make context match viewpoprt width and be somewhat taller.
        // This will show the vertical scrollbar and not the horizontal one.
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.up;
        contentRT.anchorMax = Vector2.one;
        contentRT.sizeDelta = new Vector2(0, 300);
        contentRT.pivot = Vector2.up;

        // Setup UI components.

        ScrollRect scrollRect = root.AddComponent<ScrollRect>();
        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontalScrollbar = hScrollbar.GetComponent<Scrollbar>();
        scrollRect.verticalScrollbar = vScrollbar.GetComponent<Scrollbar>();
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.horizontalScrollbarSpacing = -3;
        scrollRect.verticalScrollbarSpacing = -3;

        Image rootImage = root.AddComponent<Image>();
        rootImage.sprite = resources.background;
        rootImage.type = Image.Type.Sliced;
        rootImage.color = s_PanelColor;

        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.sprite = resources.mask;
        viewportImage.type = Image.Type.Sliced;

        return root;
    }

    private static GameObject CreateConsoleGui()
    {

        //  Canvas
        GameObject canvas = new GameObject("UCC_Canvas");

        UnityCommandConsole.Instance.consoleCanvas = canvas.AddComponent<Canvas>();
        UnityCommandConsole.Instance.consoleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityCommandConsole.Instance.consoleCanvas.sortingOrder = 32767;

        UnityCommandConsole.Instance.consoleCanvas.gameObject.AddComponent<GraphicRaycaster>();

        CanvasScaler canvasScaler = canvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.referenceResolution = new Vector2(800f, 600f);

        UnityCommandConsole.Instance.consoleCanvas.gameObject.AddComponent<GraphicRaycaster>();

        //  Panel
        Instance.consolePanel = UnityCommandConsole.CreatePanel(GetStandardResources());

        RectTransform panelRT = Instance.consolePanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0.3f);
        panelRT.anchoredPosition = new Vector2(0f, 0f);
        panelRT.sizeDelta = new Vector2(0f, 0f);

        Image panelImage = Instance.consolePanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.4f);

        UnityCommandConsole.SetParentAndAlign(Instance.consolePanel, canvas);

        //  ScrollView
        GameObject scrollview = UnityCommandConsole.CreateScrollView(GetStandardResources());

        ScrollRect scrollviewSR = scrollview.GetComponent<ScrollRect>();
        scrollviewSR.movementType = ScrollRect.MovementType.Clamped;

        RectTransform scrollviewRT = scrollview.GetComponent<RectTransform>();
        scrollviewRT.anchorMin = new Vector2(0f, 0f);
        scrollviewRT.anchorMax = new Vector2(1f, 1f);
        scrollviewRT.sizeDelta = new Vector2(-15f, -41f);
        scrollviewRT.localPosition = new Vector3(0f, 11.5f, 0f);
        scrollviewRT.anchoredPosition = new Vector2(0f, 11.5f);

        Image scrollviewImage = scrollview.GetComponent<Image>();
        scrollviewImage.color = new Color(0f, 0f, 0f, 0.43f);

        // remove horizontal scrollbar
        GameObject.Destroy(GameObject.Find("UCC_Scrollbar_Horizontal"));

        // add Text to scrollview content
        GameObject scrollViewContent = GameObject.Find("UCC_Content");

        RectTransform scrollViewContentRT = scrollViewContent.GetComponent<RectTransform>();
        scrollViewContentRT.pivot = new Vector2(0f, 0f);

        GameObject scrollViewContentText = UnityCommandConsole.CreateText(GetStandardResources());
        UnityCommandConsole.Instance.consoleText = scrollViewContentText.GetComponent<Text>();
        UnityCommandConsole.Instance.consoleText.text = "DEBUG CONSOLE\n------------------------\ntype 'help' to view available commands";
        UnityCommandConsole.Instance.consoleText.color = new Color(1f, 1f, 1f, 0.90f);
        UnityCommandConsole.Instance.consoleText.fontSize = 11;
        UnityCommandConsole.Instance.consoleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        UnityCommandConsole.Instance.consoleText.verticalOverflow = VerticalWrapMode.Overflow;
        UnityCommandConsole.Instance.consoleText.alignment = TextAnchor.LowerLeft;

        UnityCommandConsole.SetParentAndAlign(scrollViewContentText, scrollViewContent);

        VerticalLayoutGroup verticalLayoutGroup = scrollViewContent.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childAlignment = TextAnchor.LowerLeft;
        verticalLayoutGroup.padding.left = 5;
        verticalLayoutGroup.padding.right = 5;
        verticalLayoutGroup.padding.top = 0;
        verticalLayoutGroup.padding.bottom = 8;

        ContentSizeFitter contentSizeFitter = scrollViewContent.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UnityCommandConsole.SetParentAndAlign(scrollview, Instance.consolePanel);


        //  InputField
        GameObject inputField = UnityCommandConsole.CreateInputField(GetStandardResources());

        UnityCommandConsole.Instance.consoleInputField = inputField.GetComponent<InputField>();

        RectTransform inputFieldRT = inputField.GetComponent<RectTransform>();
        inputFieldRT.anchorMin = new Vector2(0.275f, 0f);
        inputFieldRT.anchorMax = new Vector2(0.725f, 0f);
        inputFieldRT.sizeDelta = new Vector2(425f, 23f);
        inputFieldRT.localPosition = new Vector3(0f, -90f, 0f);
        inputFieldRT.anchoredPosition = new Vector2(0f, 18f);

        Image inputFieldImage = inputField.GetComponent<Image>();
        inputFieldImage.color = new Color(0f, 0f, 0f, 0.43f);

        UnityCommandConsole.Instance.inputText = UnityCommandConsole.Instance.consoleInputField.gameObject.transform.Find("UCC_Text").gameObject.GetComponent<Text>();
        UnityCommandConsole.Instance.inputText.fontSize = 10;
        UnityCommandConsole.Instance.inputText.alignment = TextAnchor.MiddleLeft;
        UnityCommandConsole.Instance.inputText.color = new Color(1f, 1f, 1f);

        RectTransform inputTextRT = UnityCommandConsole.Instance.inputText.gameObject.GetComponent<RectTransform>();
        inputTextRT.localPosition = new Vector3(0f, 1.1f);
        inputTextRT.anchorMin = new Vector2(0f, 0f);
        inputTextRT.anchorMax = new Vector2(1f, 1f);
        inputTextRT.anchoredPosition = new Vector2(0f, 1.1f);
        inputTextRT.sizeDelta = new Vector2(-11.42f, -9.42f);

        Text inputFieldPlaceholder = UnityCommandConsole.Instance.consoleInputField.gameObject.transform.Find("UCC_Placeholder").gameObject.GetComponent<Text>();
        inputFieldPlaceholder.text = "ENTER A COMMAND";
        inputFieldPlaceholder.color = new Color(1f, 1f, 1f, 0.90f);
        inputFieldPlaceholder.fontSize = 10;

        UnityCommandConsole.SetParentAndAlign(inputField, Instance.consolePanel);

        return canvas;
    }
    #endregion

    #region ConsoleHelperMethods
    public void FocusAndClearInput()
    {
        ClearInput();
        FocusInput();
    }

    public void FocusInput()
    {
        Instance.consoleInputField.ActivateInputField();
        Instance.consoleInputField.MoveTextEnd(true);
        EventSystem.current.SetSelectedGameObject(Instance.consoleInputField.gameObject, null);
    }

    public void ClearInput()
    {
        Instance.consoleInputField.text = "";
    }

    public void Clear(params object[] args)
    {
        Instance.consoleText.text = string.Empty;
    }

    public void Print(string msg)
    {
        Instance.consoleText.text += "\n" + msg;
    }

    public void Help(params object[] args)
    {
        Instance.Print("---- Available Commands ----");
        foreach (Command cmd in Instance.CommandsList)
            Instance.Print("\t " + cmd.command + (String.IsNullOrEmpty(cmd.description) ? " - No Description" : " - " + cmd.description));
    }
    #endregion

    public static void Init()
    {
        if (UnityCommandConsole.Instance != null)
        {
            return;
        }

        //  GameObject
        GameObject go = new GameObject("UnityCommandConsoleObject", typeof(UnityCommandConsole));
        GameObject canvas = CreateConsoleGui();
        UnityCommandConsole.SetParentAndAlign(canvas, go);

        UnityCommandConsole.Instance = go.GetComponent<UnityCommandConsole>();

        Instance.CommandsList = new List<Command>();
        Instance.Commands = new Dictionary<string, CommandDelegate>();
        Instance.Hotkeys = new Dictionary<KeyCode, CommandDelegate>();

        AddCommands();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Instance.Active = false;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Instance.consoleCanvas.gameObject.SetActive(Instance.Active);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Instance.Active = !Instance.Active;
            Instance.consoleCanvas.gameObject.SetActive(Instance.Active);

            if (Instance.Active)
                FocusAndClearInput();
        }

        if(Instance.Active)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (System.String.IsNullOrEmpty(Instance.inputText.text))
                    return;

                string[] splitCommand = Instance.inputText.text.ToLower().Split(new char[] {' '}, 2);

                string cmd = splitCommand[0];

                string[] args = new string[] { };

                if (splitCommand.Length > 1)
                    args = splitCommand[1].Split();

                try
                {
                    Commands[cmd](args);
                }
                catch (KeyNotFoundException ex)
                {
                    Instance.Print("'" + cmd + "' command not found");
                }
                catch (Exception ex)
                {
                    Instance.Print("ERROR: " + ex.Message);
                }

                FocusAndClearInput();
            }
            else
            {
                foreach( KeyCode key in Instance.Hotkeys.Keys)
                {
                    if (key != KeyCode.None && Input.GetKeyDown(key))
                        Instance.Hotkeys[key]();
                }
            }
        }
    }

    public static void AddCommands()
    {
        Instance.CommandsList.AddRange(new Command[] {
            new Command("help", Instance.Help, KeyCode.None, "prints this help menu listing the available commands"),
            new Command("clear", Instance.Clear, KeyCode.F1, "clears the debug console text"),

            // Add mod methods here:
        });

        foreach (Command cmd in Instance.CommandsList)
        {
            Instance.Commands.Add(cmd.command, cmd.function);

            if (cmd.hotkey != KeyCode.None)
                Instance.Hotkeys.Add(cmd.hotkey, cmd.function);
        }
    }

    #region ModMethods
    #endregion
}
