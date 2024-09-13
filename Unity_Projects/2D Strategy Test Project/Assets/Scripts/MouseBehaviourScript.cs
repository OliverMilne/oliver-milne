using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MouseBehaviourScript : MonoBehaviour
{
    /// <summary>
    /// This controls mouse behaviour. It's attached to ScriptBucket.
    /// </summary>
    private static MouseBehaviourScript instance;
    public static MouseBehaviourScript Instance { get { return instance; } }
    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }

    public Tilemap tilemapInUse;
    private Vector3Int _hoverTileLoc;
    public Tile clickerTile;
    private List<GameObject> _hoverGameObjects;

    public void MouseBehaviourScript_Initialise()
    {
        if (_hoverGameObjects != null) 
            foreach (GameObject gameObject in _hoverGameObjects) Destroy(gameObject);
        _hoverGameObjects = new List<GameObject>();
        // Debug.Log("MouseBehaviourScript initialised");
    }

    void Update()
    {
        // Getting hold of the hovered tile
        Vector3Int tileLoc = GetHoveredTileLoc();

        // Hover behaviour
        if (tileLoc != _hoverTileLoc)
        {
            _hoverTileLoc = tileLoc;
            WipeHoverEntities();

            TileArrayEntry myTileAE = null;
            try { myTileAE = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc); }
            catch { }

            if (myTileAE == null)
            {
                // Debug.Log("No tile detected at " + worldMousePosition.ToString() + "; tileLoc = " + tileLoc.ToString());
            }
            else
            {
                // hover route behaviour
                if (SelectorScript.Instance.selectedObject != null) 
                {
                    LocatableObject selectedUnit = SelectorScript.Instance.selectedObject.GetComponent<LocatableObject>();
                    if (selectedUnit.GetLocatableLocationTAE().TileLoc != tileLoc)
                        _hoverGameObjects = UnitMovement.Instance.MoveUnitPreview(
                            selectedUnit,
                            myTileAE,
                            selectedUnit.unitInfo.moveDistance.value
                            );
                    // movement range preview for hovered selected unit
                    /*else _hoverGameObjects = _unitMovement.DebugAccessibleTilesPreview(
                            selectedUnit,
                            selectedUnit.unitInfo.moveDistance
                            );*/
                }
            }
        }

        // L-click effects
        // At some point I'm going to want double-left-click to stack select

        if (Input.GetMouseButtonDown(0)) 
        {
            if (false) { } // if the cursor is over the UI, don't select a tile
            else
            {
                Tile clickedTile = tilemapInUse.GetTile(tileLoc) as Tile;
                if (clickedTile != null)
                {
                    TileArrayEntry tileAtTileLoc = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc);
                    if (tileAtTileLoc.visibilityByPlayerID[PlayerProperties.humanPlayerID] == TileVisibility.Visible 
                        || tileAtTileLoc.forceVisible)
                        SelectorScript.Instance.SelectTileLocContents(tileLoc);
                    else SelectorScript.Instance.ClearSelection();
                    // Debug.Log("Attempted to SelectTileLocContents");
                }
                WipeHoverEntities();
            }
        }

        // R-click effects
        if (Input.GetMouseButtonDown(1))
        {
            if (false) { } // if the cursor is over the UI, don't act on a tile
            else if (SelectorScript.Instance.selectedObject != null)
            {
                try
                {
                    SelectorScript.Instance.selectedObject.ReceiveActionClick(
                        TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc));
                } 
                catch { }
                WipeHoverEntities();
            }
        }
    }

    public TileArrayEntry GetHoveredTile()
    {
        return TileFinders.Instance.GetTileArrayEntryAtLocationQuick(GetHoveredTileLoc());
    }
    private Vector3Int GetHoveredTileLoc()
    {
        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return tilemapInUse.WorldToCell(new Vector3(worldMousePosition.x, worldMousePosition.y));
    }
    private void WipeHoverEntities()
    {
        foreach (GameObject gameObject in _hoverGameObjects)
        {
            Destroy(gameObject);
        }
        _hoverGameObjects = new List<GameObject>();
    }
}
