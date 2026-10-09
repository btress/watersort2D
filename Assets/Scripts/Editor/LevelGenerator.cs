using System.Collections.Generic;
using UnityEngine;

public static class LevelGenerator
{
    public static readonly Color[] Palette =
    {
        new Color(0.85f, 0.10f, 0.10f),
        new Color(1.00f, 0.92f, 0.00f),
        new Color(0.10f, 0.35f, 1.00f),
        new Color(0.50f, 0.05f, 0.65f),
        new Color(0.10f, 0.75f, 0.25f),
        new Color(1.00f, 0.50f, 0.00f),
        new Color(0.00f, 0.80f, 0.85f),
        new Color(1.00f, 0.40f, 0.70f)
    };

    public static List<BottleData> Generate(int bottleCount, int colorCount, int maxAttempts = 300)
    {
        int capacity = BottleController.Capacity;
        colorCount = Mathf.Clamp(colorCount, 1, Mathf.Min(Palette.Length, bottleCount - 1));

        List<List<int>> layout = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            layout = BuildRandomLayout(bottleCount, colorCount, capacity);
            if (HasCompletedBottle(layout, capacity)) continue;
            if (LevelSolver.IsSolvable(layout, capacity)) return ToBottleData(layout);
        }

        Debug.LogWarning("LevelGenerator: không xác nhận được level giải được sau nhiều lần thử, hãy bấm Generate lại.");
        return ToBottleData(layout);
    }

    private static List<List<int>> BuildRandomLayout(int bottleCount, int colorCount, int capacity)
    {
        List<int> paletteOrder = new List<int>();
        for (int i = 0; i < Palette.Length; i++) paletteOrder.Add(i);
        Shuffle(paletteOrder);

        List<int> units = new List<int>();
        for (int c = 0; c < colorCount; c++)
        {
            for (int k = 0; k < capacity; k++) units.Add(paletteOrder[c]);
        }
        Shuffle(units);

        List<List<int>> layout = new List<List<int>>();
        for (int b = 0; b < bottleCount; b++) layout.Add(new List<int>());
        for (int i = 0; i < units.Count; i++) layout[i / capacity].Add(units[i]);

        Shuffle(layout);
        return layout;
    }

    private static bool HasCompletedBottle(List<List<int>> layout, int capacity)
    {
        for (int i = 0; i < layout.Count; i++)
        {
            List<int> bottle = layout[i];
            if (bottle.Count != capacity) continue;

            bool uniform = true;
            for (int k = 1; k < capacity; k++)
            {
                if (bottle[k] != bottle[0]) uniform = false;
            }
            if (uniform) return true;
        }
        return false;
    }

    private static List<BottleData> ToBottleData(List<List<int>> layout)
    {
        List<BottleData> result = new List<BottleData>();
        for (int i = 0; i < layout.Count; i++)
        {
            BottleData data = new BottleData();
            for (int k = 0; k < layout[i].Count; k++) data.colors.Add(Palette[layout[i][k]]);
            result.Add(data);
        }
        return result;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
