using UnityEditor;
using UnityEngine;

public static class LevelAssetCreator
{
    private const string RootFolder = "Assets/ScriptableObjects";
    private const string LevelFolder = RootFolder + "/Levels";
    private const string DatabasePath = RootFolder + "/LevelDatabase.asset";

    // { số ống, số màu }
    private static readonly int[,] Specs = { { 4, 3 }, { 5, 4 }, { 7, 5 }, { 8, 6 }, { 9, 7 } };

    [MenuItem("Tools/Water Sort/Create Sample Levels")]
    public static void CreateSampleLevels()
    {
        if (AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(DatabasePath) != null &&
            !EditorUtility.DisplayDialog("Create Sample Levels",
                "Đã có LevelDatabase. Tạo lại sẽ ghi đè dữ liệu 5 level mẫu. Tiếp tục?", "Ghi đè", "Hủy"))
        {
            return;
        }

        EnsureFolder("Assets", "ScriptableObjects");
        EnsureFolder(RootFolder, "Levels");

        LevelDatabaseSO database = LoadOrCreate<LevelDatabaseSO>(DatabasePath);
        database.levels.Clear();

        for (int i = 0; i < Specs.GetLength(0); i++)
        {
            LevelDataSO level = LoadOrCreate<LevelDataSO>($"{LevelFolder}/Level_{i + 1:00}.asset");
            level.levelIndex = i + 1;
            level.bottles = LevelGenerator.Generate(Specs[i, 0], Specs[i, 1]);
            EditorUtility.SetDirty(level);
            database.levels.Add(level);
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = database;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
