using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WinPopupUI : MonoBehaviour
{
    private static readonly Vector2 CenterAnchor = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 CardSize = new Vector2(800f, 720f);
    private static readonly Vector2 ButtonSize = new Vector2(520f, 130f);

    [SerializeField] private float _showDuration = 0.35f;

    private GameObject _dim;
    private RectTransform _card;
    private Tween _showTween;

    public event Action NextClicked;
    public event Action ReplayClicked;

    private void Awake()
    {
        _dim = UIFactory.CreatePanel(transform, "Dim", new Color(0f, 0f, 0f, 0.7f)).gameObject;

        _card = UIFactory.CreatePanel(_dim.transform, "Card", new Color(0.12f, 0.17f, 0.28f, 1f));
        _card.anchorMin = CenterAnchor;
        _card.anchorMax = CenterAnchor;
        _card.sizeDelta = CardSize;
        _card.anchoredPosition = Vector2.zero;

        UIFactory.CreateText(_card, "LEVEL COMPLETE!", 72, new Vector2(0f, 220f), new Color(1f, 0.9f, 0.2f));

        Button next = UIFactory.CreateButton(_card, "NEXT LEVEL", ButtonSize, CenterAnchor,
            new Vector2(0f, -10f), new Color(0.15f, 0.65f, 0.30f));
        next.onClick.AddListener(() => NextClicked?.Invoke());

        Button replay = UIFactory.CreateButton(_card, "REPLAY", ButtonSize, CenterAnchor,
            new Vector2(0f, -180f), new Color(0.20f, 0.50f, 0.90f));
        replay.onClick.AddListener(() => ReplayClicked?.Invoke());

        _dim.SetActive(false);
    }

    private void OnDestroy()
    {
        _showTween?.Kill();
    }

    public void Show()
    {
        _showTween?.Kill();
        _dim.SetActive(true);

        _card.localScale = Vector3.zero;
        _showTween = _card
            .DOScale(Vector3.one, _showDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    public void Hide()
    {
        _showTween?.Kill();
        _dim.SetActive(false);
    }
}