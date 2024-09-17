using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverlayGraphicsScript : MonoBehaviour
{
    private static OverlayGraphicsScript instance;
    public static OverlayGraphicsScript Instance {  get { return instance; } }

    public GameObject moveRangeEdgePrefab;
    public GameObject moveRangeLowerCornerPrefab;
    public GameObject moveRangeUpperCornerPrefab;
    public GameObject moveRangeEdgePrefabOrange;
    public GameObject moveRangeLowerCornerPrefabOrange;
    public GameObject moveRangeUpperCornerPrefabOrange;
    public GameObject tileDebugText;

    private List<GameObject> _selectionGraphics;

    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }
    public void OverlayGraphicsScript_Initialise()
    {
        if (_selectionGraphics != null) foreach (GameObject go in _selectionGraphics) Destroy(go);
        _selectionGraphics = new List<GameObject>();
    }

    public void DrawSelectionGraphics(SelectableObject selectedObject)
    {
        // Debug.Log("SelectorScript: DrawSelectionGraphics called");
        foreach (GameObject gameObject in _selectionGraphics)
        {
            Destroy(gameObject);
        }
        _selectionGraphics = new List<GameObject>();

        // what to do if it's a unit
        UnitInfo selectedUnitInfo;
        if (selectedObject == null) return;
        if (selectedObject.TryGetComponent<UnitInfo>(out selectedUnitInfo))
        {
            // draw the selection graphics
            LocatableObject selectedLocatable = selectedObject.GetComponent<LocatableObject>();
            List<TileArrayEntry> areaTiles = UnitMovement.Instance.GetLocation1TurnReachableTiles(
                selectedLocatable,
                selectedUnitInfo.moveDistance.value,
                true);
            if (selectedUnitInfo.currentActions > 0)
                _selectionGraphics = BorderIllustrator.DrawBorders(
                    areaTiles, moveRangeEdgePrefab, moveRangeLowerCornerPrefab, moveRangeUpperCornerPrefab);
            // use orange borders instead of green if it's going to cost Readiness
            else _selectionGraphics = BorderIllustrator.DrawBorders(
                    areaTiles, moveRangeEdgePrefabOrange, moveRangeLowerCornerPrefabOrange,
                    moveRangeUpperCornerPrefabOrange);

            // display selected unit on tile
            TileArrayEntry unitTile = selectedLocatable.GetLocatableLocationTAE();
            unitTile.SetVisibleUnit(selectedUnitInfo.unitInfoID);
        }
    }
    public GameObject CreateTileDebugText(TileArrayEntry tae, string text)
    {
        Vector3 taePosition = tae.GetTileWorldLocation();
        Vector3 createPosition = new Vector3(taePosition.x, taePosition.y, taePosition.z);
        GameObject debugTextObject =
            Instantiate(tileDebugText, createPosition, Quaternion.identity);
        debugTextObject.GetComponent<TextMesh>().text = text;
        return debugTextObject;
    }
}
