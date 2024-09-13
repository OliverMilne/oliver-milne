using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class AIUnitGroupBehaviours : MonoBehaviour
{
    public static void AttackNearestEnemy(List<int> locatableIDs)
    {
        foreach (int objId in locatableIDs) AttackNearestEnemy(objId);
    }
    public static TileArrayEntry AttackNearestEnemy(int locatableID)
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
            UnitMovement.Instance.MoveUnitDefault(
                obj, destination, objUnitInfo.moveDistance.value);
            return destination;
        }
        else return obj.GetLocatableLocationTAE();
    }
    public static TileArrayEntry GroupRoveAndAttack(List<int> locatableIDs)
    {
        int selectIndex = Random.Range(0, locatableIDs.Count);
        int leaderID = locatableIDs[selectIndex];
        TileArrayEntry leaderDestination = MillAndAttack(leaderID);
        foreach (int objId in locatableIDs.Where(x => x != leaderID))
            MillNearAndAttack(objId, leaderDestination.TileLoc);
        return leaderDestination;
    }
    public static void MillAboutRandomly(List<int> locatableIDs)
    {
        foreach (int objId in locatableIDs) MillAboutRandomly(objId);
    }
    public static TileArrayEntry MillAboutRandomly(int locatableID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();

        // Pick a random tile in range
        List<TileArrayEntry> reachableTiles =
            UnitMovement.Instance.GetLocatable1TurnReachableTiles(
                obj, objUnitInfo.moveDistance.value, false);
        int selectIndex = Random.Range(0, reachableTiles.Count);
        TileArrayEntry destination = reachableTiles[selectIndex];

        // Move obj there
        UnitMovement.Instance.MoveUnitDefault(obj, destination, objUnitInfo.moveDistance.value);
        return destination;
    }
    public static void MillAndAttack(List<int> locatableIDs)
    {
        foreach (int objId in locatableIDs) MillAndAttack(objId);
    }
    public static TileArrayEntry MillAndAttack(int locatableID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();
        Debug.Log("Unit " + objUnitInfo.unitInfoID + " is milling and attacking");

        if (AIUnitStatus.HasVisibleEnemyInMovementRange(objUnitInfo))
            return AttackNearestEnemy(locatableID);
        else return MillAboutRandomly(locatableID);
    }
    public static TileArrayEntry MillNear(int locatableID, Vector3Int targetLoc)
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
        UnitMovement.Instance.MoveUnitDefault(obj, destination, objUnitInfo.moveDistance.value);
        return destination;
    }
    public static TileArrayEntry MillNearAndAttack(int locatableID, Vector3Int targetLoc)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[locatableID];
        if (!obj.isUnit) return obj.GetLocatableLocationTAE();
        UnitInfo objUnitInfo = obj.GetComponent<UnitInfo>();
        Debug.Log("Unit " + objUnitInfo.unitInfoID + " is hanging near " + targetLoc);

        if (AIUnitStatus.HasVisibleEnemyInMovementRange(objUnitInfo))
            return AttackNearestEnemy(locatableID);
        else return MillNear(locatableID, targetLoc);
    }
    public static TileArrayEntry MillNearAndAttack(int locatableID, int associateID)
    {
        LocatableObject obj = LocatableObject.locatableObjectsById[associateID];
        return MillNearAndAttack(locatableID, obj.GetLocatableLocationTAE().TileLoc);
    }
}
