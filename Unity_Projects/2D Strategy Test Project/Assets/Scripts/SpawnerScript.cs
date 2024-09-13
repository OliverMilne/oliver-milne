using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AddressableAssets;

public class SpawnerScript : MonoBehaviour
{
    private static SpawnerScript instance;
    public static SpawnerScript Instance { get { return instance; } }

    public Transform unitPrefab;
    public UnitInfo spawningUnitInfo;
    public Tilemap tilemap;

    private int _defaultUnit_currentActions ;
    private float _defaultUnit_currentReadiness;
    private int _defaultUnit_hitpoints;
    private int _defaultUnit_maxActions;
    private float _defaultUnit_maxMeleeDamage;
    private float _defaultUnit_meleeDefence;
    private int _defaultUnit_moveDistance;
    private string _defaultUnit_unitName;
    private string _defaultUnit_unitSpriteResourcesAddress;
    private string _defaultUnit_unitPlayerColourSpriteResourcesAddress;

    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }
    public void SpawnerScript_Initialise()
    {
        UnitInfo.nextUnitInfoID = 0;

        _defaultUnit_currentActions = 1;
        _defaultUnit_currentReadiness = 1;
        _defaultUnit_hitpoints = 10;
        _defaultUnit_maxActions = 1;
        _defaultUnit_maxMeleeDamage = 10;
        _defaultUnit_meleeDefence = 0.25f;
        _defaultUnit_moveDistance = 4;
        _defaultUnit_unitName = "Default Unit";
        _defaultUnit_unitSpriteResourcesAddress = "Clubman Base";
        _defaultUnit_unitPlayerColourSpriteResourcesAddress = "Clubman Player Colour";
    }

    public void DefaultUnitSpawn(int playerId, TileArrayEntry tae)
    {
        CurrentGameState.Instance.gameStateInfo.unitDataDict[-1] = new UnitData();
        spawningUnitInfo.FillOutUnitInfo(
            _defaultUnit_currentActions,
            _defaultUnit_currentReadiness,
            _defaultUnit_hitpoints,
            _defaultUnit_maxActions,
            _defaultUnit_maxMeleeDamage,
            _defaultUnit_meleeDefence,
            _defaultUnit_moveDistance,
            playerId,
            _defaultUnit_unitName,
            _defaultUnit_unitSpriteResourcesAddress,
            _defaultUnit_unitPlayerColourSpriteResourcesAddress );

        Transform spawnedUnit = 
            Instantiate(unitPrefab, tilemap.CellToWorld(tae.TileLoc), Quaternion.identity);
        // Debug.Log("Spawned Transform " + spawnedUnit.name);
        LocatableObject spawnedLocatableObject = spawnedUnit.GetComponent<LocatableObject>();
        spawnedLocatableObject.isSelectable = true;
        spawnedLocatableObject.isUnit = true;
        // Debug.Log("Spawned LocatableObject " + spawnedLocatableObject.name);
        tae.AssignTileContents(spawnedLocatableObject);
    }
    public void SpawnUnit(int playerId, TileArrayEntry tae, UnitInfo unitInfo)
    {
        spawningUnitInfo.CopyUnitInfo(unitInfo);
        spawningUnitInfo.ownerID = playerId;

        Transform spawnedUnit = 
            Instantiate(unitPrefab, tilemap.CellToWorld(tae.TileLoc), Quaternion.identity);
        LocatableObject spawnedLocatableObject = spawnedUnit.GetComponent<LocatableObject>();
        spawnedLocatableObject.isSelectable = true;
        spawnedLocatableObject.isUnit = true;
        tae.AssignTileContents(spawnedLocatableObject);
    }
    public void SpawnUnitFromGameStateInfo(int locatableId)
    {
        LocatableObject.nextLocatableID = locatableId;
        UnitInfo.nextUnitInfoID 
            = (int)CurrentGameState.Instance.gameStateInfo.locatablesInfoDict[locatableId].unitInfoId;
        var newUnit = Instantiate(unitPrefab);
        newUnit.GetComponent<LocatableObject>().unitInfo = newUnit.GetComponent<UnitInfo>();
        if (newUnit.GetComponent<LocatableObject>().unitInfo.unitInfoID
            != newUnit.GetComponent<UnitInfo>().unitInfoID) 
            throw new System.Exception("unitInfoID mismatch for locatableID " + locatableId);
        InitialiserScript.Instance.spawnCount++;
    }
}
