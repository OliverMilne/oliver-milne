using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InitialiserScript : MonoBehaviour
{
    private static InitialiserScript instance;
    public static InitialiserScript Instance { get { return instance; } }

    // Debug checkers
    public int spawnCount = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null) { instance = this; }
        InitialiseNewGame();
    }
    public void InitialiseLoadGame()
    {
        // Wipe all preexisting stuff
        SelectorScript.Instance.ClearSelection();
        LocatableObject.WipeAllLocatableObjectsAndReset();
        Debug.Log("LocatableObjectsByID count = " + LocatableObject.locatableObjectsById.Count);
        CurrentGameState.Instance.LoadGame();


        // Initialise scripts from GameStateInfo
        SpawnerScript.Instance.SpawnerScript_Initialise();
        PlayerSetupScript.Instance.PlayerSetupScript_InitialiseFromGameStateInfo();
        TurnManagerScript.Instance.TurnManagerScript_InitialiseFromGameStateInfo();

        // This is the point at which all the LocatableObjects have to be created
        spawnCount = 0;
        foreach (int locatableID in CurrentGameState.Instance.gameStateInfo.locatablesInfoDict.Keys)
        {
            if (CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isScenery)
                SceneryManager.Instance.SpawnSceneryFromGameStateInfo(locatableID);
            else if (CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isUnit)
                SpawnerScript.Instance.SpawnUnitFromGameStateInfo(locatableID);
        }
        Debug.Log("Spawned " + spawnCount + " LocatableObjects");
        Debug.Log("LocatableObjectsByID count = " + LocatableObject.locatableObjectsById.Count);

        MapArrayScript.Instance.MapArrayScript_InitialiseFromGameStateInfo();
        MouseBehaviourScript.Instance.MouseBehaviourScript_Initialise();
        UIControlScript.Instance.UIControlScript_Initialise();
        PlayerAIMasterScript.Instance.PlayerAIMasterScript_Initialise();
        VictoryConditions.Instance.VictoryConditions_Initialise();

        try { OnLoadGame(); } catch { }
    }
    public void InitialiseNewGame()
    {
        // OverlayGraphicsScript doesn't rely on anything else to initialise, and wiping stuff needs it
        OverlayGraphicsScript.Instance.OverlayGraphicsScript_Initialise();
        
        // Wipe all preexisting stuff
        SelectorScript.Instance.ClearSelection();
        LocatableObject.WipeAllLocatableObjectsAndReset();
        CurrentGameState.Instance.NewGame();

        // Initialise scripts in dependency-safe order
        SpawnerScript.Instance.SpawnerScript_Initialise();
        PlayerSetupScript.Instance.PlayerSetupScript_Initialise();
        TurnManagerScript.Instance.TurnManagerScript_Initialise();
        MapArrayScript.Instance.MapArrayScript_Initialise();
        MouseBehaviourScript.Instance.MouseBehaviourScript_Initialise();
        UIControlScript.Instance.UIControlScript_Initialise();
        PlayerAIMasterScript.Instance.PlayerAIMasterScript_Initialise();
        InitialDebugScript.Instance.InitialDebugScript_Initialise();
        VictoryConditions.Instance.VictoryConditions_Initialise();

        // Once everything's set up, start the first turn
        TurnManagerScript.Instance.StartFirstTurn();
    }
    // Event to let stuff hook on to
    public delegate void OnLoadGameCallback();
    public event OnLoadGameCallback OnLoadGame;
}
