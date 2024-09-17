using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AITileScoringScripts : MonoBehaviour
{
    public static float ExplorationPotential(int aiPlayerID, TileArrayEntry tae, int visionRange,
        out bool foundUnexploredTiles)
    {
        List<TileArrayEntry>[] tilesVisibleFromTile 
            = UnitVisionScript.Instance.getVisibleTiles(tae, visionRange, aiPlayerID);
        List<TileArrayEntry> newlyVisibleTiles 
            = tilesVisibleFromTile[0].Where(
                x => x.GetVisibilityByPlayerID(aiPlayerID) == TileVisibility.Hidden).ToList();
        if (newlyVisibleTiles.Count > 0) foundUnexploredTiles = true;
        else foundUnexploredTiles = false;
        List<TileArrayEntry> exploredTilesOverlooked
            = tilesVisibleFromTile[0].Where(
                x => x.GetVisibilityByPlayerID(aiPlayerID) == TileVisibility.Explored).ToList();
        return (float)newlyVisibleTiles.Count + (0.001f*exploredTilesOverlooked.Count);
    }
    public static bool HasVisibleEnemyUnit(int aiPlayerID, TileArrayEntry tae)
    {
        if (tae.GetVisibilityByPlayerID(aiPlayerID) != TileVisibility.Visible) return false;

        List<LocatableObject> localUnits = tae.TileContents.Where(x => x.isUnit).ToList();
        localUnits = localUnits.Where(x => x.GetComponent<UnitInfo>().ownerID != aiPlayerID).ToList();
        if (localUnits.Count > 0 ) return true;

        return false;
    }
}
