using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;

public class PlayerProperties : MonoBehaviour
{
    /// <summary>
    /// The playerID of the human player.
    /// </summary>
    public static int humanPlayerID;
    /// <summary>
    /// Holds PlayerProperties objects during play. Not saved.
    /// </summary>
    public static Dictionary<int, PlayerProperties> playersById 
        = new Dictionary<int, PlayerProperties>();

    public int playerID { get; private set; }
    /// <summary>
    /// Actions get added at turn start, and removed by AIUnitBehaviours (AI) or UnitBehaviour (human player).
    /// </summary>
    public int actions
    {
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].actions;
        set 
        {
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].actions = value;
            UIControlScript.Instance.ShowPlayerDetails();
        }
    }
    /// <summary>
    /// Don't delete things directly from here! Instead, use the method EndMission(aIMissionID).
    /// Keys are IDs, as usual.
    /// </summary>
    public Dictionary<int,AIMission> aIMissions 
    { 
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].aIMissions;
        set 
        {
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].aIMissions = value;
        }
    }
    public void EndMission(int aIMissionID)
    {
        aIMissions[aIMissionID].Dispose();
        aIMissions.Remove(aIMissionID);
    }
    public bool isEnvironment
    {
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isEnvironment;
        set { CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isEnvironment = value; }
    }
    public bool isHumanPlayer 
    { 
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isHumanPlayer; 
        set 
        { 
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isHumanPlayer = value;
            if (value) humanPlayerID = playerID;
        } 
    }
    /// <summary>
    /// Keys: locatableID; Values: aIMissionID. Auto-updates to cull dead locatableIDs;
    /// dead aIMissionIDs should be cleaned out by the AIMission disposer.
    /// </summary>
    public Dictionary<int, int?> objectMissionAssignment
    {
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].objectMissionAssignment;
        set
        {
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].objectMissionAssignment = value;
        }
    }
    private void UpdateObjectMissionAssignmentBcosOwnedObjectIdsChanged(
        object sender, NotifyCollectionChangedEventArgs e)
    {
        Debug.Log("Updating object mission assignment...");
        Dictionary<int, int?> updatedDict = new();
        foreach (int id in ownedObjectIds)
        {
            try { updatedDict[id] = objectMissionAssignment[id]; }
            catch { updatedDict[id] = null; }
        }
        objectMissionAssignment = updatedDict;
    }
    public ObservableCollection<int> ownedObjectIds 
    { 
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].ownedObjectIds; 
        set { CurrentGameState.Instance.gameStateData.playerDataDict[playerID].ownedObjectIds = value; }
    }
    public Color playerColor
    {
        get => new Color(
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[0],
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[1],
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[2],
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[3]);
        set
        {
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[0] = value.r;
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[1] = value.g;
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[2] = value.b;
            CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerColor[3] = value.a;
            foreach (int id in ownedObjectIds)
            {
                try
                {
                    LocatableObject.locatableObjectsById[id]
                        .GetComponent<UnitGraphicsController>().ApplySprite();
                }
                catch { }
            }
        }
    }
    public string playerName
    {
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerName;
        set { CurrentGameState.Instance.gameStateData.playerDataDict[playerID].playerName = value; }
    }

    public static int nextPlayerID; // nb: starts at 1
    public static List<Color> usedPlayerColors = new List<Color>();

    private void Awake()
    {
        playerID = nextPlayerID;
        // generate new PlayerData info if there isn't already some in the GameStateInfo
        if (!CurrentGameState.Instance.gameStateData.playerDataDict.ContainsKey(playerID))
        {
            CurrentGameState.Instance.gameStateData.playerDataDict.Add(playerID, new PlayerData());
            playerName = "Player " + playerID;
            playerColor = GeneratePlayerColor();

            foreach (int id in ownedObjectIds)
            {
                objectMissionAssignment[id] = null;
            }
        }
        nextPlayerID++;
        usedPlayerColors.Add(playerColor);
        // Debug.Log("Generated player color for Player " + playerID + ": " + playerColor.ToString());
        playersById.Add(playerID, this);
        ownedObjectIds.CollectionChanged += UpdateObjectMissionAssignmentBcosOwnedObjectIdsChanged;

        TurnManagerScript.Instance.OnStartTurn += RefreshPlayerActionsOnOwnStartTurn;
    }

    public void PreDestructionProtocols()
    {
        TurnManagerScript.Instance.OnStartTurn -= RefreshPlayerActionsOnOwnStartTurn;
        CurrentGameState.Instance.gameStateData.playerDataDict.Remove(playerID);
    }
    private static Color GeneratePlayerColor()
    {
        float r;
        float g;
        float b;

        int loopBreaker = 0;
        while (loopBreaker < 1000)
        {
            loopBreaker++;

            r = Random.value;
            g = Random.value;
            b = Random.value;

            // see if it's in the right range
            if (r + g + b < 1 || r + g + b > 2.5) continue;

            // see if it's distinct enough
            bool isDistinct = true;
            foreach (Color c in usedPlayerColors)
            {
                float distinctnessIndex = Mathf.Abs(r - c.r) + Mathf.Abs(g - c.g) + Mathf.Abs(b - c.b);
                if (distinctnessIndex < 0.5) isDistinct = false;
            }

            if (isDistinct) return new Color(r, g, b);
        }
        throw new System.Exception("Couldn't generate a color!");
    }
    private void RefreshPlayerActionsOnOwnStartTurn()
    {
        if (TurnManagerScript.Instance.CurrentPlayer.playerID == playerID)
            actions = 4;
    }
    public static void ResetPlayers()
    {
        nextPlayerID = 1;
        usedPlayerColors.Clear();
        playersById = new Dictionary<int, PlayerProperties> ();
        PlayerSetupScript.Instance.WipeAllPlayers();
    }
}
