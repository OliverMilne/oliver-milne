using System.Collections;
using System.Collections.Generic;
// using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System;

public class SelectorScript : MonoBehaviour
{
    private static SelectorScript instance;
    public static SelectorScript Instance { get { return instance; } }

    public int selectedIndex;

    public Tilemap tilemapInUse;

    public GameObject moveRangeEdgePrefab;
    public GameObject moveRangeLowerCornerPrefab;
    public GameObject moveRangeUpperCornerPrefab;

    private SelectableObject _selectedObjectBehind;
    public SelectableObject selectedObject
    {
        get => _selectedObjectBehind;
        set 
        { 
            _selectedObjectBehind = value; 
            UIControlScript.Instance.SelectedObjectDisplay(); 
            OverlayGraphicsScript.Instance.DrawSelectionGraphics(selectedObject); 
        }
    }

    // TODO: turn this into SelectorScript_Initialise and have the InitialiserScript call it
    private void Awake()
    {
        if (name == "Selector") instance = this;
    }

    public void ClearSelection()
    {
        selectedObject = null;
    }
    public void RefreshSelectionGraphics()
    {
        OverlayGraphicsScript.Instance.DrawSelectionGraphics(selectedObject);
    }
    public void SelectTileAEContents(TileArrayEntry selectionTile)
    {
        // tileContents may contain both selectable and unselectable objects
        if (selectionTile != null && selectionTile.GetTileContents().TrueForAll(x => !x.isSelectable)) 
        { 
            selectedObject = null; return; 
        }

        // first, reset _selectedIndex if selectedObject is null or in a different tile
        if (selectedObject == null) selectedIndex = 0;
        else if (selectedObject.GetComponent<LocatableObject>().GetLocatableLocationTAE() == null) 
            selectedIndex = 0;
        else if (selectionTile.taeID == 
            selectedObject.GetComponent<LocatableObject>().GetLocatableLocationTAE().taeID)
                selectedIndex = (selectedIndex + 1) % selectionTile.GetTileContents().Count;
        else selectedIndex = 0;
        // Debug.Log("SelectorScript: _selectedIndex = " + _selectedIndex);

        // now search through selectionTile.tileContents until you hit something selectable
        int whileBreaker = 0;
        while (whileBreaker < 1000)
        {
            whileBreaker++;
            if (selectionTile.GetTileContents()[selectedIndex].isSelectable)
            {
                selectedObject = selectionTile.GetTileContents()[selectedIndex].GetComponent<SelectableObject>();
                selectedObject.GetComponent<LocatableObject>().DebugLocatableInfo();
                break;
            }
            selectedIndex = (selectedIndex + 1) % selectionTile.tileContentsIds.Count;
        }
        // Debug.Log("Selected " + selectedObject.ToString());
    }
    public void SelectTileLocContents(Vector3Int tileLoc)
    {
        TileArrayEntry selectionTile = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(tileLoc);
        SelectTileAEContents(selectionTile);
    }
}
