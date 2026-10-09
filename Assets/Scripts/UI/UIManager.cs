using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 TopRightAnchor = new Vector2(1f, 1f);

    private GameObject _startPanel;
    private GameObject _hud;
    private Text _levelLabel;

    public event Action PlayClicked;
    public event Action ResetClicked;

    public WinPopupUI WinPopup { get; private set; }

    private void Awake()
    {
        EnsureEventSystem();

        Canvas canvas = UIFactory.CreateCanvas("GameCanvas", 100);
        BuildHud(canvas.transform);
        BuildStartPanel(canvas.transform);
        BuildWinPopup(canvas.transform);
    }

    public void ShowStart(bool show)
    {
        _startPanel.SetActive(show);
    }

    public void ShowHud(bool show)
    {
        _hud.SetActive(show);
    }

    public void SetLevelNumber(int number)
    {
        _levelLabel.text = $"LEVEL {number}";
    }

    private void BuildHud(Transform parent)
    {
        _hud = UIFactory.CreateContainer(parent, "HUD").gameObject;

        _levelLabel = UIFactory.CreateText(_hud.transform, "LEVEL 1", 60, new Vector2(-200f, 860f), Color.white);

        Button reset = UIFactory.CreateButton(_hud.transform, "RESET", new Vector2(260f, 100f),
            TopRightAnchor, new Vector2(-40f, -40f), new Color(0.85f, 0.30f, 0.25f), 44);
        reset.onClick.AddListener(() => ResetClicked?.Invoke());

        _hud.SetActive(false);
    }

    private void BuildStartPanel(Transform parent)
    {
        _startPanel = UIFactory.CreatePanel(parent, "StartPanel", new Color(0.05f, 0.08f, 0.15f, 0.95f)).gameObject;

        UIFactory.CreateText(_startPanel.transform, "WATER SORT", 110, new Vector2(0f, 250f), Color.white);

        Button play = UIFactory.CreateButton(_startPanel.transform, "PLAY", new Vector2(420f, 130f),
            CenterAnchor, new Vector2(0f, -100f), new Color(0.15f, 0.65f, 0.30f));
        play.onClick.AddListener(() => PlayClicked?.Invoke());
    }

    private void BuildWinPopup(Transform parent)
    {
        RectTransform root = UIFactory.CreateContainer(parent, "WinPopupUI");
        WinPopup = root.gameObject.AddComponent<WinPopupUI>();
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
