using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateData
{
    public GameStateData() { }

    // Dictionaries containing game state information
    public Dictionary<string, IDDispenser> iDDispensers
        = new()
        {
            { "AIMission", new IDDispenser() },
            { "LocatableObject", new IDDispenser(1) }, // to avoid duplicating ID 0
            // playerID actually initialises by dispensing, so the 1 below is just to avoid having a Player 0
            { "PlayerProperties", new IDDispenser(1) }, 
            { "TileArrayEntry", new IDDispenser() }
        };
    public Dictionary<int, LocatableData> locatableDataDict = new();
    public Dictionary<string, int> miscIntsDataDict = new();
    public Dictionary<int, PlayerData> playerDataDict = new();
    public Dictionary<int, SceneryData> sceneryDataDict = new();
    public Dictionary<string, int> turnData = new();
    public Dictionary<int, UnitData> unitDataDict = new();

    // Misc individual pieces of game state information
    public CameraMovementData cameraMovementData = new();
    public MapData mapData = new();
}
