using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateData
{
    public GameStateData() { }

    // Dictionaries containing game state information
    public Dictionary<string, IDDispenser> iDDispensers
        = new Dictionary<string, IDDispenser>()
        {
            { "AIMission", new IDDispenser() },
            { "LocatableObject", new IDDispenser(1) } // I forget why this starts at 1 but it's important
        };
    public Dictionary<int, LocatableData> locatableDataDict = new Dictionary<int, LocatableData>();
    public Dictionary<int, PlayerData> playerDataDict = new Dictionary<int, PlayerData>();
    public Dictionary<int, SceneryData> sceneryDataDict = new Dictionary<int, SceneryData>();
    public Dictionary<string, int> turnData = new Dictionary<string, int>();
    public Dictionary<int, UnitData> unitDataDict = new Dictionary<int, UnitData>();

    // Misc individual pieces of game state information
    public CameraMovementData cameraMovementData = new CameraMovementData();
    public MapData mapData = new MapData();
}
