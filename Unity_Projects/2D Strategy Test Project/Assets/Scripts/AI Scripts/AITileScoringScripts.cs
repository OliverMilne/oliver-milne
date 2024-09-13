using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AITileScoringScripts : MonoBehaviour
{
    public static bool HasVisibleEnemyUnit(int aiPlayerID, TileArrayEntry tae)
    {
        if (tae.visibilityByPlayerID[aiPlayerID] != TileVisibility.Visible) return false;

        List<LocatableObject> localUnits = tae.TileContents.Where(x => x.isUnit).ToList();
        localUnits = localUnits.Where(x => x.GetComponent<UnitInfo>().ownerID != aiPlayerID).ToList();
        if (localUnits.Count > 0 ) return true;

        return false;
    }
}
