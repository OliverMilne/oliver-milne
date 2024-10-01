using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is the base AI mission class
/// </summary>
public abstract class AIMission : MonoBehaviour
{
    // Needs to be able to hold info about its mission, assign appropriate behaviours each turn,
    // and, when it's completed, let the strategic AI know and delete itself
    public List<UnitInfo> assignedUnits = new List<UnitInfo>();
    public List<int> assignedUnitIDs 
    { 
        get 
        { 
            List<int> idsList = new List<int>();
            foreach (UnitInfo uinf in assignedUnits)
                idsList.Add(uinf.unitInfoID);
            return idsList;
        } 
    }
    /// <summary>
    /// Changing this number doesn't change the AI player's actual supply of APs. This is just a count
    /// of how many have been assigned to the mission this turn.
    /// </summary>
    public int assignedActionPoints = 0;
    public int playerID;

    public AIMission(List<UnitInfo> assignedUnits, int assignedActionPoints, int playerID)
    {
        this.assignedUnits = assignedUnits;
        this.assignedActionPoints = assignedActionPoints;
    }

    /// <summary>
    /// Should assign appropriate behaviours to its associated units.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public abstract void PerformTurnActivities();
}

public class AIMission_SearchAndDestroy : AIMission
{
    public AIMission_SearchAndDestroy(List<UnitInfo> assignedUnits, int assignedActionPoints, int playerID)
        : base(assignedUnits, assignedActionPoints, playerID) 
    {
    }

    public override void PerformTurnActivities()
    {
        // if no enemies visible, explore as a group. else, attack
        bool enemiesVisible = false;
        foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
            if (AITileScoringScripts.HasVisibleEnemyUnit(playerID, tae))
                enemiesVisible = true;
        if (enemiesVisible)
        {
            AIUnitGroupBehaviours.GroupMillAndAttack(assignedUnitIDs);
        }
        else AIUnitGroupBehaviours.GroupExploreUndirected(assignedUnitIDs);
    }
}