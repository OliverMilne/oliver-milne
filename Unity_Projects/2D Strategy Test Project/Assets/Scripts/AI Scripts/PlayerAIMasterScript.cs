using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAIMasterScript : MonoBehaviour
{
    private static PlayerAIMasterScript instance;
    public static PlayerAIMasterScript Instance { get { return instance; } }

    private bool _isInitialised = false;

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }
    /// <summary>
    /// This doesn't do anything right now, but it might later, so I've not deleted it
    /// </summary>
    public void PlayerAIMasterScript_Initialise()
    {
        if (!_isInitialised)
        {
            _isInitialised = true;
        }
    }

    private void PlayerAIDoStuff(PlayerProperties playerProperties)
    {
        // Plan: explore as a group until an enemy is sighted, then beeline to them and attack
        if (playerProperties.ownedObjectIds.Count > 0)
        {
            // this is to be replaced
            AIUnitGroupBehaviours.GroupMillAndAttack(playerProperties.ownedObjectIds);
        }
    }
    public void PlayerTurnAiCaller()
    {
        if (TurnManagerScript.Instance.CurrentPlayer.isHumanPlayer) return;

        // add code to do stuff here
        PlayerAIDoStuff(TurnManagerScript.Instance.CurrentPlayer);

        Debug.Log(TurnManagerScript.Instance.CurrentPlayer.playerName + " took their turn!");
        TurnManagerScript.Instance.EndTurn();
    }
}
