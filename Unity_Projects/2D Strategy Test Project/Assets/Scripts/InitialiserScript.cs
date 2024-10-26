using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class InitialiserScript : MonoBehaviour
{
    private static InitialiserScript instance;
    public static InitialiserScript Instance { get { return instance; } }
    /// <summary>
    /// This is for preventing stuff like inconvenient multithreading during game initialisation
    /// </summary>
    public bool initialisationInProgress { get; private set; }

    // Debug checkers
    public int spawnCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null) { instance = this; }
        initialisationInProgress = false;
        InitialiseNewGame();
    }
    public void InitialiseLoadGame()
    {
        initialisationInProgress = true;
        // Debug.Log("Initialising load game");
        // Wipe all preexisting stuff
        SelectorScript.Instance.ClearSelection();
        // Debug.Log("Selection cleared for load");
        LocatableObject.WipeAllLocatableObjectsAndReset();
        // Debug.Log("LocatableObjectsByID count = " + LocatableObject.locatableObjectsById.Count);
        CurrentGameState.Instance.LoadGameStateInfo();

        
        // Initialise scripts from GameStateInfo
        UnitSpawnerScript.Instance.SpawnerScript_Initialise();
        PlayerSetupScript.Instance.PlayerSetupScript_InitialiseFromGameStateInfo();
        TurnManagerScript.Instance.TurnManagerScript_InitialiseFromGameStateInfo();
        
        // This is the point at which all the LocatableObjects have to be created
        spawnCount = 0;
        foreach (int locatableID in CurrentGameState.Instance.gameStateData.locatableDataDict.Keys)
        {
            if (CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isScenery)
                SceneryManager.Instance.SpawnSceneryFromGameStateInfo(locatableID);
            else if (CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isUnit)
                UnitSpawnerScript.Instance.SpawnUnitFromGameStateInfo(locatableID);
        }
        // Debug.Log("Spawned " + spawnCount + " LocatableObjects");
        // Debug.Log("LocatableObjectsByID count = " + LocatableObject.locatableObjectsById.Count);

        MapArrayScript.Instance.MapArrayScript_InitialiseFromGameStateInfo();
        MouseBehaviourScript.Instance.MouseBehaviourScript_Initialise();
        UIControlScript.Instance.UIControlScript_Initialise();
        PlayerAIMasterScript.Instance.PlayerAIMasterScript_Initialise();
        VictoryConditions.Instance.VictoryConditions_Initialise();

        try { OnLoadGame(); } catch { }
        initialisationInProgress = false;
    }
    public void InitialiseNewGame()
    {
        initialisationInProgress = true;
        // OverlayGraphicsScript doesn't rely on anything else to initialise, and wiping stuff needs it
        OverlayGraphicsScript.Instance.OverlayGraphicsScript_Initialise();
        
        // Wipe all preexisting stuff
        SelectorScript.Instance.ClearSelection();
        LocatableObject.WipeAllLocatableObjectsAndReset();
        CurrentGameState.Instance.NewGameStateData();

        // Initialise scripts in dependency-safe order
        UnitSpawnerScript.Instance.SpawnerScript_Initialise();
        PlayerSetupScript.Instance.PlayerSetupScript_Initialise();
        TurnManagerScript.Instance.TurnManagerScript_Initialise();
        MapArrayScript.Instance.MapArrayScript_Initialise();
        MouseBehaviourScript.Instance.MouseBehaviourScript_Initialise();
        UIControlScript.Instance.UIControlScript_Initialise();
        PlayerAIMasterScript.Instance.PlayerAIMasterScript_Initialise();
        StartingUnitSpawner.Instance.StartingUnitSpawner_Initialise();
        VictoryConditions.Instance.VictoryConditions_Initialise();

        // Once everything's set up, start the first turn
        TurnManagerScript.Instance.StartFirstTurn();
        initialisationInProgress = false;
    }
    // Event to let stuff hook on to
    public delegate void OnLoadGameCallback();
    public event OnLoadGameCallback OnLoadGame;
}
