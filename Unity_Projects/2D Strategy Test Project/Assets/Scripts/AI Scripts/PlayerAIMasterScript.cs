using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// There is only one of this, and it attaches to ScriptBucket.
/// </summary>
public class PlayerAIMasterScript : MonoBehaviour
{
    private static PlayerAIMasterScript instance;
    public static PlayerAIMasterScript Instance { get { return instance; } }

    private bool _isInitialised = false;
    /// <summary>
    /// This should be incremented and decremented in the coroutines, i.e. at the lowest level.
    /// </summary>
    public int ActionsAIWaitingToFinish = 0;
    private bool PlayerAIIsDone = false;

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

    /// <summary>
    /// Gets the specified player AI to do their during-turn stuff.
    /// </summary>
    /// <param name="playerProperties"></param>
    private IEnumerator PlayerAIDoStuff(PlayerProperties playerProperties)
    {
        // reset per-turn mission data
        foreach (var a in playerProperties.aIMissions) a.Value.movesTakenThisTurn = 0;

        int loopBreaker = 0;
        while(playerProperties.actions > 0)
        {
            loopBreaker++;
            if (loopBreaker == 2000) throw new System.Exception("Player action loop hit limit!");

            // assess situation and modify missions as appropriate
            if (AssessSituationAndModifyMissions(playerProperties)) 
                { Debug.Log($"Player {playerProperties.playerID} owns no objects!"); break; }

            // assign remaining actions to missions according to mission priority & action requests
            AIMission nextMission = PickNextMissionToAct(playerProperties);
            if (nextMission == null) break;

            // perform 1 action and wait for it to play out
            nextMission.PerformNextAction();
            // Debug.Log($"playerProperties.ownedObjectIds[0]: {playerProperties.ownedObjectIds[0]}");
            while (ActionsAIWaitingToFinish > 0) yield return null;
        }
        PlayerAIIsDone = true;
        yield break;
    }
    /// <summary>
    /// Checks if the player is human; if not, calls PlayerAIDoStuff and then ends the turn.
    /// </summary>
    public IEnumerator PlayerTurnAiCaller()
    {
        if (TurnManagerScript.Instance.CurrentPlayer.isHumanPlayer) yield break;

        while (ActionsAIWaitingToFinish > 0) yield return null;
        Debug.Log("PlayerTurnAICaller ready to call PlayerAIDoStuff");

        PlayerAIIsDone = false;
        StartCoroutine(PlayerAIDoStuff(TurnManagerScript.Instance.CurrentPlayer));
        while (!PlayerAIIsDone) yield return null;

        Debug.Log(TurnManagerScript.Instance.CurrentPlayer.playerName + " took their turn!");
        TurnManagerScript.Instance.EndTurnOnUpdate = true;
        yield break;
    }
    #region PlayerAIDoStuff steps
    /// <summary>
    /// Should return 'true' if it indicates the PlayerAIDoStuff 'while' loop should be broken.
    /// </summary>
    /// <param name="playerProperties"></param>
    /// <returns></returns>
    private bool AssessSituationAndModifyMissions(PlayerProperties playerProperties)
    {
        if (playerProperties.ownedObjectIds.Count == 0) return true;

        // for now, we just create a Search and Destroy mission if there are unassigned units
        List<int> unassignedUnitIDs = playerProperties.objectMissionAssignment.Keys.
            Where(x => playerProperties.objectMissionAssignment[x] == null).ToList();
        if (unassignedUnitIDs.Count > 0)
        {
            AIMission newMission = new AIMission_SearchAndDestroy(playerProperties.playerID);
            foreach (var x in unassignedUnitIDs) 
                playerProperties.aIMissions[newMission.aIMissionID].AssignUnit(x);
        }

        return false;
    }
    private AIMission PickNextMissionToAct(PlayerProperties playerProperties)
    {
        Dictionary<int, int> missionActionRequests = new Dictionary<int, int>();
        foreach (var pair in playerProperties.aIMissions)
            missionActionRequests[pair.Key] = pair.Value.MakeActionRequest();
        AIMission nextMission = null;
        foreach (var missionID in missionActionRequests.Keys)
            if (missionActionRequests[missionID] > 0)
            {
                nextMission = playerProperties.aIMissions[missionID];
                break;
            }
        return nextMission;
    }
    #endregion
}
