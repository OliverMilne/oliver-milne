using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HotkeysScript : MonoBehaviour
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
        // open/close game menu
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            if (UIControlScript.Instance.InGameMenu.activeInHierarchy) 
                UIControlScript.Instance.InGameMenu.SetActive(false);
            else UIControlScript.Instance.InGameMenu.SetActive(true);
        }
        // toggle sprite visibility
        if (Input.GetKeyDown(KeyCode.Space) && _selectorScript.selectedObject != null) 
        { 
            UnitGraphicsController unitGraphicsController = 
                _selectorScript.selectedObject.GetComponent<UnitGraphicsController>();
            if (unitGraphicsController != null) unitGraphicsController.ToggleSpriteVisibility();
            Debug.Log("DebugControlsScript: Sprite visibility toggled for selected object");
        }
        // autoexplore
        if (Input.GetKeyDown(KeyCode.A) && _selectorScript.selectedObject != null
            && _selectorScript.selectedObject.GetComponent<LocatableObject>().isUnit)
        {
            // immediately make the selected unit autoexplore
            AIUnitBehaviours.ExploreUndirected(
                _selectorScript.selectedObject.locatableObject.locatableID, PlayerProperties.humanPlayerID);
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
            Debug.Log("Attempting load game");
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
        // show two move accessible tiles on selected unit
        if (Input.GetKeyDown(KeyCode.R) && _selectorScript.selectedObject != null
            && _selectorScript.selectedObject.GetComponent<LocatableObject>().isUnit)
        {
            OverlayGraphicsScript.Instance.DebugTemporaryOverlay(
                _selectorScript.selectedObject.GetComponent<LocatableObject>(), 2);
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
                        tae.GetPlayersForWhomTileVisible().Count().ToString()));
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
            else foreach (var entry in CurrentGameState.Instance.gameStateData.locatableDataDict)
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
