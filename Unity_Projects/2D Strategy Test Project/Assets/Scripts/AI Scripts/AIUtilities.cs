using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIUtilities
{
    public static bool AreEnemiesVisible(int playerID)
    {
        bool enemiesVisible = false;
        foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
            if (AITileScoringScripts.HasVisibleEnemyUnit(playerID, tae))
                enemiesVisible = true;
        return enemiesVisible;
    }
}
