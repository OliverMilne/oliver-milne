using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUnitBehaviours1By1 : MonoBehaviour
{
    /// <summary>
    /// The group attacks the enemy nearest the leader.
    /// </summary>
    /// <param name="locatableIDs"></param>
    /// <param name="orderingPlayerID"></param>
    /// <returns></returns>
    public static TileArrayEntry GroupAttackNearestVisibleEnemy(List<int> locatableIDs, int leaderLocID,
        int orderingPlayerID)
    {
        TileArrayEntry attackTarget = UnitMovement.Instance.GetQuickestReachableTileWithCondition(
            LocatableObject.locatableObjectsById[leaderLocID],
            x => AITileScoringScripts.HasVisibleEnemyUnit(orderingPlayerID, x));

        // select highest-Readiness member of the group
        int? unitIdToMove = null;
        float topReadiness = 0;
        foreach (int id in locatableIDs)
        {
            if (unitIdToMove == null) unitIdToMove = id;
            if (LocatableObject.locatableObjectsById[id].unitInfo.currentReadiness > topReadiness)
            {
                unitIdToMove = id;
                topReadiness = LocatableObject.locatableObjectsById[id].unitInfo.currentReadiness;
            }
        }
        if (unitIdToMove != null)
        {
            return AIUnitBehaviours.MoveToPoint((int)unitIdToMove, attackTarget, orderingPlayerID);
        }
        return null;
    }
    /// <summary>
    /// If group is in cohesion, send leader to explore, else bring followers to leader
    /// </summary>
    /// <param name="locatableIDs"></param>
    /// <param name="leaderLocID"></param>
    /// <param name="orderingPlayerID"></param>
    /// <returns></returns>
    public static TileArrayEntry GroupExploreUndirected(List<int> locatableIDs, int leaderLocID,
        int orderingPlayerID)
    {
        bool inCohesion = true;
        List<int> outCohesionUnitIds = new List<int>();
        TileArrayEntry currentLeaderTAE 
            = LocatableObject.locatableObjectsById[leaderLocID].GetLocatableLocationTAE();

        // find out whether & which units are out of cohesion
        foreach (int id in locatableIDs)
        {
            if (leaderLocID == id) continue;
            LocatableObject follower = LocatableObject.locatableObjectsById[id];
            List<TileArrayEntry> twoTurnTiles
                = UnitMovement.Instance.GetLocation1TurnReachableTiles(follower, 2);
            List<int> tTTIDs = new();
            foreach (TileArrayEntry tTT in twoTurnTiles) tTTIDs.Add(tTT.taeID);
            if (!tTTIDs.Contains(currentLeaderTAE.taeID)) 
            { 
                inCohesion = false; 
                outCohesionUnitIds.Add(id);
            }
        }

        if (inCohesion) 
        {
            Debug.Log($"Unit {leaderLocID} is exploring!");
            return AIUnitBehaviours.ExploreUndirected(leaderLocID, orderingPlayerID);
        }
        else
        {
            // pick a unit out of cohesion with the highest Readiness
            int? unitIdToMove = null;
            float topReadiness = 0;
            foreach(int id in outCohesionUnitIds)
            {
                if (unitIdToMove == null) unitIdToMove = id;
                if (LocatableObject.locatableObjectsById[id].unitInfo.currentReadiness > topReadiness)
                {
                    unitIdToMove = id;
                    topReadiness = LocatableObject.locatableObjectsById[id].unitInfo.currentReadiness;
                }
            }
            Debug.Log($"Unit {unitIdToMove} is catching up with unit {leaderLocID}!");
            return AIUnitBehaviours.MillNear((int)unitIdToMove, currentLeaderTAE.TileLoc, orderingPlayerID);
        }
    }
}
