using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// This class contains methods for units to do things like refresh their actions at the start of a turn.
/// It's attached to the unit itself.
/// </summary>
public class UnitBehaviour : MonoBehaviour
{
    private LocatableObject _thisLocatableObject;
    private SelectableObject _thisSelectableObject;
    private UnitInfo _unitInfo;

    private void Awake()
    {
        _thisLocatableObject = GetComponent<LocatableObject>();
        _thisSelectableObject = GetComponent<SelectableObject>();
        _unitInfo = GetComponent<UnitInfo>();

        // Debug.Log("Unit " + _unitInfo.unitInfoID + "'s UnitBehaviour script is awake!");
        _thisSelectableObject.ActionClickBehaviourFires += DefaultUnitAction;
        TurnManagerScript.Instance.OnStartTurn += StartTurnBehaviour;
    }

    private float DealHPDamage(UnitInfo targetUnit)
    {
        float damageRoll 
            = UnityEngine.Random.Range(_unitInfo.maxMeleeDamage * 0.5f, _unitInfo.maxMeleeDamage);
        Debug.Log("damageRoll is " + damageRoll);
        float readinessAdjustedDamageRoll = damageRoll * _unitInfo.currentReadiness;
        Debug.Log("readinessAdjustedDamageRoll is " + readinessAdjustedDamageRoll);
        float defenceAdjustedDamageRoll = readinessAdjustedDamageRoll * (1 - targetUnit.EffectiveDefence);
        Debug.Log("defenceAdjustedDamageRoll is " + defenceAdjustedDamageRoll);
        targetUnit.hitpoints -= Mathf.CeilToInt(defenceAdjustedDamageRoll);
        return defenceAdjustedDamageRoll;
    }
    /// <summary>
    /// This method picks the appropriate action based on the target tile and the UnitInfo
    /// </summary>
    /// <param name="t"></param>
    private void DefaultUnitAction(TileArrayEntry t)
    {
        Move(t);
    }
    public void MeleeAttackTile(TileArrayEntry t)
    {
        // get units in tile
        List<UnitInfo> unitsInTile = new List<UnitInfo>();
        foreach (int id in t.tileContentsIds)
        {
            if (LocatableObject.locatableObjectsById[id].isUnit)
                unitsInTile.Add(LocatableObject.locatableObjectsById[id].GetComponent<UnitInfo>());
        }

        // get hostile units from that list
        List<UnitInfo> hostileUnitsInTile = unitsInTile.Where(x => x.ownerID != _unitInfo.ownerID).ToList();

        // make sure there're units in that list
        if (hostileUnitsInTile.Count == 0) 
        {
            Debug.Log("No hostiles found in tile!");
            return; 
        }

        // pick toughest defender
        int defenderIndex = 0;
        foreach (UnitInfo defender in hostileUnitsInTile)
        {
            if (defender.meleeDefence * defender.currentReadiness 
                > hostileUnitsInTile[defenderIndex].meleeDefence 
                * hostileUnitsInTile[defenderIndex].currentReadiness)
                defenderIndex = hostileUnitsInTile.FindIndex(x => x.unitInfoID == defender.unitInfoID);
        }
        UnitInfo tileDefender = hostileUnitsInTile[defenderIndex];
        Debug.Log("tileDefender is unit " + tileDefender.unitInfoID);
        int tileDefMaxHP = tileDefender.maxHP;

        // deal HP damage to defender
        float damageTaken = DealHPDamage(tileDefender);
        Debug.Log("damageTaken is " + damageTaken);

        // update hostileUnitsInTile in case something died
        unitsInTile = new List<UnitInfo>();
        foreach (int id in t.tileContentsIds)
        {
            if (LocatableObject.locatableObjectsById[id].isUnit)
                unitsInTile.Add(LocatableObject.locatableObjectsById[id].GetComponent<UnitInfo>());
        }
        hostileUnitsInTile = unitsInTile.Where(x => x.ownerID != _unitInfo.ownerID).ToList();
        // deal readiness damage to every hostile unit in the tile
        foreach (UnitInfo unit in hostileUnitsInTile)
        {
            unit.currentReadiness -= damageTaken/tileDefMaxHP;
        }
    }
    private void Move(TileArrayEntry t)
    {
        if (PlayerProperties.playersById[_unitInfo.ownerID].actions > 0)
        {
            if (UnitMovement.Instance.MoveUnitDefault(
                GetComponent<LocatableObject>(), t, _unitInfo.moveDistance.value))
            {
                PlayerProperties.playersById[_unitInfo.ownerID].actions--;
            }
        }
        else Debug.Log("No actions remaining!");
    }
    public void PreDestructionProtocols()
    {
        TurnManagerScript.Instance.OnStartTurn -= StartTurnBehaviour;
    }
    private void StartTurnBehaviour()
    {
        if (TurnManagerScript.Instance.CurrentPlayer.playerID == _unitInfo.ownerID)
            _unitInfo.currentActions = _unitInfo.maxActions;
    }
}
