using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MouseBehaviourScript : MonoBehaviour
{
    private static MouseBehaviourScript instance;
    /// <summary>
    /// This controls mouse behaviour. It's attached to ScriptBucket.
    /// </summary>
    public static MouseBehaviourScript Instance { get { return instance; } }
    private void Awake()
    {
        if (name == "ScriptBucket" && instance == null) instance = this;
    }

    public Tilemap tilemapInUse;
    private Vector3Int _hoverTileLoc;
    public Tile clickerTile;
    private List<GameObject> _hoverGameObjectsBehind = new List<GameObject>();
    /// <summary>
    /// ALWAYS WAIT FOR _hoverGameObjectsSemaphore BEFORE USING THIS! 
    /// When set, this destroys every GameObject in _hoverGameObjectsBehind. Otherwise it behaves
    /// just like a normal dictionary.
    /// </summary>
    private List<GameObject> _HoverGameObjects
    {
        get => _hoverGameObjectsBehind;
        set
        {
            if (_hoverGameObjectsBehind != null)
                foreach (GameObject gameObject in _hoverGameObjectsBehind)
                {
                    Destroy(gameObject);
                }
            _hoverGameObjectsBehind = value;
        }
    }
    private readonly SemaphoreSlim _hoverGameObjectsSemaphore = new SemaphoreSlim(1,1);

    public void MouseBehaviourScript_Initialise()
    {
        _HoverGameObjects = new List<GameObject>();
        // Debug.Log("MouseBehaviourScript initialised");
    }

    void Update()
    {
        // don't do any map stuff if a popup is open
        if (UIControlScript.Instance.popupIsOpen) return;

        // Getting hold of the hovered tile
        Vector3Int tileLoc = GetHoveredTileLoc();

        // Hover behaviour
        if (tileLoc != _hoverTileLoc)
        {
            _hoverTileLoc = tileLoc;
            _hoverGameObjectsSemaphore.Wait();
            _HoverGameObjects = new List<GameObject>();
            _hoverGameObjectsSemaphore.Release();

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
                    LocatableObject selectedUnit 
                        = SelectorScript.Instance.selectedObject.GetComponent<LocatableObject>();
                    if (selectedUnit.GetLocatableLocationTAE().TileLoc != tileLoc)
                        HoverMovementPreview(selectedUnit, myTileAE);
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
                    TileArrayEntry tileAtTileLoc 
                        = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc);
                    if (tileAtTileLoc.GetVisibilityByPlayerID(PlayerProperties.humanPlayerID) 
                        == TileVisibility.Visible || tileAtTileLoc.forceVisible)
                        SelectorScript.Instance.SelectTileLocContents(tileLoc);
                    else SelectorScript.Instance.ClearSelection();
                    // Debug.Log("Attempted to SelectTileLocContents");
                }
                _hoverGameObjectsSemaphore.Wait();
                _HoverGameObjects = new List<GameObject>();
                _hoverGameObjectsSemaphore.Release();
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
                _hoverGameObjectsSemaphore.Wait();
                _HoverGameObjects = new List<GameObject>();
                _hoverGameObjectsSemaphore.Release();
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
    private void HoverMovementPreview(LocatableObject unit, TileArrayEntry targetTAE)
    {
        int startingTileUpdateNumber = TileArrayEntry.tileUpdateNumber;
        List<GameObject> moveUnitPreview = UnitMovement.Instance.MoveUnitPreview(
                            unit,
                            targetTAE,
                            unit.unitInfo.moveDistance.value
                            );
        _hoverGameObjectsSemaphore.Wait();
        if (startingTileUpdateNumber == TileArrayEntry.tileUpdateNumber
            && GetHoveredTileLoc() == targetTAE.TileLoc) 
        {
            _HoverGameObjects = moveUnitPreview; 
        }
        _hoverGameObjectsSemaphore.Release();
    }
}
