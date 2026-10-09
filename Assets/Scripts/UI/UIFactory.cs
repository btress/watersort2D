using UnityEngine;
using UnityEngine.UI;

public static class UIFactory
{
    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Vector2 TextBoxSize = new Vector2(900f, 200f);

    private static Font _font;

    private static Font DefaultFont
    {
        get
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform CreateContainer(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        return rect;
    }

    public static RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        RectTransform rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        return rect;
    }

    public static Text CreateText(Transform parent, string content, int fontSize, Vector2 anchoredPosition, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = DefaultFont;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;

        text.rectTransform.sizeDelta = TextBoxSize;
        text.rectTransform.anchoredPosition = anchoredPosition;
        return text;
    }

    public static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchor,
        Vector2 anchoredPosition, Color color, int fontSize = 56)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        go.GetComponent<Image>().color = color;

        Text text = CreateText(go.transform, label, fontSize, Vector2.zero, Color.white);
        text.rectTransform.sizeDelta = size;

        return go.GetComponent<Button>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
