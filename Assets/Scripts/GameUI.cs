using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Tự tạo Canvas + màn hình Start + màn hình Win bằng code.
// Không cần gắn thủ công: GameController sẽ tự AddComponent script này.
public class GameUI : MonoBehaviour
{
    GameObject startPanel;
    GameObject winPanel;
    Font font;

    GameObject hud;

    public void Build(Action onPlay, Action onReplay, Action onReset)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasGO = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // ---- HUD trong lúc chơi: nút Reset ở góc trên bên phải
        hud = new GameObject("HUD", typeof(RectTransform));
        hud.transform.SetParent(canvasGO.transform, false);
        RectTransform hudRT = hud.GetComponent<RectTransform>();
        hudRT.anchorMin = Vector2.zero;
        hudRT.anchorMax = Vector2.one;
        hudRT.offsetMin = Vector2.zero;
        hudRT.offsetMax = Vector2.zero;

        MakeButton(hud.transform, "RESET", new Vector2(-170, -110), new Color(0.85f, 0.3f, 0.25f), onReset);
        RectTransform resetRT = hud.transform.GetChild(0).GetComponent<RectTransform>();
        resetRT.anchorMin = resetRT.anchorMax = new Vector2(1f, 1f);   // neo góc trên phải
        resetRT.sizeDelta = new Vector2(260, 100);
        resetRT.anchoredPosition = new Vector2(-160, -100);
        resetRT.GetComponentInChildren<Text>().rectTransform.sizeDelta = resetRT.sizeDelta;
        resetRT.GetComponentInChildren<Text>().fontSize = 44;
        hud.SetActive(false);

        // ---- Màn hình bắt đầu
        startPanel = MakePanel(canvasGO.transform, new Color(0.05f, 0.08f, 0.15f, 0.95f));
        MakeText(startPanel.transform, "WATER SORT", 110, new Vector2(0, 250), Color.white);
        MakeButton(startPanel.transform, "PLAY", new Vector2(0, -100), new Color(0.15f, 0.65f, 0.3f), onPlay);

        // ---- Màn hình thắng
        winPanel = MakePanel(canvasGO.transform, new Color(0.05f, 0.08f, 0.15f, 0.88f));
        MakeText(winPanel.transform, "YOU WIN!", 130, new Vector2(0, 250), new Color(1f, 0.9f, 0.2f));
        MakeButton(winPanel.transform, "REPLAY", new Vector2(0, -100), new Color(0.2f, 0.5f, 0.9f), onReplay);

        startPanel.SetActive(false);
        winPanel.SetActive(false);
    }

    public void ShowHud(bool show) { hud.SetActive(show); }
    public void ShowStart(bool show) { startPanel.SetActive(show); }
    public void ShowWin(bool show) { winPanel.SetActive(show); }

    // ------------------------------------------------------------ helpers
    GameObject MakePanel(Transform parent, Color color)
    {
        GameObject go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = color;
        return go;
    }

    Text MakeText(Transform parent, string content, int size, Vector2 pos, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text t = go.GetComponent<Text>();
        t.text = content;
        t.font = font;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;

        RectTransform rt = t.rectTransform;
        rt.sizeDelta = new Vector2(900, 200);
        rt.anchoredPosition = pos;
        return t;
    }

    void MakeButton(Transform parent, string label, Vector2 pos, Color color, Action onClick)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 130);
        rt.anchoredPosition = pos;

        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        Text txt = MakeText(go.transform, label, 56, Vector2.zero, Color.white);
        txt.rectTransform.sizeDelta = rt.sizeDelta;
    }
}