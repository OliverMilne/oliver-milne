using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Also contains unit group behaviours; unit group behaviours don't fit the AI system, and
/// so should only be used as group commands for player units.
/// </summary>
public class AIUnitBehaviours : MonoBehaviour
{
    public static void AttackNearestEnemyToHand(List<int> locatableIDs, int orderingPlayerID)
    {
        foreach (int objId in locatableIDs) AttackNearestEnemyToHand(objId, orderingPlayerID);
    }
    public static TileArrayEntry AttackNearestEnemyToHand(int locatableID, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        // Get tiles with enemies
        AIUnitStatus.HasVisibleEnemyInMovementRange(objUnitInfo,
            out List<TileArrayEntry> enemyOccupiedTiles);

        TileArrayEntry destination = TileFinders.Instance.GetClosestTileFromList(
            obj.GetLocatableLocationTAE(), enemyOccupiedTiles, objUnitInfo.ownerID, true);

        if (destination != null)
        // Move obj there
        {
            PlayerProperties.playersById[(int)orderingPlayerID].actions--;
            UnitMovement.Instance.MoveUnitDefault(
                obj, destination, objUnitInfo.moveDistance.value);
            return destination;
        }
        else return obj.GetLocatableLocationTAE();
    }
    public static TileArrayEntry AttackNearestVisibleEnemy(int locatableID, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        TileArrayEntry destination = UnitMovement.Instance.GetQuickestReachableTileWithCondition(
            obj, x => AITileScoringScripts.HasVisibleEnemyUnit(orderingPlayerID, x));

        if (destination != null)
        // Move obj there
        {
            PlayerProperties.playersById[(int)orderingPlayerID].actions--;
            UnitMovement.Instance.MoveUnitDefault(
                obj, destination, objUnitInfo.moveDistance.value);
            return destination;
        }
        else return obj.GetLocatableLocationTAE();
    }
    public static TileArrayEntry ExploreUndirected(int locatableID, int? orderingPlayerID)
    {
        // if there's nowhere new to see, look further afield
        bool nothingNewToSee = true;

        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        // Rank visible tiles in range by ExplorationPotential, pick one that has the highest
        List<TileArrayEntry> reachableTiles =
            UnitMovement.Instance.GetLocation1TurnReachableTiles(
                obj, objUnitInfo.moveDistance.value);
        TileArrayEntry highestScoringTile = reachableTiles.First();
        float maxExplorationValue = AITileScoringScripts.ExplorationPotential(
            objUnitInfo.ownerID, highestScoringTile, 5, out bool foundUnexploredTiles);
        if (foundUnexploredTiles) nothingNewToSee = false;
        foreach (TileArrayEntry tae in reachableTiles)
        {
            if (AITileScoringScripts.ExplorationPotential(objUnitInfo.ownerID, tae, 5,
                out foundUnexploredTiles) > maxExplorationValue) 
            { 
                highestScoringTile = tae;
                maxExplorationValue = AITileScoringScripts.ExplorationPotential(
                    objUnitInfo.ownerID, tae, 5, out bool _);
            }
            if (foundUnexploredTiles) nothingNewToSee = false;
        }

        // if there's nothing new to see, look further afield
        if (nothingNewToSee)
        {
            // find all reachable tiles
            reachableTiles = UnitMovement.Instance.GetLocation1TurnReachableTiles(
                obj, 1000);

            // see which of those does better
            highestScoringTile = reachableTiles.First();
            maxExplorationValue = AITileScoringScripts.ExplorationPotential(
            objUnitInfo.ownerID, highestScoringTile, 5, out foundUnexploredTiles);
            if (foundUnexploredTiles) nothingNewToSee = false;
            foreach (TileArrayEntry tae in reachableTiles)
            {
                if (AITileScoringScripts.ExplorationPotential(objUnitInfo.ownerID, tae, 5,
                    out foundUnexploredTiles) > maxExplorationValue)
                {
                    highestScoringTile = tae;
                    maxExplorationValue = AITileScoringScripts.ExplorationPotential(
                        objUnitInfo.ownerID, tae, 5, out bool _);
                }
                if (foundUnexploredTiles) nothingNewToSee = false;
            }
        }

        if (nothingNewToSee) 
        {
            Debug.Log("No unexplored tiles accessible!");
        }
        // Move obj there
        if (highestScoringTile != null)
        {
            if (orderingPlayerID != null) PlayerProperties.playersById[(int)orderingPlayerID].actions--;
            UnitMovement.Instance.MoveUnitDefault(
                obj, highestScoringTile, objUnitInfo.moveDistance.value);
            return highestScoringTile;
        }
        else throw new System.Exception("ExploreUndirected target selection error!");
    }
    public static TileArrayEntry GroupExploreUndirected(List<int> locatableIDs, int orderingPlayerID)
    {
        int selectIndex = Random.Range(0, locatableIDs.Count);
        int leaderID = locatableIDs[selectIndex];
        TileArrayEntry leaderDestination = ExploreUndirected(leaderID, orderingPlayerID);
        foreach (int objId in locatableIDs.Where(x => x != leaderID))
            MillNear(objId, leaderDestination.TileLoc, orderingPlayerID);
        return leaderDestination;
    }
    public static TileArrayEntry GroupMillAndAttack(List<int> locatableIDs, int orderingPlayerID)
    {
        int selectIndex = Random.Range(0, locatableIDs.Count);
        int leaderID = locatableIDs[selectIndex];
        TileArrayEntry leaderDestination = MillAndAttack(leaderID, orderingPlayerID);
        foreach (int objId in locatableIDs.Where(x => x != leaderID))
            MillNearAndAttack(objId, leaderDestination.TileLoc, orderingPlayerID);
        return leaderDestination;
    }
    public static void MillAboutRandomly(List<int> locatableIDs, int orderingPlayerID)
    {
        foreach (int objId in locatableIDs) MillAboutRandomly(objId, orderingPlayerID);
    }
    public static TileArrayEntry MillAboutRandomly(int locatableID, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        // Pick a random tile in range
        List<TileArrayEntry> reachableTiles =
            UnitMovement.Instance.GetLocation1TurnReachableTiles(
                obj, objUnitInfo.moveDistance.value, false);
        int selectIndex = Random.Range(0, reachableTiles.Count);
        TileArrayEntry destination = reachableTiles[selectIndex];

        // Move obj there
        PlayerProperties.playersById[(int)orderingPlayerID].actions--;
        UnitMovement.Instance.MoveUnitDefault(obj, destination, objUnitInfo.moveDistance.value);
        return destination;
    }
    public static void MillAndAttack(List<int> locatableIDs, int orderingPlayerID)
    {
        foreach (int objId in locatableIDs) MillAndAttack(objId, orderingPlayerID);
    }
    public static TileArrayEntry MillAndAttack(int locatableID, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();
        Debug.Log("Unit " + objUnitInfo.unitInfoID + " is milling and attacking");

        if (AIUnitStatus.HasVisibleEnemyInMovementRange(objUnitInfo))
            return AttackNearestEnemyToHand(locatableID, orderingPlayerID);
        else return MillAboutRandomly(locatableID, orderingPlayerID);
    }
    public static TileArrayEntry MillNear(int locatableID, Vector3Int targetLoc, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        // Get the nearest tile within 2 moves of the target, but not the target itself
        TileArrayEntry targetTAE 
            = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(targetLoc);
        List<TileArrayEntry> adjacentTAEs 
            = UnitMovement.Instance.GetLocation1TurnReachableTiles(
                targetLoc, 2, objUnitInfo.ownerID);
        int selectIndex = Random.Range(0, adjacentTAEs.Count);
        TileArrayEntry destination = adjacentTAEs[selectIndex];

        // Go there
        PlayerProperties.playersById[(int)orderingPlayerID].actions--;
        UnitMovement.Instance.MoveUnitDefault(obj, destination, objUnitInfo.moveDistance.value);
        return destination;
    }
    public static TileArrayEntry MillNearAndAttack(int locatableID, Vector3Int targetLoc, 
        int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();
        Debug.Log("Unit " + objUnitInfo.unitInfoID + " is hanging near " + targetLoc);

        if (AIUnitStatus.HasVisibleEnemyInMovementRange(objUnitInfo))
            return AttackNearestEnemyToHand(locatableID, orderingPlayerID);
        else return MillNear(locatableID, targetLoc, orderingPlayerID);
    }
    public static TileArrayEntry MillNearAndAttack(int locatableID, int associateID, int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[associateID];
        return MillNearAndAttack(locatableID, obj.GetLocatableLocationTAE().TileLoc, orderingPlayerID);
    }
    public static TileArrayEntry MoveToPoint(int locatableID, TileArrayEntry destination,
        int orderingPlayerID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        {
            PlayerProperties.playersById[(int)orderingPlayerID].actions--;
            UnitMovement.Instance.MoveUnitDefault(
                obj, destination, objUnitInfo.moveDistance.value);
            return destination;
        }
    }
}
