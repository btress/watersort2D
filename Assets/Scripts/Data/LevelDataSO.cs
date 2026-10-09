using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BottleData
{
    // Màu từ đáy lên miệng ống
    public List<Color> colors = new List<Color>();
}

[CreateAssetMenu(fileName = "Level_01", menuName = "Water Sort/Level Data")]
public class LevelDataSO : ScriptableObject
{
    public int levelIndex;
    public List<BottleData> bottles = new List<BottleData>();
}
