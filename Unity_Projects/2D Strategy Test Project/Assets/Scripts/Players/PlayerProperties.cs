using System;
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
    public static int humanPlayerID
    {
        get
        { return CurrentGameState.Instance.gameStateData.miscIntsDataDict["PlayerProperties.humanPlayerID"]; }
        set
        {
            CurrentGameState.Instance.gameStateData.miscIntsDataDict["PlayerProperties.humanPlayerID"] 
                = value;
        }
    }
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
    private Dictionary<int, AIMission> _aIMissionsBehind = new();
    /// <summary>
    /// This isn't saved with the gamestate - it's for telling AIMissions whether to populate
    /// _aIMissionsBehind on get.
    /// </summary>
    private bool _aIMissionsBehindIsPopulated = false;
    /// <summary>
    /// Don't delete things directly from here! Instead, use the method AIMission.Dispose().
    /// Keys are IDs, as usual.
    /// </summary>
    public Dictionary<int,AIMission> AIMissions 
    {
        get 
        {
            if (!_aIMissionsBehindIsPopulated)
            {
                _aIMissionsBehindIsPopulated = true; // putting this before everything else to avoid loops
                foreach (KeyValuePair<int, AIMissionData> pair
                    in CurrentGameState.Instance.gameStateData.playerDataDict[playerID].aIMissionData)
                {
                    // create AIMissions of the appropriate types to match the data here
                    object[] args = { playerID, pair.Key };
                    Activator.CreateInstance(pair.Value.missionType, args);
                }
            }
            return _aIMissionsBehind;
        }
    }
    public bool isWildlife
    {
        get => CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isWildlife;
        set { CurrentGameState.Instance.gameStateData.playerDataDict[playerID].isWildlife = value; }
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
    /// dead aIMissionIDs are cleaned out by the AIMission disposer.
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

    private static int _nextPlayerIDBehind;
    public static int nextPlayerID
    {
        get
        {
            int returnValue = _nextPlayerIDBehind;
            _nextPlayerIDBehind
                = CurrentGameState.Instance.gameStateData.iDDispensers["PlayerProperties"].DispenseID();
            return returnValue;
        }
        set { _nextPlayerIDBehind = value; }
    }
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

            r = UnityEngine.Random.value;
            g = UnityEngine.Random.value;
            b = UnityEngine.Random.value;

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
        nextPlayerID = CurrentGameState.Instance.gameStateData.iDDispensers["PlayerProperties"].DispenseID();
        usedPlayerColors.Clear();
        playersById = new Dictionary<int, PlayerProperties> ();
        PlayerSetupScript.Instance.WipeAllPlayers();
    }
}
