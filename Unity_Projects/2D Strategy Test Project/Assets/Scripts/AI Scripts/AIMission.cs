using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is the base AI mission class. AI missions hold information about what the AI is
/// currently up to, act on that information by calling appropriate AIUnitGpBehavPickers,
/// and provide reports on their status.
/// </summary>
public abstract class AIMission : IDisposable
{
    public int aIMissionID { get; }
    protected bool _disposed = false;
    public int movesTakenThisTurn = 0;

    // Needs to be able to hold info about its mission, assign appropriate behaviours each turn,
    // and, when it's completed, let the strategic AI know and delete itself
    /// <summary>
    /// This produces a list on demand but anything you write to it will be lost - instead,
    /// change the PlayerProperties' objectMissionAssignment dictionary.
    /// </summary>
    public List<int> AssignedUnitIDs 
    {
        get
        {
            return PlayerProperties.playersById[playerID].objectMissionAssignment.Keys.Where(
                x => PlayerProperties.playersById[playerID].objectMissionAssignment[x] == aIMissionID)
                .ToList();
        }
    }
    public int playerID;

    public AIMission(int playerID)
    {
        aIMissionID = CurrentGameState.Instance.gameStateData.iDDispensers["AIMission"].DispenseID();
        this.playerID = playerID;
        PlayerProperties.playersById[playerID].aIMissions.Add(aIMissionID, this);
    }
    ~AIMission()
    {
        if (!_disposed) Dispose();
    }

    /// <summary>
    /// This needs reworking once unitInfoIDs are retired
    /// </summary>
    /// <param name="unitInfo"></param>
    /// <exception cref="System.Exception"></exception>
    public void AssignUnit(UnitInfo unitInfo)
    {
        PlayerProperties.playersById[playerID].objectMissionAssignment[unitInfo.unitInfoID] = aIMissionID;
    }
    public void AssignUnit(int unitInfoID)
    {
        PlayerProperties.playersById[playerID].objectMissionAssignment[unitInfoID] = aIMissionID;
    }
    /// <summary>
    /// This removes the mission from all associated PlayerProperties dictionaries,
    /// so it should be used to end missions.
    /// </summary>
    public void Dispose()
    {
        foreach (var key in PlayerProperties.playersById[playerID].objectMissionAssignment.Keys)
            if (PlayerProperties.playersById[playerID].objectMissionAssignment[key] == aIMissionID)
                PlayerProperties.playersById[playerID].objectMissionAssignment[key] = null;
        PlayerProperties.playersById[playerID].aIMissions.Remove(aIMissionID);
        _disposed = true;
    }
    public virtual int MakeActionRequest()
    {
        return AssignedUnitIDs.Count - movesTakenThisTurn;
    }
    /// <summary>
    /// Should assign appropriate behaviours to its associated units.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public void PerformNextAction()
    {
        movesTakenThisTurn++;
        PerformNextActionMissionSpecific();
    }
    protected abstract void PerformNextActionMissionSpecific();
    public abstract MissionStatusReport ReportStatus();
}
/// <summary>
/// Remember to dispose AIMissions!
/// </summary>
public class AIMission_SearchAndDestroy : AIMission
{
    public AIMission_SearchAndDestroy(int playerID)
        : base(playerID) { }

    public override int MakeActionRequest()
    {
        if (AIUtilities.AreEnemiesVisible(playerID))
            return AssignedUnitIDs.Count * 2;
        else return base.MakeActionRequest();
    }
    protected override void PerformNextActionMissionSpecific()
    {
        // if no enemies visible, explore as a group. else, attack
        if (AIUtilities.AreEnemiesVisible(playerID))
            AIUnitBehaviours1By1.GroupAttackNearestVisibleEnemy(AssignedUnitIDs, AssignedUnitIDs[0],playerID);
        else AIUnitBehaviours1By1.GroupExploreUndirected(AssignedUnitIDs, AssignedUnitIDs[0], playerID); 
    }
    public override MissionStatusReport ReportStatus()
    {
        throw new NotImplementedException();
    }
}