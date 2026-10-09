using UnityEditor;
using UnityEngine;

// Khôi phục giá trị cấu hình gốc của prefab Bottle (đọc từ file Bottle.prefab cũ)
public static class RestoreBottleValues
{
    [MenuItem("Tools/Water Sort/Restore Bottle Values")]
    public static void Restore()
    {
        GameObject selected = Selection.activeGameObject;
        BottleController bottle = selected != null ? selected.GetComponent<BottleController>() : null;
        if (bottle == null)
        {
            EditorUtility.DisplayDialog("Restore Bottle Values",
                "Hãy chọn prefab Bottle (trong Project) hoặc object Bottle có BottleController rồi chạy lại.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(bottle);

        so.FindProperty("_scaleAndRotationCurve").animationCurveValue = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(90f, 0.47f, -0.012098228f, -0.012098228f));

        so.FindProperty("_fillAmountCurve").animationCurveValue = new AnimationCurve(
            new Keyframe(0f, 0.34f, 0f, 0f),
            new Keyframe(30f, 0.34f, 0f, 0f),
            new Keyframe(54f, 0.13f, -0.011886792f, -0.011886792f),
            new Keyframe(83f, -0.29f, -0.0175f, -0.0175f),
            new Keyframe(90f, -0.5f, -0.033300724f, -0.033300724f));

        so.FindProperty("_rotationSpeedCurve").animationCurveValue = new AnimationCurve(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(90f, 0.2f, -0.02777883f, -0.02777883f));

        SetFloatArray(so.FindProperty("_fillAmounts"), new[] { -0.5f, -0.29f, -0.08f, 0.13f, 0.34f });
        SetFloatArray(so.FindProperty("_rotationValues"), new[] { 54f, 71f, 83f, 90f });
        so.FindProperty("_timeToRotate").floatValue = 1.5f;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bottle);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã khôi phục giá trị cho BottleController.", bottle);
    }

    private static void SetFloatArray(SerializedProperty property, float[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).floatValue = values[i];
        }
    }
}
