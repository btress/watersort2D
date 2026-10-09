using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDataSO))]
public class LevelDataSOEditor : Editor
{
    private const float PreviewBottleWidth = 36f;
    private const float PreviewBottleHeight = 100f;
    private const float PreviewGap = 8f;

    private readonly List<string> _errors = new List<string>();
    private readonly List<string> _warnings = new List<string>();

    private int _bottleCount = 4;
    private int _colorCount = 3;
    private bool _hasValidated;

    public override void OnInspectorGUI()
    {
        LevelDataSO level = (LevelDataSO)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Level Tool", EditorStyles.boldLabel);
        _bottleCount = EditorGUILayout.IntSlider("Số ống", _bottleCount, 3, 12);
        _colorCount = EditorGUILayout.IntSlider("Số màu", _colorCount, 1, LevelGenerator.Palette.Length);

        int emptyBottles = _bottleCount - _colorCount;
        if (emptyBottles < 1)
        {
            EditorGUILayout.HelpBox("Số ống phải lớn hơn số màu ít nhất 1 (cần ống trống).", MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Random Solvable Level")) Generate(level);
        if (GUILayout.Button("Validate Level")) Validate(level);
        if (GUILayout.Button("Clear Data")) Clear(level);
        EditorGUILayout.EndHorizontal();

        DrawValidationResult();

        EditorGUILayout.Space();
        DrawPreview(level);
    }

    private void Generate(LevelDataSO level)
    {
        Undo.RecordObject(level, "Generate Level");
        level.bottles = LevelGenerator.Generate(_bottleCount, _colorCount);
        EditorUtility.SetDirty(level);
        Validate(level);
    }

    private void Validate(LevelDataSO level)
    {
        LevelValidator.Validate(level, _errors, _warnings);
        _hasValidated = true;
    }

    private void Clear(LevelDataSO level)
    {
        Undo.RecordObject(level, "Clear Level");
        level.bottles.Clear();
        EditorUtility.SetDirty(level);
        _errors.Clear();
        _warnings.Clear();
        _hasValidated = false;
    }

    private void DrawValidationResult()
    {
        if (!_hasValidated) return;

        for (int i = 0; i < _errors.Count; i++) EditorGUILayout.HelpBox(_errors[i], MessageType.Error);
        for (int i = 0; i < _warnings.Count; i++) EditorGUILayout.HelpBox(_warnings[i], MessageType.Warning);

        if (_errors.Count == 0 && _warnings.Count == 0)
        {
            EditorGUILayout.HelpBox("Level hợp lệ.", MessageType.Info);
        }
    }

    private static void DrawPreview(LevelDataSO level)
    {
        if (level.bottles.Count == 0) return;

        EditorGUILayout.LabelField("Preview (đáy → miệng)", EditorStyles.boldLabel);
        Rect area = GUILayoutUtility.GetRect(0f, PreviewBottleHeight + 8f, GUILayout.ExpandWidth(true));
        float layerHeight = PreviewBottleHeight / BottleController.Capacity;

        for (int i = 0; i < level.bottles.Count; i++)
        {
            Rect bottle = new Rect(area.x + i * (PreviewBottleWidth + PreviewGap), area.y,
                PreviewBottleWidth, PreviewBottleHeight);
            EditorGUI.DrawRect(bottle, new Color(0.15f, 0.15f, 0.15f));

            List<Color> colors = level.bottles[i].colors;
            for (int k = 0; k < colors.Count; k++)
            {
                Rect layer = new Rect(bottle.x + 2f, bottle.yMax - (k + 1) * layerHeight,
                    bottle.width - 4f, layerHeight - 2f);
                EditorGUI.DrawRect(layer, colors[k]);
            }
        }
    }
}
