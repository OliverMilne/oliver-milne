using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSetupScript : MonoBehaviour
{
    private static PlayerSetupScript instance;
    public static PlayerSetupScript Instance { get => instance; }

    public Transform playerPrefab;
    public List<PlayerProperties> playerList;

    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }
    public void PlayerSetupScript_Initialise()
    {
        PlayerProperties.ResetPlayers();
        var player1 = Instantiate(playerPrefab);
        PlayerProperties player1Properties = player1.GetComponent<PlayerProperties>();
        player1Properties.isHumanPlayer = true;

        var player2 = Instantiate(playerPrefab);
        PlayerProperties player2Properties = player2.GetComponent<PlayerProperties>();

        playerList = new List<PlayerProperties> { player1Properties, player2Properties };

        // Debug functionality
        string playerString = "Players: ";
        foreach (PlayerProperties player in playerList) 
        { 
            playerString = playerString + player.playerName + ". "; 
        }
        Debug.Log(playerString);
    }
    public void PlayerSetupScript_InitialiseFromGameStateInfo()
    {
        playerList = new List<PlayerProperties>();
        PlayerProperties.ResetPlayers();
        foreach(var playerDataEntry in CurrentGameState.Instance.gameStateInfo.playerDataDict)
        {
            PlayerProperties.nextPlayerID = playerDataEntry.Key;
            var player = Instantiate(playerPrefab);
            playerList.Add(player.GetComponent<PlayerProperties>());
        }

        // Debug functionality
        string playerString = "Players: ";
        foreach (PlayerProperties player in playerList)
        {
            playerString = playerString + player.playerName + ". ";
        }
        Debug.Log(playerString);
    }
    public void WipeAllPlayers()
    {
        foreach (PlayerProperties p in playerList) 
        {
            p.PreDestructionProtocols();
            Destroy(p.gameObject); 
        }
        playerList = new List<PlayerProperties>();
    }
}
