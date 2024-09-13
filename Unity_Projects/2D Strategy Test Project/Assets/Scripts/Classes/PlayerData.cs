using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    public int actions = 0;
    public bool isHumanPlayer = false;
    public bool isEnvironment = false;
    public string playerName;
    public float[] playerColor = new float[4];
    public List<int> ownedObjectIds = new List<int>();
}
