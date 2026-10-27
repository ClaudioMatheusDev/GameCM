using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Cria elementos de uGUI em codigo para que as telas nao dependam de edicao na cena.</summary>
public static class UiFactory
{
    public static readonly Color OverlayColor = new Color(0.04f, 0.05f, 0.08f, 0.93f);
    public static readonly Color TitleColor = new Color(0.95f, 0.65f, 0.15f, 1f);
    public static readonly Color MutedColor = new Color(0.75f, 0.78f, 0.85f, 1f);

    private static Font font;

    public static Font DefaultFont
    {
        get
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return font;
        }
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    /// <summary>Painel de tela cheia com os filhos empilhados e centralizados.</summary>
    public static RectTransform CreatePanel(Transform parent, string name, Color background, float spacing)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = background.a > 0f;

        VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(40, 40, 40, 40);
        return rect;
    }

    public static Text CreateLabel(
        Transform parent,
        string text,
        int fontSize,
        float height,
        Color color,
        FontStyle style = FontStyle.Normal)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.GetComponent<Text>();
        label.font = DefaultFont;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 900f;
        layout.preferredHeight = height;
        return label;
    }

    public static Button CreateButton(Transform parent, string label, UnityAction onClick, float width = 360f)
    {
        GameObject buttonObject = new GameObject(
            label + " Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.2f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.55f, 0.1f, 1f);
        colors.selectedColor = new Color(0.9f, 0.55f, 0.1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.4f, 0.05f, 1f);
        colors.disabledColor = new Color(0.12f, 0.12f, 0.15f, 0.8f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 50f;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = DefaultFont;
        text.text = label;
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return button;
    }

    /// <summary>Linha horizontal para agrupar botoes e rotulos lado a lado.</summary>
    public static RectTransform CreateRow(Transform parent, float height)
    {
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = height;
        element.preferredWidth = 520f;
        return row.GetComponent<RectTransform>();
    }

    public static void SetButtonText(Button button, string text)
    {
        button.GetComponentInChildren<Text>().text = text;
    }
}
