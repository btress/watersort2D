using System.Collections.Generic;
using UnityEngine;

public static class LevelValidator
{
    private const int MinEmptyBottles = 1;
    private const int MaxEmptyBottles = 2;

    public static void Validate(LevelDataSO level, List<string> errors, List<string> warnings)
    {
        errors.Clear();
        warnings.Clear();

        int capacity = BottleController.Capacity;
        if (level.bottles.Count == 0)
        {
            errors.Add("Level chưa có ống nào.");
            return;
        }

        int totalUnits = 0;
        int emptyBottles = 0;
        Dictionary<Color, int> colorCounts = new Dictionary<Color, int>();

        for (int i = 0; i < level.bottles.Count; i++)
        {
            List<Color> colors = level.bottles[i].colors;

            if (colors.Count > capacity) errors.Add($"Ống {i + 1} có {colors.Count} tầng (tối đa {capacity}).");
            if (colors.Count == 0) emptyBottles++;

            totalUnits += colors.Count;
            for (int k = 0; k < colors.Count; k++)
            {
                colorCounts.TryGetValue(colors[k], out int count);
                colorCounts[colors[k]] = count + 1;
            }

            if (colors.Count == capacity && IsUniform(colors))
            {
                warnings.Add($"Ống {i + 1} đã hoàn thành sẵn ngay từ đầu.");
            }
        }

        if (totalUnits % capacity != 0)
        {
            errors.Add($"Tổng số đơn vị màu ({totalUnits}) không chia hết cho {capacity}.");
        }

        foreach (KeyValuePair<Color, int> pair in colorCounts)
        {
            if (pair.Value != capacity)
            {
                errors.Add($"Màu #{ColorUtility.ToHtmlStringRGB(pair.Key)} có {pair.Value} đơn vị (cần đúng {capacity}).");
            }
        }

        if (emptyBottles < MinEmptyBottles || emptyBottles > MaxEmptyBottles)
        {
            errors.Add($"Số ống trống là {emptyBottles}, cần từ {MinEmptyBottles} đến {MaxEmptyBottles}.");
        }

        if (errors.Count == 0 && !IsSolvable(level, capacity))
        {
            warnings.Add("Solver không tìm được lời giải cho level này (có thể không giải được).");
        }
    }

    private static bool IsUniform(List<Color> colors)
    {
        for (int i = 1; i < colors.Count; i++)
        {
            if (colors[i] != colors[0]) return false;
        }
        return true;
    }

    private static bool IsSolvable(LevelDataSO level, int capacity)
    {
        Dictionary<Color, int> ids = new Dictionary<Color, int>();
        List<List<int>> layout = new List<List<int>>();

        for (int i = 0; i < level.bottles.Count; i++)
        {
            List<int> bottle = new List<int>();
            foreach (Color color in level.bottles[i].colors)
            {
                if (!ids.TryGetValue(color, out int id))
                {
                    id = ids.Count;
                    ids[color] = id;
                }
                bottle.Add(id);
            }
            layout.Add(bottle);
        }

        return LevelSolver.IsSolvable(layout, capacity);
    }
}
