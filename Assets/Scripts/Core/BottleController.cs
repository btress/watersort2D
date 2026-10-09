using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class BottleController : MonoBehaviour
{
    public const int Capacity = 4;

    private const int SortingBoost = 2;
    private const float FillEpsilon = 0.005f;
    private const float GlowScale = 1f;
    private const float GlowPulseScale = 1.22f;
    private const float GlowPulseDuration = 0.45f;

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int RotationMultiplierId = Shader.PropertyToID("_SARM");
    private static readonly int[] ColorIds =
    {
        Shader.PropertyToID("_C1"), Shader.PropertyToID("_C2"),
        Shader.PropertyToID("_C3"), Shader.PropertyToID("_C4")
    };
    private static readonly Color SelectedTint = new Color(1f, 0.93f, 0.25f, 1f);
    private static readonly Color GlowColor = new Color(1f, 0.93f, 0.25f, 0.45f);

    [Header("References")]
    [SerializeField, FormerlySerializedAs("bottleMaskSR")] private SpriteRenderer _bottleMaskRenderer;
    [SerializeField, FormerlySerializedAs("lineRenderer")] private LineRenderer _streamRenderer;
    [SerializeField] private Transform _mouthPoint;
    [SerializeField, FormerlySerializedAs("leftRotationPoint")] private Transform _leftRotationPoint;
    [SerializeField, FormerlySerializedAs("rightRotationPoint")] private Transform _rightRotationPoint;

    [Header("Animation")]
    [SerializeField, FormerlySerializedAs("ScaleAndRontationMultiplierCurve")] private AnimationCurve _scaleAndRotationCurve;
    [SerializeField, FormerlySerializedAs("FillAmountCurve")] private AnimationCurve _fillAmountCurve;
    [SerializeField, FormerlySerializedAs("fillAmounts")] private float[] _fillAmounts;
    [SerializeField, FormerlySerializedAs("rotationValues")] private float[] _rotationValues;
    [SerializeField, FormerlySerializedAs("timeToRotate")] private float _timeToRotate = 1f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;
    [SerializeField] private Ease _tiltEase = Ease.InOutSine;

    private readonly Color[] _layers = new Color[Capacity];

    private SpriteRenderer _spriteRenderer;
    private SpriteRenderer _glowRenderer;
    private Material _maskMaterial;
    private BottleConfetti _confetti;

    private int _baseSpriteOrder;
    private int _baseMaskOrder;
    private Vector3 _homePosition;

    private bool _isConfigured;
    private int _layerCount;
    private Color _topColor;
    private int _topLayerCount;

    private BottleController _target;
    private Transform _pivot;
    private float _direction = 1f;
    private int _transferCount;
    private int _rotationIndex;

    private Sequence _pourSequence;
    private Tween _glowTween;
    private float _lastAngle;
    private bool _streaming;

    public event Action<BottleController> StreamStarted;
    public event Action<BottleController, BottleController> PourFinished;

    public int LayerCount => _layerCount;
    public Color TopColor => _topColor;
    public int TopLayerCount => _topLayerCount;
    public bool IsEmpty => _layerCount == 0;
    public bool IsComplete => _layerCount == Capacity && _topLayerCount == Capacity;
    public bool IsLocked { get; private set; }
    public Bounds Bounds => _spriteRenderer.bounds;

    private float SurfaceY
    {
        get
        {
            Bounds bounds = _spriteRenderer.bounds;
            return Mathf.Lerp(bounds.min.y, bounds.max.y, (float)_layerCount / Capacity);
        }
    }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _maskMaterial = _bottleMaskRenderer.material;
        _baseSpriteOrder = _spriteRenderer.sortingOrder;
        _baseMaskOrder = _bottleMaskRenderer.sortingOrder;

        _isConfigured = ValidateConfiguration();

        EnsureMouthPoint();
        CreateGlow();

        _confetti = gameObject.AddComponent<BottleConfetti>();
        _confetti.Build(_mouthPoint, _spriteRenderer.sharedMaterial);

        if (_streamRenderer != null) _streamRenderer.enabled = false;
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
        if (_maskMaterial != null) Destroy(_maskMaterial);
    }

    public void Place(Vector3 position)
    {
        _homePosition = position;
        transform.position = position;
    }

    public void Init(List<Color> colors)
    {
        if (!_isConfigured) return;

        ResetState();

        _layerCount = Mathf.Min(colors.Count, Capacity);
        for (int i = 0; i < Capacity; i++)
        {
            _layers[i] = i < _layerCount ? colors[i] : Color.clear;
        }

        ApplyColorsToShader();
        _maskMaterial.SetFloat(FillAmountId, _fillAmounts[_layerCount]);
        _maskMaterial.SetFloat(RotationMultiplierId, Evaluate(_scaleAndRotationCurve, 0f));
        RefreshTopInfo();
    }

    public void ResetForPool()
    {
        ResetState();
        _layerCount = 0;
        _topLayerCount = 0;
    }

    public bool CanReceive(Color color)
    {
        if (IsLocked || _layerCount >= Capacity) return false;
        return _layerCount == 0 || _topColor == color;
    }

    public void SetSelected(bool selected)
    {
        _spriteRenderer.color = selected ? SelectedTint : Color.white;

        _glowTween?.Kill();
        _glowTween = null;

        if (selected)
        {
            _glowRenderer.sortingOrder = Mathf.Min(_spriteRenderer.sortingOrder, _bottleMaskRenderer.sortingOrder) - 1;
            _glowRenderer.transform.localScale = Vector3.one * GlowScale;
            _glowRenderer.gameObject.SetActive(true);

            // Glow nhấp nháy nhẹ khi đang được chọn
            _glowTween = _glowRenderer.transform
                .DOScale(GlowPulseScale, GlowPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }
        else
        {
            _glowRenderer.gameObject.SetActive(false);
        }
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void PlayCompleteEffect()
    {
        _confetti.Play();
    }

    public void PourInto(BottleController target)
    {
        _target = target;
        _transferCount = Mathf.Min(_topLayerCount, Capacity - target._layerCount);
        _rotationIndex = Capacity - 1 - (_layerCount - _transferCount);
        ChoosePivot();

        target.ReceiveLayers(_topColor, _transferCount);

        _spriteRenderer.sortingOrder += SortingBoost;
        _bottleMaskRenderer.sortingOrder += SortingBoost;

        BuildPourSequence();
    }

    private void BuildPourSequence()
    {
        Vector3 approachPosition = _pivot == _leftRotationPoint
            ? _target._rightRotationPoint.position
            : _target._leftRotationPoint.position;

        float moveDuration = 1f / Mathf.Max(0.01f, _moveSpeed);
        float finalAngle = _direction * _rotationValues[_rotationIndex];

        _pourSequence?.Kill();
        _pourSequence = DOTween.Sequence()
            .SetLink(gameObject)
            // 1. Bay tới chai đích
            .Append(transform.DOMove(approachPosition, moveDuration).SetEase(_moveEase))
            // 2. Nghiêng chai + rót
            .AppendCallback(() =>
            {
                _lastAngle = 0f;
                _streaming = false;
            })
            .Append(DOVirtual.Float(0f, finalAngle, _timeToRotate, OnTiltPourUpdate).SetEase(_tiltEase))
            .AppendCallback(() => OnTiltPourComplete(finalAngle))
            // 3. Nghiêng về
            .AppendCallback(() => _lastAngle = finalAngle)
            .Append(DOVirtual.Float(finalAngle, 0f, _timeToRotate, OnTiltBackUpdate).SetEase(_tiltEase))
            .AppendCallback(OnTiltBackComplete)
            // 4. Bay về vị trí cũ
            .Append(transform.DOMove(_homePosition, moveDuration).SetEase(_moveEase))
            .OnComplete(FinishPour);
    }

    private void OnTiltPourUpdate(float angle)
    {
        transform.RotateAround(_pivot.position, Vector3.forward, _lastAngle - angle);
        _maskMaterial.SetFloat(RotationMultiplierId, Evaluate(_scaleAndRotationCurve, angle));

        float fill = Evaluate(_fillAmountCurve, angle);
        if (_fillAmounts[_layerCount] > fill + FillEpsilon)
        {
            if (!_streaming)
            {
                _streaming = true;
                StartStream();
            }

            _maskMaterial.SetFloat(FillAmountId, fill);
            _target.AddFill(Evaluate(_fillAmountCurve, _lastAngle) - fill);
        }

        _lastAngle = angle;
    }

    private void OnTiltPourComplete(float finalAngle)
    {
        _maskMaterial.SetFloat(RotationMultiplierId, Evaluate(_scaleAndRotationCurve, finalAngle));
        _maskMaterial.SetFloat(FillAmountId, Evaluate(_fillAmountCurve, finalAngle));

        _layerCount -= _transferCount;
        _target._layerCount += _transferCount;

        if (_streamRenderer != null) _streamRenderer.enabled = false;
    }

    private void OnTiltBackUpdate(float angle)
    {
        transform.RotateAround(_pivot.position, Vector3.forward, _lastAngle - angle);
        _maskMaterial.SetFloat(RotationMultiplierId, Evaluate(_scaleAndRotationCurve, angle));
        _lastAngle = angle;
    }

    private void OnTiltBackComplete()
    {
        transform.eulerAngles = Vector3.zero;
        _maskMaterial.SetFloat(RotationMultiplierId, Evaluate(_scaleAndRotationCurve, 0f));

        RefreshTopInfo();
        _target.RefreshTopInfo();
    }

    private void FinishPour()
    {
        _spriteRenderer.sortingOrder -= SortingBoost;
        _bottleMaskRenderer.sortingOrder -= SortingBoost;

        BottleController target = _target;
        _target = null;
        _pourSequence = null;
        PourFinished?.Invoke(this, target);
    }

    private void StartStream()
    {
        if (_streamRenderer != null)
        {
            Vector3 start = _pivot.position;
            Vector3 end = new Vector3(start.x, _target.SurfaceY, start.z);

            _streamRenderer.startColor = _topColor;
            _streamRenderer.endColor = _topColor;
            _streamRenderer.SetPosition(0, start);
            _streamRenderer.SetPosition(1, end);
            _streamRenderer.enabled = true;
        }

        StreamStarted?.Invoke(this);
    }

    private void ReceiveLayers(Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _layers[_layerCount + i] = color;
        }
        ApplyColorsToShader();
    }

    private void AddFill(float amount)
    {
        _maskMaterial.SetFloat(FillAmountId, _maskMaterial.GetFloat(FillAmountId) + amount);
    }

    private void ChoosePivot()
    {
        if (transform.position.x > _target.transform.position.x)
        {
            _pivot = _leftRotationPoint;
            _direction = -1f;
        }
        else
        {
            _pivot = _rightRotationPoint;
            _direction = 1f;
        }
    }

    private void RefreshTopInfo()
    {
        if (_layerCount == 0)
        {
            _topLayerCount = 0;
            return;
        }

        _topColor = _layers[_layerCount - 1];
        _topLayerCount = 1;
        for (int i = _layerCount - 2; i >= 0 && _layers[i] == _topColor; i--)
        {
            _topLayerCount++;
        }
    }

    // Curve chỉ định nghĩa cho góc 0..90, nên lấy trị tuyệt đối để xoay sang trái (góc âm) vẫn dùng đúng curve
    private static float Evaluate(AnimationCurve curve, float angle)
    {
        return curve.Evaluate(Mathf.Abs(angle));
    }

    private void ApplyColorsToShader()
    {
        for (int i = 0; i < Capacity; i++)
        {
            _maskMaterial.SetColor(ColorIds[i], _layers[i]);
        }
    }

    private void KillTweens()
    {
        _pourSequence?.Kill();
        _pourSequence = null;
        _glowTween?.Kill();
        _glowTween = null;
    }

    private void ResetState()
    {
        KillTweens();

        _target = null;
        IsLocked = false;
        transform.rotation = Quaternion.identity;
        _spriteRenderer.sortingOrder = _baseSpriteOrder;
        _bottleMaskRenderer.sortingOrder = _baseMaskOrder;
        SetSelected(false);

        if (_streamRenderer != null) _streamRenderer.enabled = false;
        _confetti.Clear();
    }

    private bool ValidateConfiguration()
    {
        bool valid = true;

        if (_fillAmounts == null || _fillAmounts.Length < Capacity + 1)
        {
            Debug.LogError("BottleController: 'Fill Amounts' phải có ít nhất 5 phần tử. Prefab Bottle đã mất dữ liệu cấu hình.", this);
            valid = false;
        }
        if (_rotationValues == null || _rotationValues.Length < Capacity)
        {
            Debug.LogError("BottleController: 'Rotation Values' phải có ít nhất 4 phần tử.", this);
            valid = false;
        }
        if (_leftRotationPoint == null || _rightRotationPoint == null)
        {
            Debug.LogError("BottleController: thiếu Left/Right Rotation Point.", this);
            valid = false;
        }

        return valid;
    }

    private void EnsureMouthPoint()
    {
        if (_mouthPoint != null) return;

        Bounds bounds = _spriteRenderer.bounds;
        Transform mouth = new GameObject("MouthPoint").transform;
        mouth.SetParent(transform, false);
        mouth.position = new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
        _mouthPoint = mouth;
    }

    private void CreateGlow()
    {
        GameObject glow = new GameObject("SelectGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localScale = Vector3.one * GlowScale;

        _glowRenderer = glow.AddComponent<SpriteRenderer>();
        _glowRenderer.sprite = _spriteRenderer.sprite;
        _glowRenderer.color = GlowColor;
        _glowRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
        glow.SetActive(false);
    }
}