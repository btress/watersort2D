using System;
using System.Collections.Generic;
using UnityEngine;

public class WaterSortController : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    private readonly List<BottleController> _bottles = new List<BottleController>();
    private BottleController _selected;
    private bool _isBusy;
    private bool _inputEnabled;

    public event Action LevelCompleted;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void OnDestroy()
    {
        Clear();
    }

    private void Update()
    {
        if (!_inputEnabled || _isBusy) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 point = _camera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.zero);
        if (hit.collider == null) return;

        if (hit.collider.TryGetComponent(out BottleController bottle))
        {
            HandleBottleClick(bottle);
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    public void Setup(List<BottleController> bottles)
    {
        Clear();

        for (int i = 0; i < bottles.Count; i++)
        {
            bottles[i].StreamStarted += HandleStreamStarted;
            bottles[i].PourFinished += HandlePourFinished;
            _bottles.Add(bottles[i]);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < _bottles.Count; i++)
        {
            _bottles[i].StreamStarted -= HandleStreamStarted;
            _bottles[i].PourFinished -= HandlePourFinished;
        }

        _bottles.Clear();
        _selected = null;
        _isBusy = false;
    }

    private void HandleBottleClick(BottleController bottle)
    {
        if (bottle.IsLocked) return;

        if (_selected == null)
        {
            if (bottle.IsEmpty)
            {
                AudioManager.Play(SfxType.Error);
                return;
            }

            _selected = bottle;
            _selected.SetSelected(true);
            AudioManager.Play(SfxType.Click);
        }
        else if (_selected == bottle)
        {
            _selected.SetSelected(false);
            _selected = null;
            AudioManager.Play(SfxType.Click);
        }
        else
        {
            TryPour(_selected, bottle);
        }
    }

    private void TryPour(BottleController source, BottleController target)
    {
        source.SetSelected(false);
        _selected = null;

        if (!target.CanReceive(source.TopColor))
        {
            AudioManager.Play(SfxType.Error);
            return;
        }

        _isBusy = true;
        AudioManager.Play(SfxType.Click);
        source.PourInto(target);
    }

    private void HandleStreamStarted(BottleController source)
    {
        AudioManager.Play(SfxType.Pour);
    }

    private void HandlePourFinished(BottleController source, BottleController target)
    {
        if (target.IsComplete && !target.IsLocked)
        {
            target.Lock();
            target.PlayCompleteEffect();
            AudioManager.Play(SfxType.Complete);
        }

        _isBusy = false;

        if (AllBottlesSolved())
        {
            _inputEnabled = false;
            LevelCompleted?.Invoke();
        }
    }

    private bool AllBottlesSolved()
    {
        for (int i = 0; i < _bottles.Count; i++)
        {
            if (!_bottles[i].IsEmpty && !_bottles[i].IsComplete) return false;
        }
        return true;
    }
}
