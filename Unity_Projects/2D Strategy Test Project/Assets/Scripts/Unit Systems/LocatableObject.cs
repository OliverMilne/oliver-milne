using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class LocatableObject : MonoBehaviour
{
    private static int _nextLocatableIDBehind;
    public static int nextLocatableID 
    { 
        get 
        {
            int returnValue = _nextLocatableIDBehind;
            _nextLocatableIDBehind
                = CurrentGameState.Instance.gameStateData.iDDispensers["LocatableObject"].DispenseID();
            return returnValue;
        } 
        set { _nextLocatableIDBehind = value; }
    }
    public static Dictionary<int, LocatableObject> locatableObjectsById = 
        new Dictionary<int, LocatableObject>(); // make this immutable down the line?
    /// <summary>
    /// Fires whenever a locatable object is destroyed, for victory condition checks
    /// </summary>
    public static event Action LocatableObjectDestroyed;

    public int locatableID { get; private set; }
    /// <summary>
    /// Shows whether PreDestructionProtocols has been called
    /// </summary>
    private bool _preparedForDeath = false; 

    // stuff drawn from LocatableData
    public int assignedTAEID 
    {
        get => CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].assignedTAEID;
        set
        {
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].assignedTAEID = value;
        }
    }
    public bool isUnit
    {
        get => CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isUnit;
        set
        {
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isUnit = value;
            if (value) unitInfo = GetComponent<UnitInfo>();
        }
    }
    public bool isScenery
    {
        get => CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isScenery;
        set
        {
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isScenery = value;
        }
    }
    public bool isSelectable
    {
        get => CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isSelectable;
        set
        {
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].isSelectable = value;
        }
    }
    private UnitInfo _unitInfoBehind;
    public UnitInfo unitInfo 
    { 
        get => _unitInfoBehind; 
        set 
        { 
            _unitInfoBehind = value;
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID].unitInfoId =
                value.unitInfoID;
        } 
    }

    private void Awake()
    {
        // assign locatableID
        locatableID = nextLocatableID;
        // nextLocatableID++;
        if (!CurrentGameState.Instance.gameStateData.locatableDataDict.ContainsKey(locatableID))
        {
            CurrentGameState.Instance.gameStateData.locatableDataDict[locatableID] = new LocatableData();
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
    private void OnDestroy()
    {
        if (!_preparedForDeath) PreDestructionProtocols();
        try { LocatableObjectDestroyed(); } catch { }
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
        CurrentGameState.Instance.gameStateData.locatableDataDict.Remove(locatableID);

        // mark all this complete
        _preparedForDeath = true;
    }
    public static void WipeAllLocatableObjectsAndReset()
    {
        List<LocatableObject> allLocatables = locatableObjectsById.Values.ToList();
        foreach (var entry in allLocatables) 
        {
            if (entry.isUnit) entry.unitInfo.updateVisionOnDestroy = false;
            entry.PreDestructionProtocols();
            Destroy(entry.gameObject);
        }
        locatableObjectsById = new Dictionary<int, LocatableObject>();
        nextLocatableID = 0;
    }
    private void WipeThisFromPlayerOwnershipDicts()
    {
        foreach (PlayerProperties player in PlayerSetupScript.Instance.playerList)
            player.ownedObjectIds.Remove(locatableID);
    }
}
