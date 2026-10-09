using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private const string LevelPrefKey = "WaterSort.CurrentLevel";
    private const int MaxSingleRowBottles = 3;
    private const int ThreeRowBottleCount = 9;

    [SerializeField] private LevelDatabaseSO _database;
    [SerializeField] private ObjectPooler _pooler;
    [SerializeField] private WaterSortController _waterSort;
    [SerializeField] private UIManager _ui;
    [SerializeField] private CameraFitter _cameraFitter;
    [SerializeField] private Transform _boardCenter;
    [SerializeField] private Vector2 _bottleSpacing = new Vector2(1.2f, 1.5f);
    [SerializeField] private float _winPopupDelay = 1.3f;

    private readonly List<BottleController> _activeBottles = new List<BottleController>();
    private WaitForSeconds _winWait;
    private Coroutine _winRoutine;

    public int CurrentLevelIndex { get; private set; }

    private void Start()
    {
        _winWait = new WaitForSeconds(_winPopupDelay);

        _ui.PlayClicked += HandlePlayClicked;
        _ui.ResetClicked += HandleResetClicked;
        _ui.WinPopup.NextClicked += HandleNextClicked;
        _ui.WinPopup.ReplayClicked += HandleReplayClicked;
        _waterSort.LevelCompleted += HandleLevelCompleted;

        CurrentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(LevelPrefKey, 0), 0, _database.Count - 1);

        _ui.ShowStart(true);
        _ui.ShowHud(false);
        LoadLevel(CurrentLevelIndex);
        _waterSort.SetInputEnabled(false);
    }

    private void OnDestroy()
    {
        if (_ui != null)
        {
            _ui.PlayClicked -= HandlePlayClicked;
            _ui.ResetClicked -= HandleResetClicked;
            if (_ui.WinPopup != null)
            {
                _ui.WinPopup.NextClicked -= HandleNextClicked;
                _ui.WinPopup.ReplayClicked -= HandleReplayClicked;
            }
        }

        if (_waterSort != null) _waterSort.LevelCompleted -= HandleLevelCompleted;
    }

    private void LoadLevel(int index)
    {
        if (_winRoutine != null)
        {
            StopCoroutine(_winRoutine);
            _winRoutine = null;
        }

        ReleaseBoard();

        LevelDataSO level = _database.GetLevel(index);
        if (level == null)
        {
            Debug.LogError($"LevelManager: không tìm thấy level index {index}.");
            return;
        }

        int total = level.bottles.Count;
        for (int i = 0; i < total; i++)
        {
            BottleController bottle = _pooler.Get();
            bottle.Place(GetSlotPosition(i, total));
            bottle.Init(level.bottles[i].colors);
            _activeBottles.Add(bottle);
        }

        FitCamera();
        _waterSort.Setup(_activeBottles);
        _ui.SetLevelNumber(index + 1);

        PlayerPrefs.SetInt(LevelPrefKey, index);
        PlayerPrefs.Save();
    }

    private void FitCamera()
    {
        if (_cameraFitter == null || _activeBottles.Count == 0) return;

        Bounds board = _activeBottles[0].Bounds;
        for (int i = 1; i < _activeBottles.Count; i++)
        {
            board.Encapsulate(_activeBottles[i].Bounds);
        }

        _cameraFitter.Fit(board, _activeBottles[0].Bounds.size);
    }

    private void ReleaseBoard()
    {
        _waterSort.Clear();

        for (int i = 0; i < _activeBottles.Count; i++)
        {
            _pooler.Release(_activeBottles[i]);
        }
        _activeBottles.Clear();
    }

    private Vector3 GetSlotPosition(int index, int total)
    {
        int rows = total <= MaxSingleRowBottles ? 1 : (total >= ThreeRowBottleCount ? 3 : 2);
        int columns = Mathf.CeilToInt(total / (float)rows);
        int row = index / columns;
        int column = index % columns;
        int inRow = Mathf.Min(columns, total - row * columns);

        float x = (column - (inRow - 1) * 0.5f) * _bottleSpacing.x;
        float y = -(row - (rows - 1) * 0.5f) * _bottleSpacing.y;
        return _boardCenter.position + new Vector3(x, y, 0f);
    }

    private void StartCurrentLevel()
    {
        LoadLevel(CurrentLevelIndex);
        _ui.ShowHud(true);
        _waterSort.SetInputEnabled(true);
    }

    private void HandlePlayClicked()
    {
        AudioManager.Play(SfxType.Click);
        _ui.ShowStart(false);
        _ui.ShowHud(true);
        _waterSort.SetInputEnabled(true);
    }

    private void HandleResetClicked()
    {
        AudioManager.Play(SfxType.Click);
        StartCurrentLevel();
    }

    private void HandleLevelCompleted()
    {
        _winRoutine = StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        _ui.ShowHud(false);
        AudioManager.Play(SfxType.Win);
        yield return _winWait;
        _ui.WinPopup.Show();
        _winRoutine = null;
    }

    private void HandleNextClicked()
    {
        AudioManager.Play(SfxType.Click);
        _ui.WinPopup.Hide();
        CurrentLevelIndex = (CurrentLevelIndex + 1) % _database.Count;
        StartCurrentLevel();
    }

    private void HandleReplayClicked()
    {
        AudioManager.Play(SfxType.Click);
        _ui.WinPopup.Hide();
        StartCurrentLevel();
    }
}
