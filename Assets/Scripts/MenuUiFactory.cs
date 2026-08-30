using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class MenuUiFactory
{
    private static readonly string[] JapaneseFontFiles =
    {
        "YuGothM.ttc",
        "meiryo.ttc",
        "BIZ-UDGothicR.ttc",
        "msgothic.ttc"
    };

    public static readonly Color BackgroundColor = new Color(0.015f, 0.035f, 0.05f, 1f);
    public static readonly Color PanelColor = new Color(0.035f, 0.07f, 0.09f, 0.96f);
    public static readonly Color CyanColor = new Color(0.05f, 0.82f, 1f, 1f);
    public static readonly Color WhiteColor = new Color(0.94f, 0.98f, 1f, 1f);
    public static readonly Color MutedColor = new Color(0.5f, 0.65f, 0.7f, 1f);

    private static TMP_FontAsset menuFontAsset;

    public static Canvas CreateCanvas(string objectName, int sortingOrder = 50)
    {
        EnsureMenuCamera();

        GameObject canvasObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform CreateRect(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    public static Image CreateImage(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            anchorMin,
            anchorMax,
            pivot,
            anchoredPosition,
            size
        );
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            anchorMin,
            anchorMax,
            pivot,
            anchoredPosition,
            size
        );
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetMenuFontAsset();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static TMP_FontAsset GetMenuFontAsset()
    {
        if (menuFontAsset != null)
        {
            return menuFontAsset;
        }

        string fontDirectory = System.Environment.GetFolderPath(
            System.Environment.SpecialFolder.Fonts
        );

        foreach (string fileName in JapaneseFontFiles)
        {
            string fontPath = Path.Combine(fontDirectory, fileName);

            if (!File.Exists(fontPath))
            {
                continue;
            }

            menuFontAsset = TMP_FontAsset.CreateFontAsset(
                fontPath,
                0,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024
            );

            if (menuFontAsset != null)
            {
                menuFontAsset.name = "MenuJapaneseFontAsset";
                return menuFontAsset;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private static void EnsureMenuCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        GameObject cameraObject = new GameObject("MenuCamera", typeof(Camera));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = BackgroundColor;
        camera.cullingMask = 0;
    }

    public static Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform rect = CreateRect(
            objectName,
            parent,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            anchoredPosition,
            size
        );
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = PanelColor;
        colors.highlightedColor = new Color(0.06f, 0.28f, 0.36f, 1f);
        colors.selectedColor = new Color(0.04f, 0.38f, 0.5f, 1f);
        colors.pressedColor = CyanColor;
        colors.disabledColor = new Color(0.08f, 0.1f, 0.11f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreateImage(
            "Accent",
            rect,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            Vector2.zero,
            new Vector2(6f, 0f),
            CyanColor
        );
        CreateText(
            "Label",
            rect,
            label,
            27f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            WhiteColor,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(24f, 0f),
            new Vector2(-48f, 0f)
        );
        return button;
    }

    public static EventSystem EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            );
            eventSystem = eventObject.GetComponent<EventSystem>();
        }

        InputSystemUIInputModule inputModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputModule == null)
        {
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputModule.AssignDefaultActions();

        StandaloneInputModule legacyModule =
            eventSystem.GetComponent<StandaloneInputModule>();

        if (legacyModule != null)
        {
            legacyModule.enabled = false;
        }

        return eventSystem;
    }
}
