using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugControlsScript : MonoBehaviour
{
    private OverlayGraphicsScript _overlayGraphicsScript;
    private SelectorScript _selectorScript;
    private List<GameObject> _debugOverlays;

    void Awake()
    {
        _overlayGraphicsScript = GetComponent<OverlayGraphicsScript>();
        _selectorScript = FindObjectOfType<SelectorScript>();

        _debugOverlays = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        // toggle sprite visibility
        if (Input.GetKeyDown(KeyCode.Space) && _selectorScript.selectedObject != null) 
        { 
            UnitGraphicsController unitGraphicsController = 
                _selectorScript.selectedObject.GetComponent<UnitGraphicsController>();
            if (unitGraphicsController != null) unitGraphicsController.ToggleSpriteVisibility();
            Debug.Log("DebugControlsScript: Sprite visibility toggled for selected object");
        }
        // apply movement buff
        if (Input.GetKeyDown(KeyCode.B) && _selectorScript.selectedObject != null
            && _selectorScript.selectedObject.GetComponent<LocatableObject>().isUnit)
        {
            _selectorScript.selectedObject.GetComponent<UnitInfo>().moveDistance.ApplyBuff(
                "DebugMovementBuff", new AdditiveBuff<int>
                {
                    description = "",
                    name = "",
                    turnsRemaining = 1,
                    value = 3
                });
            OverlayGraphicsScript.Instance.DrawSelectionGraphics(_selectorScript.selectedObject);
        }
        // spawn default unit for human player
        if (Input.GetKeyDown(KeyCode.C))
        {
            SpawnerScript.Instance.DefaultUnitSpawn(
                TurnManagerScript.Instance.CurrentPlayer.playerID,
                MouseBehaviourScript.Instance.GetHoveredTile());
        }
        // damage selected unit
        if (Input.GetKeyDown(KeyCode.D) && _selectorScript.selectedObject != null
            && _selectorScript.selectedObject.GetComponent<LocatableObject>().isUnit)
        {
            SelectorScript.Instance.selectedObject.GetComponent<UnitInfo>().hitpoints--;
        }
        // load game
        if (Input.GetKeyDown(KeyCode.L))
    {
        InitialiserScript.Instance.InitialiseLoadGame();
        Debug.Log("Loaded game");
    }
        // new game
        if (Input.GetKeyDown(KeyCode.N)) 
        {
            InitialiserScript.Instance.InitialiseNewGame();
        }
        // wipe all locatables and reset
        if (Input.GetKeyDown(KeyCode.O)) 
        {
            ReportTileContentsCount();
            LocatableObject.WipeAllLocatableObjectsAndReset();
            ReportTileContentsCount();
        }
        // render stat bar background on selected unit
        if (Input.GetKeyDown(KeyCode.R) && _selectorScript.selectedObject != null
            && _selectorScript.selectedObject.GetComponent<LocatableObject>().isUnit)
        {
            _selectorScript.selectedObject.GetComponent<UnitGraphicsController>().RenderUnitBarBackground();
        }
        // save game
        if (Input.GetKeyDown(KeyCode.S))
        {
            CurrentGameState.Instance.SaveGame();
            Debug.Log("Game saved!");
        }
        // show how many players can see each tile
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (_debugOverlays.Count > 0)
            {
                foreach (GameObject overlay in _debugOverlays) GameObject.Destroy(overlay);
                _debugOverlays.Clear();
            }
            else foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
                {
                    _debugOverlays.Add(_overlayGraphicsScript.CreateTileDebugText(
                        tae, 
                        tae.visibilityByPlayerID.Where(
                            x => x.Value == TileVisibility.Visible).Count().ToString()));
                }
        }
        // toggle tile number overlays
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (_debugOverlays.Count > 0)
            {
                foreach (GameObject overlay in _debugOverlays) GameObject.Destroy(overlay);
                _debugOverlays.Clear();
            }
            else foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
                {
                    _debugOverlays.Add(_overlayGraphicsScript.CreateTileDebugText(
                        tae, Mathf.Ceil(tae.visibleUnitID).ToString()));
                }
        }
        // some debug stuff about showing unit sprites
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (_debugOverlays.Count > 0)
            {
                foreach (GameObject overlay in _debugOverlays) GameObject.Destroy(overlay);
                _debugOverlays.Clear();
            }
            else foreach (var entry in CurrentGameState.Instance.gameStateInfo.locatablesInfoDict)
                {
                    if (entry.Value.isUnit
                        && LocatableObject.locatableObjectsById[entry.Key]
                        .GetComponent<UnitGraphicsController>()._isVisible)
                        _debugOverlays.Add(_overlayGraphicsScript.CreateTileDebugText(
                            MapArrayScript.Instance.MapTileArrayDict[
                                LocatableObject.locatableObjectsById[entry.Key].assignedTAEID],
                            entry.Value.unitInfoId.ToString()));
                }
        }
        // toggle FOW
        if (Input.GetKeyDown(KeyCode.Z))
        {
            MapArrayScript mapArrayScript = GetComponent<MapArrayScript>();
            mapArrayScript.ToggleFOW();
        }
    }
    private void ReportTileContentsCount()
    {
        int tileContentsCount = 0;
        foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
        {
            tileContentsCount += tae.tileContentsIds.Count;
        }
        Debug.Log("tileContentsCount = " + tileContentsCount);
    }
}
