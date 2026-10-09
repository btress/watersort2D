using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Water Sort/Level Database")]
public class LevelDatabaseSO : ScriptableObject
{
    public List<LevelDataSO> levels = new List<LevelDataSO>();

    public int Count => levels.Count;

    public LevelDataSO GetLevel(int index)
    {
        return index >= 0 && index < levels.Count ? levels[index] : null;
    }
}
