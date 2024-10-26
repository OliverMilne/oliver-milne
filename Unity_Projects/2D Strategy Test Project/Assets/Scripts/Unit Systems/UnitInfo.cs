using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
// using UnityEditor.PackageManager;
using UnityEngine;
using Newtonsoft.Json.Bson;
using System.Collections.ObjectModel;

/// <summary>
/// This class holds all the stats and info on a unit except its location (held by LocatableObject).
/// Should also contain sprite info.
/// </summary>
public class UnitInfo : MonoBehaviour, IDisplayable
{
    /// <summary>
    /// Where there's an associated LocatableObject, this should be the same as the locatableID.
    /// </summary>
    public int unitInfoID { get; private set; }
    public List<IBuffedVariable> Buffables;
    public bool updateVisionOnDestroy = true;

    // data drawn from associated UnitData
    public int currentActions 
    { 
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].currentActions; 
        set 
        {
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].currentActions 
                = value;
            if (!TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _)) 
                SelectorScript.Instance.RefreshSelectionGraphics();
            OnDisplayableInfoUpdated();
        }
    }
    public float currentReadiness
    { 
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].currentReadiness;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].currentReadiness 
                = Mathf.Clamp(value, 0, 1);
            // make sure this is actually a unit and not the SpawnerScript!
            if (TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _)) return;
            GetComponent<UnitGraphicsController>().RenderUnitReadinessBar();
            OnDisplayableInfoUpdated();
        }
    }
    public float EffectiveDefence
    {
        get => meleeDefence * currentReadiness;
    }
    /// <summary>
    /// Always set this AFTER maxHP!
    /// </summary>
    public int hitpoints
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].hitpoints;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].hitpoints 
                = Mathf.Clamp(value, 0, maxHP); 
            // make sure this is actually a unit and not the SpawnerScript!
            if (TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _)) return;
            OnDisplayableInfoUpdated();
            GetComponent<UnitGraphicsController>().RenderUnitHealthBar();
            if (CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].hitpoints <= 0)
            {
                GetComponent<LocatableObject>().PreDestructionProtocols();
                Debug.Log("Unit " + unitInfoID + " died when its hitpoints reached zero!");
                Destroy(gameObject);
            }
        }
    }
    public int maxActions
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxActions;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxActions = value;
            OnDisplayableInfoUpdated();
        }
    }
    public int maxHP
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxHP;
        set
        {
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxHP = value;
            if (CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxHP <= hitpoints)
            {
                hitpoints = CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxHP;
            }
            if (TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _)) return;
            GetComponent<UnitGraphicsController>().RenderUnitHealthBar();
            OnDisplayableInfoUpdated();
        }
    }
    public float maxMeleeDamage
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxMeleeDamage;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].maxMeleeDamage = value;
            OnDisplayableInfoUpdated();
        }
    }
    public float meleeDefence
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].meleeDefence;
        set
        {
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].meleeDefence
                = Mathf.Clamp(value, 0, 1);
            OnDisplayableInfoUpdated();
        }
    }
    public BuffedInt moveDistance
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].moveDistance;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].moveDistance = value;
            OnDisplayableInfoUpdated();
        }
    }
    public int ownerID
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].ownerID;
        set
        {
            SetPlayerOwnershipRegistration(value);
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].ownerID = value;
            // Apply player colour
            if (UnitSprite != null) ApplySprite();
            OnDisplayableInfoUpdated();
        }
    }
    public string unitName
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].unitName;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].unitName = value;
            OnDisplayableInfoUpdated();
        }
    }
    public Sprite UnitSprite
    {
        get => Resources.Load<Sprite>(unitSpriteResourcesAddress);
    }
    public string unitSpriteResourcesAddress
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].unitSpriteResourcesAddress;
        set 
        { 
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID].unitSpriteResourcesAddress 
                = value;
            OnDisplayableInfoUpdated();
        }
    }
    public Sprite UnitPlayerColourSprite
    {
        get => Resources.Load<Sprite>(unitPlayerColourSpriteResourcesAddress);
    }
    public string unitPlayerColourSpriteResourcesAddress
    {
        get => CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID]
            .unitPlayerColourSpriteResourcesAddress;
        set
        {
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID]
                .unitPlayerColourSpriteResourcesAddress = value;
            OnDisplayableInfoUpdated();
        }
    }

    private void Awake()
    {
        // subscriptions
        OnDisplayableInfoUpdated += MinimalOnDisplayableInfoUpdated;
        if (!TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _))
        {
            TurnManagerScript.Instance.OnStartTurn += StartOwnersTurnRegen;
            TurnManagerScript.Instance.OnStartTurn += TickDownBuffsOnOwnersTurn;
        }

        // set unitInfoID
        if (TryGetComponent<UnitSpawnerScript>(out UnitSpawnerScript _)) unitInfoID = -1;
        else 
        { 
            unitInfoID = GetComponent<LocatableObject>().locatableID;
        }

        // write UnitData
        if (!CurrentGameState.Instance.gameStateData.unitDataDict.ContainsKey(unitInfoID))
        {
            CurrentGameState.Instance.gameStateData.unitDataDict[unitInfoID] = new UnitData();
            CopyUnitInfo(UnitSpawnerScript.Instance.spawningUnitInfo);
        }

        // setup non-saved fields
        Buffables = new List<IBuffedVariable>() { moveDistance };

        // Debug.Log("UnitInfo " + unitInfoID + " is awake!");
    }

    private void ApplySprite()
    {
        if (unitInfoID != -1)
        {
            UnitGraphicsController unitGraphicsController = GetComponent<UnitGraphicsController>();
            unitGraphicsController.ApplySprite();
        }
    }
    public void CopyUnitInfo(UnitInfo other)
    {
        currentActions = other.currentActions;
        currentReadiness = other.currentReadiness;
        maxHP = other.maxHP;
        hitpoints = other.hitpoints;
        maxActions = other.maxActions;
        maxMeleeDamage = other.maxMeleeDamage;
        meleeDefence = other.meleeDefence;
        moveDistance = other.moveDistance;
        unitSpriteResourcesAddress = other.unitSpriteResourcesAddress;
        unitPlayerColourSpriteResourcesAddress = other.unitPlayerColourSpriteResourcesAddress;
        unitName = other.unitName;

        ownerID = other.ownerID;

        // ApplySprite();

        // Debug.Log("UnitInfo " + unitInfoID + " copied info from UnitInfo " + other.unitInfoID);
    }
    public void FillOutUnitInfo(
        int currentActions,
        float currentReadiness,
        int hitpoints,
        int maxActions,
        float maxMeleeDamage,
        float meleeDefence,
        int moveDistance,
        int ownerID,
        string unitName,
        string unitSpriteResourcesAddress,
        string unitPlayerColourSpriteResourcesAddress
        )
    {
        this.currentActions = currentActions;
        this.currentReadiness = currentReadiness;
        this.maxHP = hitpoints; // gotta do this before hitpoints!
        this.maxMeleeDamage = maxMeleeDamage;
        this.meleeDefence = meleeDefence;
        this.hitpoints = hitpoints;
        this.maxActions = maxActions;
        this.moveDistance.baseValue = moveDistance;
        this.unitSpriteResourcesAddress = unitSpriteResourcesAddress;
        this.unitName = unitName;
        this.unitPlayerColourSpriteResourcesAddress = unitPlayerColourSpriteResourcesAddress;

        this.ownerID = ownerID;

        // ApplySprite();

        // Debug.Log("Filled out UnitInfo " + unitInfoID + ": " + unitName + " with custom fields");
    }
    private void MinimalOnDisplayableInfoUpdated() { }
    private void OnDestroy()
    {
        if (updateVisionOnDestroy)
            UnitVisionScript.Instance.UpdateUnitVision();
        // OverlayGraphicsScript.Instance.DrawSelectionGraphics(SelectorScript.Instance.selectedObject);
    }
    public event DisplayableInfoUpdated OnDisplayableInfoUpdated;
    public void PreDestructionProtocols()
    {
        TurnManagerScript.Instance.OnStartTurn -= StartOwnersTurnRegen;
        TurnManagerScript.Instance.OnStartTurn -= TickDownBuffsOnOwnersTurn;
        CurrentGameState.Instance.gameStateData.unitDataDict.Remove(unitInfoID);
    }
    private void SetPlayerOwnershipRegistration(int newOwnerID)
    {
        if (ownerID != newOwnerID 
            && TryGetComponent<LocatableObject>(out LocatableObject _) // so we know it's not ScriptBucket's UnitInfo
            )
        {
            // Remove unit from existing owner
            PlayerProperties owningPlayer 
                = PlayerSetupScript.Instance.playerList.Find(x => x.playerID == ownerID);
            if (owningPlayer != null)
                owningPlayer.ownedObjectIds.Remove(GetComponent<LocatableObject>().locatableID);

            // Register itself on its new owner's list of ownedObjects
            PlayerProperties newOwningPlayer = PlayerSetupScript.Instance.playerList.Find(x => x.playerID == newOwnerID);
            newOwningPlayer.ownedObjectIds.Add(GetComponent<LocatableObject>().locatableID);
            // Debug.Log("Added unit " + unitInfoID + " to Player " + newOwnerID + "'s ownedObjects");
        }
    }
    private void StartOwnersTurnRegen()
    {
        // Debug.Log("StartTurnRegen called for unit " + unitInfoID + ". Current player is " + TurnManagerScript.Instance.CurrentPlayer.playerName);
        if (TurnManagerScript.Instance.CurrentPlayer.playerID == ownerID)
        {
            currentReadiness += 0.2f;
            // Debug.Log("Unit " + unitInfoID + " regenerated O.2 readiness");
        }
    }
    private void TickDownBuffsOnOwnersTurn()
    {
        if (TurnManagerScript.Instance.CurrentPlayer.playerID == ownerID)
            foreach (IBuffedVariable b in Buffables) b.TickDownBuffs();
    }
    public string UnitInfoString()
    {
        return "unitInfoID: " + unitInfoID
            + "; currentActions: " + currentActions
            + "; currentReadiness: " + currentReadiness
            + "; hitpoints: " + hitpoints
            + "; maxActions: " + maxActions
            + "; maxMeleeDamage: " + maxMeleeDamage
            + "; meleeDefence: " + meleeDefence
            + "; moveDistance: " + moveDistance
            + "; ownerID: " + ownerID
            + "; unitName: " + unitName;
    }
}

public class UnitData
{
    public int? assignedMissionID = null;
    public int currentActions;
    /// <summary>
    /// This should always take a value between 0 and 1. It's a multiplier for damage and defence.
    /// </summary>
    public float currentReadiness;
    public int hitpoints;
    public int maxActions;
    public int maxHP;
    public float maxMeleeDamage;
    /// <summary>
    /// This should always take a value between 0 and 1. It's a multiplier for damage taken.
    /// </summary>
    public float meleeDefence;
    public BuffedInt moveDistance = new BuffedInt() { baseValue = 0 };
    public int ownerID = -2;
    public string unitName;
    public string unitSpriteResourcesAddress;
    public string unitPlayerColourSpriteResourcesAddress;
}
