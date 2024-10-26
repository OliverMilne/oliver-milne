using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMissionData
{
    public Type missionType;
    public int playerID;

    public int movesTakenThisTurn = 0;

    public Dictionary<string, int> missionDataInts;
    public Dictionary<string, float> missionDataFloats;
    public Dictionary<string, string> missionDataStrings;
}
