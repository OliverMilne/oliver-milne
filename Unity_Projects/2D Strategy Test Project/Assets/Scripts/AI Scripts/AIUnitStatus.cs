using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUnitStatus : MonoBehaviour
{
    public static bool HasVisibleEnemyInMovementRange(UnitInfo unitInfo)
    {
        List<TileArrayEntry> reachableTiles =
                UnitMovement.Instance.GetLocation1TurnReachableTiles(
                    unitInfo.GetComponent<LocatableObject>(), 
                    unitInfo.moveDistance.value);

        // Find visible enemies within range
        foreach (TileArrayEntry tae in reachableTiles)
            if (AITileScoringScripts.HasVisibleEnemyUnit(unitInfo.ownerID, tae))
                return true;
        return false;
    }
    public static bool HasVisibleEnemyInMovementRange(
        UnitInfo unitInfo, out List<TileArrayEntry> locations)
    {
        List<TileArrayEntry> reachableTiles =
                UnitMovement.Instance.GetLocation1TurnReachableTiles(
                    unitInfo.GetComponent<LocatableObject>(),
                    unitInfo.moveDistance.value);
        List<TileArrayEntry> enemyOccupiedTiles = new List<TileArrayEntry>();

        // Find visible enemies within range
        foreach (TileArrayEntry tae in reachableTiles)
        {
            if (AITileScoringScripts.HasVisibleEnemyUnit(unitInfo.ownerID, tae))
                enemyOccupiedTiles.Add(tae);
        }
        locations = enemyOccupiedTiles;
        if (locations.Count == 0) return false;
        return true;
    }
}
