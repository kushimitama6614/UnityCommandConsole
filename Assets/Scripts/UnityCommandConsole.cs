using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnityCommandConsole : MonoBehaviour
{
    public InputField consoleInputField;
    public Text inputText;
    public Text consoleText;
    public Canvas consoleCanvas;

    public static UnityCommandConsole Instance { get; private set; }

    public bool Active { get; private set; }

    public static void Init()
    {
        if (UnityCommandConsole.Instance != null)
        {
            return;
        }

        //  GameObject
        GameObject go = new GameObject("UnityCommandConsoleObject", typeof(UnityCommandConsole));

        //  Canvas
        GameObject canvas = new GameObject("Canvas");

        UnityCommandConsole.Instance.consoleCanvas = canvas.AddComponent<Canvas>();
        UnityCommandConsole.Instance.consoleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityCommandConsole.Instance.consoleCanvas.sortingOrder = 32767;

        UnityCommandConsole.Instance.consoleCanvas.gameObject.AddComponent<GraphicRaycaster>();

        CanvasScaler canvasScaler = canvas.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.referenceResolution = new Vector2(800f, 600f);

        UnityCommandConsole.Instance.consoleCanvas.transform.parent = go.transform;

        //  Panel
        GameObject panel = new GameObject("Panel");

        panel.AddComponent<CanvasRenderer>();

        RectTransform panelRectTransform = panel.AddComponent<RectTransform>();
        panelRectTransform.sizeDelta = new Vector2(550f, 350f);
        panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelRectTransform.transform.position = canvas.transform.position;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.2f);
        panelImage.raycastTarget = true;
        panelImage.type = Image.Type.Sliced;
        panelImage.fillCenter = true;

        panel.transform.parent = UnityCommandConsole.Instance.consoleCanvas.transform;

        // Text
        GameObject consoleDisplayText = new GameObject("Text");
        consoleDisplayText.AddComponent<CanvasRenderer>();

        RectTransform consoleTextRectTransform = consoleDisplayText.AddComponent<RectTransform>();

        LayoutElement consoleDisplayTextLayoutElement = consoleDisplayText.AddComponent<LayoutElement>();
        consoleDisplayTextLayoutElement.minHeight = 35f;
        consoleDisplayTextLayoutElement.layoutPriority = 1;

        UnityCommandConsole.Instance.consoleText = consoleDisplayText.AddComponent<Text>();
        UnityCommandConsole.Instance.consoleText.text = "DEBUG CONSOLE\n------------------------\ntype 'help' to view available commands";
        UnityCommandConsole.Instance.consoleText.fontSize = 13;
        UnityCommandConsole.Instance.consoleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        UnityCommandConsole.Instance.consoleText.lineSpacing = 1f;
        UnityCommandConsole.Instance.consoleText.supportRichText = true;
        UnityCommandConsole.Instance.consoleText.verticalOverflow = VerticalWrapMode.Overflow;
        UnityCommandConsole.Instance.consoleText.color = new Color(1f, 1f, 1f);
        UnityCommandConsole.Instance.consoleText.raycastTarget = true;

        // Content
        GameObject content = new GameObject("Content");
        UnityCommandConsole.Instance.consoleText.transform.parent = content.transform;

        RectTransform contentRectTransform = content.AddComponent<RectTransform>();
        contentRectTransform.anchorMin = new Vector2(0.01f, 1f);
        contentRectTransform.anchorMax = new Vector2(1f, 0f);
        contentRectTransform.sizeDelta = new Vector2(0f, 300f);
        contentRectTransform.pivot = new Vector2(0f, 0f);

        VerticalLayoutGroup verticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childForceExpandHeight = false;
        verticalLayoutGroup.childControlHeight = true;
        verticalLayoutGroup.childAlignment = TextAnchor.LowerLeft;
        verticalLayoutGroup.padding.left = 5;
        verticalLayoutGroup.padding.right = 5;
        verticalLayoutGroup.padding.top = 0;
        verticalLayoutGroup.padding.bottom = 8;

        ContentSizeFitter contentSizeFitter = content.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Viewport
        GameObject viewport = new GameObject("Viewport");

        viewport.AddComponent<Mask>();
        viewport.AddComponent<CanvasRenderer>();

        content.transform.parent = viewport.transform;
        content.transform.localPosition = new Vector3(-50f, 0f, 0f);

        RectTransform viewportRectTransform = viewport.GetComponent<RectTransform>();
        viewportRectTransform.sizeDelta = new Vector2(-20f, -20f);
        viewportRectTransform.anchorMin = new Vector2(0f, 0.06f);
        viewportRectTransform.anchorMax = new Vector2(1f, 1f);

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.raycastTarget = true;
        viewportImage.type = Image.Type.Sliced;
        viewportImage.fillCenter = true;
        viewportImage.color = new Color(0f, 0f, 0f, 0.4f);

        ////  Handle
        //GameObject handle = new GameObject("Handle");

        //handle.AddComponent<CanvasRenderer>();

        //RectTransform handleRectTransform = handle.AddComponent<RectTransform>();
        //handleRectTransform.anchorMin = new Vector2(0f, 0f);
        //handleRectTransform.anchorMax = new Vector2(1f, 1f);

        //Image handleImage = handle.AddComponent<Image>();
        //handleImage.raycastTarget = true;
        //handleImage.type = Image.Type.Sliced;
        //handleImage.fillCenter = true;
        //handleImage.color = new Color(0f, 0f, 0f, 0.4f);

        ////  Sliding Area
        //GameObject slidingArea = new GameObject("SlidingArea");

        //RectTransform slidingAreaRectTransform = slidingArea.AddComponent<RectTransform>();
        //slidingAreaRectTransform.offsetMin = new Vector2(0f, 0f);
        //slidingAreaRectTransform.offsetMax = new Vector2(20f, 20f);

        //handle.transform.parent = slidingArea.transform;

        ////  Scrollbar Vertical
        //GameObject scrollbarVertical = new GameObject("ScrollbarVertical");

        //slidingAreaRectTransform.transform.parent = handle.transform;
        //slidingAreaRectTransform.localPosition = new Vector3(0, 0, 0);
        //slidingAreaRectTransform.anchorMin = new Vector2(0.2f, 1f);
        //slidingAreaRectTransform.anchorMax = new Vector2(1f, 1f);

        //scrollbarVertical.AddComponent<CanvasRenderer>();

        //RectTransform scrollbarVerticalRectTransform = scrollbarVertical.AddComponent<RectTransform>();
        //scrollbarVerticalRectTransform.anchorMin = new Vector2(1f, 0f);
        //scrollbarVerticalRectTransform.anchorMax = new Vector2(1f, 1f);

        //Image scrollbarVerticalImage = scrollbarVertical.AddComponent<Image>();
        //scrollbarVerticalImage.raycastTarget = true;
        //scrollbarVerticalImage.type = Image.Type.Sliced;
        //scrollbarVerticalImage.fillCenter = true;

        //Scrollbar scrollbar = scrollbarVertical.AddComponent<Scrollbar>();
        //scrollbar.interactable = true;
        //scrollbar.transition = Selectable.Transition.ColorTint;
        //scrollbar.size = 1f;
        //scrollbar.direction = 0;

        //slidingArea.transform.parent = scrollbar.transform;

        //////  Text
        //////  Placeholder
        //////  InputField

        //  Scroll View
        GameObject scrollView = new GameObject("ScrollView");
        viewport.transform.parent = scrollView.transform;
        //scrollbarVertical.transform.parent = scrollView.transform; 

        scrollView.AddComponent<CanvasRenderer>();

        scrollView.transform.parent = panel.transform;
        scrollView.transform.localPosition = new Vector3(0, 0, 0);

        RectTransform scrollViewRectTransform = scrollView.AddComponent<RectTransform>();
        scrollViewRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollViewRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollViewRectTransform.sizeDelta = new Vector2(0f, 0f);

        Image scrollViewImage = scrollView.AddComponent<Image>();
        scrollViewImage.color = new Color(0f, 0f, 0f, 0.4f);
        scrollViewImage.raycastTarget = true;
        scrollViewImage.type = Image.Type.Sliced;
        scrollViewImage.fillCenter = true;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.content = contentRectTransform;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 1f;
        scrollRect.viewport = viewportRectTransform;

        ////  Text
        ////  Placeholder
        ////  InputField

        UnityCommandConsole.Instance = go.GetComponent<UnityCommandConsole>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Instance.Active = true;
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
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Instance.consoleText.text += "hello world\n";
        }
    }
}
