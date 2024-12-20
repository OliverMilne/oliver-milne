using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// This controls mouse behaviour. It's attached to ScriptBucket.
/// </summary>
public class MouseBehaviourScript : MonoBehaviour
{
    private static MouseBehaviourScript instance;
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
    /// Should only be access from main thread.
    /// When set, this destroys every GameObject in _hoverGameObjectsBehind. Otherwise it behaves
    /// just like a normal list.
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
    /// <summary>
    /// Stores the path for the movement preview. Wait for the associated semaphore before accessing it!
    /// </summary>
    private List<TileArrayEntry> _movementPreviewBuffer = new List<TileArrayEntry>();
    private readonly SemaphoreSlim _movementPreviewBufferSemaphore = new SemaphoreSlim(1,1);
    private volatile int _movementPreviewTileUpdateNumber = -1;
    private volatile int _movementPreviewSelectedLocatableID = -2;
    private volatile int _movementPreviewTargetTAEID = -1;
    private volatile int _movementPreviewBufferUpdated = 0;
    private bool _preventMouseInteractionBehind = false;
    public bool preventMouseInteraction 
    { 
        get
        {
            if (InitialiserScript.Instance.initialisationInProgress) return true;
            else return _preventMouseInteractionBehind;
        }
        set { _preventMouseInteractionBehind = value; } 
    }

    public void MouseBehaviourScript_Initialise()
    {
        _HoverGameObjects = new List<GameObject>();
        // Debug.Log("MouseBehaviourScript initialised");
    }

    void Update()
    {
        // don't do anything if the game's initialising
        if (preventMouseInteraction) return;

        // don't do any map stuff if a popup is open
        if (UIControlScript.Instance.popupIsOpen) return;

        // Getting hold of the hovered tile
        Vector3Int tileLoc = GetHoveredTileLoc();

        UpdateHoverGameObjectsLazily();

        // Hover behaviour
        if (tileLoc != _hoverTileLoc)
        {
            _hoverTileLoc = tileLoc;

            TileArrayEntry myTileAE = null;
            try { myTileAE = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc); }
            catch { }

            if (myTileAE == null)
            {
                // Debug.Log("No tile detected at " + worldMousePosition.ToString() + "; tileLoc = " + tileLoc.ToString());
            }
            else
            {
                // Get hover route asynchronously
                if (SelectorScript.Instance.selectedObject != null) 
                {
                    LocatableObject selectedUnit 
                        = SelectorScript.Instance.selectedObject.GetComponent<LocatableObject>();
                    if (selectedUnit.GetLocatableLocationTAE().TileLoc != tileLoc)
                    { 
                        AsyncUpdateHoverMovementPreviewBuffer(selectedUnit, myTileAE,
                            selectedUnit.GetLocatableLocationTAE());
                        // Debug.Log("Movement preview buffer update called");
                    }
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
                    if (tileAtTileLoc.IsTileVisibleToPlayerWithOverrides(PlayerProperties.humanPlayerID))
                    {
                        // Debug.Log("Firing SelectTileContents");
                        SelectorScript.Instance.SelectTileLocContents(tileLoc);
                    }
                    else 
                    {
                        // Debug.Log("Firing ClearSelection");
                        SelectorScript.Instance.ClearSelection(); 
                    }
                    // Debug.Log("Attempted to SelectTileLocContents");
                }
                _HoverGameObjects = new List<GameObject>();
            }
        }

        // R-click effects
        if (Input.GetMouseButtonDown(1))
        {
            if (false) { } // if the cursor is over the UI, don't act on a tile
            else if (SelectorScript.Instance.selectedObject != null
                && TurnManagerScript.Instance.CurrentPlayer.playerID == PlayerProperties.humanPlayerID)
            {
                try
                {
                    SelectorScript.Instance.selectedObject.ReceiveActionClick(
                        TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc));
                } 
                catch { }
                _HoverGameObjects = new List<GameObject>();
            }
        }
    }

    public TileArrayEntry GetHoveredTile()
    {
        Vector3Int hoveredTileLoc = GetHoveredTileLoc();
        if (hoveredTileLoc == null || !MapArrayScript.Instance.IsVector3IntATileLocOnTheMap(hoveredTileLoc)) 
            return null;
        return TileFinders.Instance.GetTileArrayEntryAtLocationQuick(hoveredTileLoc);
    }
    private Vector3Int GetHoveredTileLoc()
    {
        Vector3 worldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return tilemapInUse.WorldToCell(new Vector3(worldMousePosition.x, worldMousePosition.y));
    }
    private async void AsyncUpdateHoverMovementPreviewBuffer(LocatableObject unit, TileArrayEntry targetTAE,
        TileArrayEntry startingTAE)
    {
        await Task.Run(() =>
        {
            AsyncThreadsManager.DeclareThreadRunning();

            // assigning ints is atomic so this should be safe...?
            _movementPreviewTileUpdateNumber = TileArrayEntry.tileUpdateNumber;
            _movementPreviewSelectedLocatableID = unit.locatableID;
            _movementPreviewTargetTAEID = targetTAE.taeID;


            List<TileArrayEntry> previewPath = UnitMovement.AStarPathCalculator(
                                startingTAE,
                                targetTAE,
                                unit.unitInfo.ownerID
                                );
            _movementPreviewBufferSemaphore.Wait();
            if (previewPath != null)
            {
                _movementPreviewBuffer = previewPath;
                _movementPreviewBufferUpdated = 1;
                // Debug.Log("Movement preview buffer filled");
            }
            else _movementPreviewBuffer = new List<TileArrayEntry>() { startingTAE };
            _movementPreviewBufferSemaphore.Release();

            AsyncThreadsManager.DeclareThreadStopped();
        });
    }
    private void UpdateHoverGameObjectsLazily()
    {
        if (1 == Interlocked.Exchange(ref _movementPreviewBufferUpdated, 0))
        {
            if (SelectorScript.Instance.selectedObject == null) return;
            if (GetHoveredTile() == null) return;
            if (_movementPreviewTileUpdateNumber == TileArrayEntry.tileUpdateNumber
                && _movementPreviewSelectedLocatableID 
                == SelectorScript.Instance.selectedObject.locatableObject.locatableID
                && _movementPreviewTargetTAEID == GetHoveredTile().taeID)
            {
                _movementPreviewBufferSemaphore.Wait();
                _HoverGameObjects = new List<GameObject>();
                bool ignoreThis = true;
                int distanceCounter = 0;
                if (_movementPreviewBuffer.Count == 0)
                {
                    _movementPreviewBufferSemaphore.Release();
                    return;
                }
                foreach (TileArrayEntry entry in _movementPreviewBuffer)
                {
                    if (ignoreThis) { ignoreThis = false; }
                    else if (true)//distanceCounter <= maxMoveDistance)
                    {
                        GameObject marker = Instantiate(
                            UnitMovement.Instance.movementPreviewMarkerPrefab,
                            MapArrayScript.Instance.tilemap.CellToWorld(entry.TileLoc),
                            Quaternion.identity);
                        _HoverGameObjects.Add(marker);
                    }
                    // add future turn move preview functionality here
                    distanceCounter++;
                }
                _movementPreviewBufferSemaphore.Release();
                // Debug.Log("Generated move preview objects");
            }
        }
    }
}
