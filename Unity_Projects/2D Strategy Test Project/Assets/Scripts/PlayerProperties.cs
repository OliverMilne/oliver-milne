using System.Collections;
using System.Collections.Generic;
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
    public int actions
    {
        get => CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].actions;
        set 
        {
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].actions = value;
            if (isHumanPlayer) UIControlScript.Instance.ShowPlayerDetails();
        }
    }
    public bool isHumanPlayer 
    { 
        get => CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].isHumanPlayer; 
        set 
        { 
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].isHumanPlayer = value;
            if (value) humanPlayerID = playerID;
        } 
    }
    public bool isEnvironment 
    { 
        get => CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].isEnvironment; 
        set { CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].isEnvironment = value; } 
    }
    public string playerName 
    { 
        get => CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerName; 
        set { CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerName = value; } 
    }
    public Color playerColor 
    { 
        get => new Color (
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[0], 
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[1], 
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[2], 
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[3]); 
        set 
        {
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[0] = value.r;
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[1] = value.g;
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[2] = value.b; 
            CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].playerColor[3] = value.a;
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
    public List<int> ownedObjectIds 
    { 
        get => CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].ownedObjectIds; 
        set { CurrentGameState.Instance.gameStateInfo.playerDataDict[playerID].ownedObjectIds = value; } 
    }

    public static int nextPlayerID; // nb: starts at 1
    public static List<Color> usedPlayerColors = new List<Color>();

    private void Awake()
    {
        playerID = nextPlayerID;
        // generate new PlayerData info if there isn't already some in the GameStateInfo
        if (!CurrentGameState.Instance.gameStateInfo.playerDataDict.ContainsKey(playerID))
        {
            CurrentGameState.Instance.gameStateInfo.playerDataDict.Add(playerID, new PlayerData());
            playerName = "Player " + playerID;
            playerColor = GeneratePlayerColor();
        }
        nextPlayerID++;
        usedPlayerColors.Add(playerColor);
        // Debug.Log("Generated player color for Player " + playerID + ": " + playerColor.ToString());
        playersById.Add(playerID, this);

        TurnManagerScript.Instance.OnStartTurn += RefreshPlayerActionsOnOwnStartTurn;
    }

    public void PreDestructionProtocols()
    {
        TurnManagerScript.Instance.OnStartTurn -= RefreshPlayerActionsOnOwnStartTurn;
        CurrentGameState.Instance.gameStateInfo.playerDataDict.Remove(playerID);
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
