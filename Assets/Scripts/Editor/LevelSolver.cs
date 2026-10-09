using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelSolver
{
    private const int MaxNodes = 300000;

    public static bool IsSolvable(List<List<int>> bottles, int capacity)
    {
        List<int>[] state = new List<int>[bottles.Count];
        for (int i = 0; i < state.Length; i++) state[i] = new List<int>(bottles[i]);

        int nodes = 0;
        return Search(state, new HashSet<string>(), capacity, ref nodes);
    }

    private static bool Search(List<int>[] state, HashSet<string> visited, int capacity, ref int nodes)
    {
        if (IsSolved(state, capacity)) return true;
        if (++nodes > MaxNodes) return false;
        if (!visited.Add(Encode(state))) return false;

        for (int from = 0; from < state.Length; from++)
        {
            List<int> source = state[from];
            if (source.Count == 0 || IsCompleted(source, capacity)) continue;

            int color = source[source.Count - 1];
            int run = TopRun(source);

            for (int to = 0; to < state.Length; to++)
            {
                if (to == from) continue;

                List<int> destination = state[to];
                if (destination.Count >= capacity) continue;
                if (destination.Count > 0 && destination[destination.Count - 1] != color) continue;
                if (destination.Count == 0 && run == source.Count) continue;

                int amount = Mathf.Min(run, capacity - destination.Count);
                source.RemoveRange(source.Count - amount, amount);
                for (int k = 0; k < amount; k++) destination.Add(color);

                bool solved = Search(state, visited, capacity, ref nodes);

                destination.RemoveRange(destination.Count - amount, amount);
                for (int k = 0; k < amount; k++) source.Add(color);

                if (solved) return true;
            }
        }
        return false;
    }

    private static bool IsSolved(List<int>[] state, int capacity)
    {
        for (int i = 0; i < state.Length; i++)
        {
            if (state[i].Count != 0 && !IsCompleted(state[i], capacity)) return false;
        }
        return true;
    }

    private static bool IsCompleted(List<int> bottle, int capacity)
    {
        return bottle.Count == capacity && TopRun(bottle) == capacity;
    }

    private static int TopRun(List<int> bottle)
    {
        int color = bottle[bottle.Count - 1];
        int run = 1;
        for (int i = bottle.Count - 2; i >= 0 && bottle[i] == color; i--) run++;
        return run;
    }

    private static string Encode(List<int>[] state)
    {
        string[] parts = new string[state.Length];
        for (int i = 0; i < state.Length; i++) parts[i] = string.Join(",", state[i]);
        Array.Sort(parts, StringComparer.Ordinal);
        return string.Join("|", parts);
    }
}
