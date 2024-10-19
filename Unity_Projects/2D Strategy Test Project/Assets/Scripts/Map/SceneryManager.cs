using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This lists scenery assets and provides methods for adding and removing them.
/// Note that movement effects are handled by the TileArrayEntry for now.
/// </summary>
public class SceneryManager : MonoBehaviour
{
    private static SceneryManager instance;
    public static SceneryManager Instance { get { return instance; } }

    public MapArrayScript mapArrayScript;
    public TileFinders tileFinders;

    public GameObject SceneryPrefab;

    public Sprite Cliff_W;
    public Sprite Cliff_NW;
    public Sprite Cliff_SW;
    public Sprite Cliff_NE;
    public Sprite Cliff_SE;
    public Sprite Cliff_E;
    public Sprite Forest;

    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }
    public void AddCliffs(TileArrayEntry tae, HexDir dir)
    {
        GameObject newScenery = 
            Instantiate(SceneryPrefab, mapArrayScript.tilemap.CellToWorld(tae.TileLoc), Quaternion.identity);
        SpriteRenderer spriteRenderer = newScenery.GetComponent<SpriteRenderer>();
        switch (dir) 
        {
            case HexDir.W: spriteRenderer.sprite = Cliff_W; break;
            case HexDir.NW: spriteRenderer.sprite = Cliff_NW; break;
            case HexDir.SW: spriteRenderer.sprite = Cliff_SW; break;
            case HexDir.E: spriteRenderer.sprite = Cliff_E; break;
            case HexDir.NE: spriteRenderer.sprite = Cliff_NE; break;
            case HexDir.SE: spriteRenderer.sprite = Cliff_SE; break;
            default: break;
        }

        LocatableObject newSceneryLocatable = newScenery.GetComponent<LocatableObject>();
        newSceneryLocatable.isScenery = true;
        tae.AssignTileContents(newSceneryLocatable);

        SceneryInfo newSceneryInfo = newScenery.GetComponent<SceneryInfo>();
        newSceneryInfo.sceneryType = SceneryType.Cliff;
        newSceneryInfo.sceneryDirection = dir;
    }
    public void AddCliffs(Vector3Int tileLoc, HexDir dir)
    {
        TileArrayEntry tae = tileFinders.GetTileArrayEntryAtLocationQuick(tileLoc);
        AddCliffs(tae, dir);
    }
    public void AddForest(TileArrayEntry tae)
    {
        GameObject newScenery =
            Instantiate(SceneryPrefab, mapArrayScript.tilemap.CellToWorld(tae.TileLoc), Quaternion.identity);
        SpriteRenderer spriteRenderer = newScenery.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = Forest;

        LocatableObject newSceneryLocatable = newScenery.GetComponent<LocatableObject>();
        newSceneryLocatable.isScenery = true;
        tae.AssignTileContents(newSceneryLocatable);

        SceneryInfo newSceneryInfo = newScenery.GetComponent<SceneryInfo>();
        newSceneryInfo.sceneryType = SceneryType.Forest;
    }
    public void AddForest(Vector3Int tileLoc)
    {
        TileArrayEntry tae = tileFinders.GetTileArrayEntryAtLocationQuick(tileLoc);
        AddForest(tae);
    }
    private bool IdentifyCliff(LocatableObject loco, HexDir dir)
    {
        if (!loco.isScenery) return false;
        SceneryInfo locoSceneryInfo = loco.GetComponent<SceneryInfo>();
        if (locoSceneryInfo.sceneryType == SceneryType.Cliff && locoSceneryInfo.sceneryDirection == dir) 
            return true;
        else return false;
    }
    public void RemoveCliffs(TileArrayEntry tae, HexDir dir)
    {
        // Debug.Log("RemoveCliffs called on TileArrayEntry " + tae.taeID);
        foreach (int locoId in tae.tileContentsIds)
        {
            try
            {
                LocatableObject loco = LocatableObject.locatableObjectsById[locoId];
                if (IdentifyCliff(loco, dir))
                {
                    loco.PreDestructionProtocols();
                    GameObject.Destroy(loco.gameObject);
                }
            } catch { }
        }
        // tae.ClearNullTileContents();
    }
    public void RemoveCliffs(Vector3Int tileLoc, HexDir dir)
    {
        TileArrayEntry tae = tileFinders.GetTileArrayEntryAtLocationQuick(tileLoc);
        RemoveCliffs(tae, dir);
    }
    public void SpawnSceneryFromGameStateInfo(int locatableId)
    {
        LocatableObject.nextLocatableID = locatableId;

        var newScenery = Instantiate(SceneryPrefab);

        SpriteRenderer spriteRenderer = newScenery.GetComponent<SpriteRenderer>();
        if (newScenery.GetComponent<SceneryInfo>().sceneryType == SceneryType.Cliff)
            switch (newScenery.GetComponent<SceneryInfo>().sceneryDirection)
            {
                case HexDir.W: spriteRenderer.sprite = Cliff_W; break;
                case HexDir.NW: spriteRenderer.sprite = Cliff_NW; break;
                case HexDir.SW: spriteRenderer.sprite = Cliff_SW; break;
                case HexDir.E: spriteRenderer.sprite = Cliff_E; break;
                case HexDir.NE: spriteRenderer.sprite = Cliff_NE; break;
                case HexDir.SE: spriteRenderer.sprite = Cliff_SE; break;
                default: break;
            }
        else if (newScenery.GetComponent<SceneryInfo>().sceneryType == SceneryType.Forest)
            spriteRenderer.sprite = Forest;
        InitialiserScript.Instance.spawnCount++;
    }
}
