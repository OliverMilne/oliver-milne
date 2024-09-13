using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This governs unit vision.
/// It attaches to ScriptBucket.
/// </summary>
public class UnitVisionScript : MonoBehaviour
{
    private static UnitVisionScript instance;
    public static UnitVisionScript Instance {  get { return instance; } }

    public MapArrayScript mapArrayScript;
    public TurnManagerScript turnManagerScript;
    private PlayerProperties _thisTurnsPlayer;

    private void Awake()
    {
        if (name == "ScriptBucket") instance = this;
    }
    private List<TileArrayEntry> getVisibleTiles(LocatableObject locatable)
    {
        List<TileArrayEntry> visibleTiles = new List<TileArrayEntry>();
        List<TileArrayEntry> nextTiles = new List<TileArrayEntry>();
        visibleTiles.Add(locatable.GetLocatableLocationTAE());
        for (int i = 0; i < 2; i++)
        {
            foreach (TileArrayEntry tae in visibleTiles) nextTiles.AddRange(tae.GetAdjacentTAEs());
            visibleTiles.AddRange(nextTiles);
        }
        return visibleTiles;
    }
    public void UpdateUnitVision()
    {
        foreach (PlayerProperties player in PlayerProperties.playersById.Values)
        {
            // turn all Visible tiles' visibility to Hidden
            foreach (TileArrayEntry tae in mapArrayScript.MapTileArray)
            {
                if (tae.visibilityByPlayerID[player.playerID] == TileVisibility.Visible)
                {
                    tae.visibilityByPlayerID[player.playerID] = TileVisibility.Explored;
                }
            }

            // for each of the player's units, mark tiles they can see Visible
            foreach (int locatableId in player.ownedObjectIds)
            {
                LocatableObject locatable = LocatableObject.locatableObjectsById[locatableId];
                // get tiles within 3
                List<TileArrayEntry> visibleTiles = getVisibleTiles(locatable);
                // set them to Visible
                foreach (TileArrayEntry tae in visibleTiles) 
                    tae.visibilityByPlayerID[player.playerID] = TileVisibility.Visible;
            }
        }
        // update all tiles' visibility graphics
        foreach (TileArrayEntry tae in mapArrayScript.MapTileArray) 
            tae.SetTileVisibilityGraphic(PlayerProperties.humanPlayerID);
    }
}
