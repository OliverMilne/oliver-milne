using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateInfo
{
    public GameStateInfo() { }

    // Dictionaries containing game state information
    public Dictionary<int, LocatableData> locatablesInfoDict = new Dictionary<int, LocatableData>();
    public Dictionary<string, int> turnInfo = new Dictionary<string, int>();
    public Dictionary<int, PlayerData> playerDataDict = new Dictionary<int, PlayerData>();
    public Dictionary<int, SceneryData> sceneryDataDict = new Dictionary<int, SceneryData>();
    public Dictionary<int, UnitData> unitDataDict = new Dictionary<int, UnitData>();

    // Misc individual pieces of game state information
    public CameraMovementData cameraMovementData = new CameraMovementData();
    public MapData mapData = new MapData();
}
