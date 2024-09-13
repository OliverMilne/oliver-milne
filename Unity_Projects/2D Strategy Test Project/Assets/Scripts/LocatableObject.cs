using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocatableObject : MonoBehaviour
{
    public static int nextLocatableID;
    public static Dictionary<int, LocatableObject> locatableObjectsById = 
        new Dictionary<int, LocatableObject>(); // make this immutable down the line?

    public int locatableID { get; private set; }

    // stuff drawn from LocatableData
    public int assignedTAEID 
    {
        get => CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].assignedTAEID;
        set
        {
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].assignedTAEID = value;
        }
    }
    public bool isUnit
    {
        get => CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isUnit;
        set
        {
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isUnit = value;
            if (value) unitInfo = GetComponent<UnitInfo>();
        }
    }
    public bool isScenery
    {
        get => CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isScenery;
        set
        {
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isScenery = value;
        }
    }
    public bool isSelectable
    {
        get => CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isSelectable;
        set
        {
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].isSelectable = value;
        }
    }
    private UnitInfo _unitInfoBehind;
    public UnitInfo unitInfo 
    { 
        get => _unitInfoBehind; 
        set 
        { 
            _unitInfoBehind = value;
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID].unitInfoId =
                value.unitInfoID;
        } 
    }

    private void Awake()
    {
        // assign locatableID
        locatableID = nextLocatableID;
        nextLocatableID++;
        if (!CurrentGameState.Instance.gameStateInfo.locatablesInfoDict.ContainsKey(locatableID))
        {
            CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableID] = new LocatableData();
        }

        locatableObjectsById.Add(locatableID, this);
    }
    public void DebugMoveToTile(TileArrayEntry entry)
    {
        entry.AssignTileContents(this);
        entry.InstantMoveTransformToTile(transform);
        OverlayGraphicsScript.Instance.DrawSelectionGraphics(SelectorScript.Instance.selectedObject);
        if (this.isUnit) entry.SetVisibleUnit(this.GetComponent<UnitInfo>().unitInfoID);
    }
    public void DebugLocatableInfo()
    {
        string infoString = "locatableID: " + locatableID + "; ";
        if(isUnit) infoString = infoString + unitInfo.UnitInfoString();
        Debug.Log(infoString);
    }
    public TileArrayEntry GetLocatableLocationTAE()
    {
        return TileFinders.Instance.GetTileArrayEntryByID(assignedTAEID);
    }
    /// <summary>
    /// Call this before destroying the GameObject!!! It calls all the other PreDestructionProtocols methods
    /// for that object's various scripts.
    /// </summary>
    public void PreDestructionProtocols()
    {
        // deselect it
        if (TryGetComponent<SelectableObject>(out SelectableObject s)) s.PreDestructionProtocols();

        // clear it out of its location
        try
        {
            GetLocatableLocationTAE().tileContentsIds =
                GetLocatableLocationTAE().tileContentsIds.Where(x => x != locatableID).ToList();
            // Debug.Log(this + " was destroyed at " + GetLocatableLocationTAE().tileLoc + "!");
        }
        catch { Debug.Log("No assignedTileArrayEntry found for " + this); }

        // remove it from player ownership dictionaries
        WipeThisFromPlayerOwnershipDicts();

        if (isUnit)
        {
            GetComponent<UnitBehaviour>().PreDestructionProtocols();
            GetComponent<UnitInfo>().PreDestructionProtocols();
        }
        if (isScenery) GetComponent<SceneryInfo>().PreDestructionProtocols();

        // remove it from locatableObjectsById
        locatableObjectsById.Remove(locatableID);

        // delete its LocatableData
        CurrentGameState.Instance.gameStateInfo.locatablesInfoDict.Remove(locatableID);
    }
    public static void WipeAllLocatableObjectsAndReset()
    {
        List<LocatableObject> allLocatables = locatableObjectsById.Values.ToList();
        foreach (var entry in allLocatables) 
        { 
            entry.PreDestructionProtocols();
            Destroy(entry.gameObject);
        }
        locatableObjectsById = new Dictionary<int, LocatableObject>();
        nextLocatableID = 0;
    }
    private void WipeThisFromPlayerOwnershipDicts()
    {
        foreach (PlayerProperties player in PlayerSetupScript.Instance.playerList)
            player.ownedObjectIds = player.ownedObjectIds.Where(x => x != locatableID).ToList();
    }
}
