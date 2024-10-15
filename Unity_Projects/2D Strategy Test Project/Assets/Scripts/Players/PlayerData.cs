using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>
/// This should only be accessed by the corresponding PlayerProperties, to protect its integrity.
/// </summary>
public class PlayerData
{
    public int actions = 0;
    public Dictionary<int, AIMission> aIMissions = new();
    public bool isHumanPlayer = false;
    public bool isEnvironment = false;
    public string playerName;
    public float[] playerColor = new float[4];
    public Dictionary<int, int?> objectMissionAssignment = new();
    public ObservableCollection<int> ownedObjectIds = new();
}
