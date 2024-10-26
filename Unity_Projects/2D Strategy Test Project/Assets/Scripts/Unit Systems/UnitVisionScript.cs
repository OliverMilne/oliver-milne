using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private List<int> GetNeighboursBehind(
        Dictionary<int, int> tilesInVisionRange, int tileInFrontID)
    {
        if (!tilesInVisionRange.ContainsKey(tileInFrontID)) return null;
        TileArrayEntry tileInFront = MapArrayScript.Instance.MapTileArrayDict[tileInFrontID];
        List<TileArrayEntry> overlap = tileInFront.GetAdjacentTAEs().Where(
            x => tilesInVisionRange.ContainsKey(x.taeID)).ToList();
        List<TileArrayEntry> TAEsBehind = overlap.Where(
            x => tilesInVisionRange[x.taeID] > tilesInVisionRange[tileInFrontID]).ToList();
        List<int> returnIDs = new List<int>();
        foreach(TileArrayEntry tae in TAEsBehind) returnIDs.Add(tae.taeID);
        return returnIDs;
    }
    private List<TileArrayEntry>[] getVisibleTiles(LocatableObject locatable, int visionRange = 5)
    {
        if (locatable.GetLocatableLocationTAE() == null)
            throw new System.Exception(
                $"Locatable {locatable.locatableID} GetLocatableLocationTAE() == null");
        return getVisibleTiles(locatable.GetLocatableLocationTAE(), visionRange);
    }
    private List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange = 5)
    {
        return getVisibleTiles(vantagePoint, visionRange, false, -2);
    }
    /// <summary>
    /// This override is for working out the ExplorationPotential of a tile, so it ignores scenery
    /// on tiles that aren't already Explored or Visible.
    /// </summary>
    /// <param name="vantagePoint"></param>
    /// <param name="visionRange"></param>
    /// <param name="playerID"></param>
    /// <returns></returns>
    public List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange, int playerID)
    {
        return getVisibleTiles(vantagePoint, visionRange, true, playerID);
    }
    private List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange, 
        bool ignoreHiddenScenery, int playerID)
    {
        // key is the taeID, value is the range to tile
        Dictionary<int, int> tilesInVisionRange = new();
        List<int> nextTiles = new();
        tilesInVisionRange.Add(vantagePoint.taeID, 0);

        // Get tiles in range with distances
        for (int i = 0; i < visionRange; i++)
        {
            // Debug.Log($"Getting tiles in vision range {i+1}");
            nextTiles.Clear();
            foreach (int taeID in tilesInVisionRange.Keys)
            { 
                List<TileArrayEntry> uncheckedTiles 
                    = MapArrayScript.Instance.MapTileArrayDict[taeID].GetAdjacentTAEs().Where(
                        x => !tilesInVisionRange.Keys.Contains(x.taeID) && !nextTiles.Contains(x.taeID)
                        ).ToList();
                foreach (TileArrayEntry tae in uncheckedTiles)
                { 
                    nextTiles.Add(tae.taeID); 
                }
            }
            // Debug.Log("at range i = " + i + ", nextTiles length = " + nextTiles.Count);
            foreach (int taeID in nextTiles) 
                tilesInVisionRange.Add(taeID, i+1);
        }

        Dictionary<int, bool> isObscuredDict = new Dictionary<int, bool>();
        Dictionary<int, bool> showExploredDict = new Dictionary<int, bool>();

        foreach (int taeID in tilesInVisionRange.Keys)
        {
            isObscuredDict[taeID] = false;
            showExploredDict[taeID] = false;
        }
        
        // Using tile utility check bools, set tiles with blockers to be invisible
        foreach (int taeID 
            in tilesInVisionRange.Keys.Where(x => tilesInVisionRange[x] > 0))
        {
            TileArrayEntry tae = MapArrayScript.Instance.MapTileArrayDict[taeID];
            // check for forests, cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae, vantagePoint);
            if ((tae.hasCliffsByDirection[directionTaeToLocatable] || tae.HasForest)
                && !(ignoreHiddenScenery 
                    && tae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
                isObscuredDict[taeID] = true;
        }

        // Cascade invisibility to those behind them
        bool visibilityChanges = true;
        while (visibilityChanges) 
        {
            visibilityChanges = false;
            List<int> obscuredTiles = tilesInVisionRange.Keys.Where(
                x => isObscuredDict[x]).ToList();
            foreach (int taeID in obscuredTiles)
            {
                TileArrayEntry tae = MapArrayScript.Instance.MapTileArrayDict[taeID];
                List<int> visibleNeighboursToObscure = GetNeighboursBehind(
                    tilesInVisionRange, taeID).Where(x => !isObscuredDict[x]).ToList();
                if (visibleNeighboursToObscure.Count > 0) 
                {
                    visibilityChanges = true;
                    foreach (int neighID in visibleNeighboursToObscure)
                        isObscuredDict[neighID] = true;
                }
            }
        }

        // Cascade visibility once from visible tiles
        List<int> unobscuredTiles = tilesInVisionRange.Keys.Where(
                x => !isObscuredDict[x]).ToList();
        foreach (int taeID in unobscuredTiles)
        {
            List<int> obscuredNeighboursToReveal = GetNeighboursBehind(
                tilesInVisionRange, taeID).Where(x => isObscuredDict[x]).ToList();
            if (obscuredNeighboursToReveal.Count > 0)
                foreach (int neighID in obscuredNeighboursToReveal)
                    isObscuredDict[neighID] = false;
        }

        // Hide visible tiles immediately behind nonadjacent cliffs down
        foreach (int taeID
            in tilesInVisionRange.Keys.Where(x => tilesInVisionRange[x] > 1 && !isObscuredDict[x]))
        {
            TileArrayEntry tae = MapArrayScript.Instance.MapTileArrayDict[taeID];
            // check for cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae,
                vantagePoint);
            try
            {
                TileArrayEntry neighbourTae 
                    = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                    tae.AdjacentTileLocsByDirection[directionTaeToLocatable]);
                if (neighbourTae.hasCliffsByDirection[HexOrientation.Opposite(directionTaeToLocatable)])
                    isObscuredDict[taeID] = true;
            }
            catch { /*nothing to throw here, this is just what happens when you're on the map edge*/ }
        }

        // Switch adjacent tiles behind cliffs to explored instead of visible
        foreach (int taeID
            in tilesInVisionRange.Keys.Where(x => tilesInVisionRange[x] == 1 && !isObscuredDict[x]))
        {
            TileArrayEntry tae = MapArrayScript.Instance.MapTileArrayDict[taeID];
            // check for cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae,
                vantagePoint);
            if (tae.hasCliffsByDirection[directionTaeToLocatable]
                && !(ignoreHiddenScenery
                    && tae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
            {
                isObscuredDict[taeID] = true;
                showExploredDict[taeID] = true;
            }
        }

        // Switch non-adjacent forests to explored instead of visible
        foreach (int taeID
            in tilesInVisionRange.Keys.Where(x => tilesInVisionRange[x] > 1 && !isObscuredDict[x]))
        {
            TileArrayEntry tae = MapArrayScript.Instance.MapTileArrayDict[taeID];
            if (tae.HasForest && 
                !(ignoreHiddenScenery && tae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
            {
                isObscuredDict[taeID] = true;
                showExploredDict[taeID] = true;
            }
        }

        List<int> visibleTileIDs = tilesInVisionRange.Keys.Where( x => !isObscuredDict[x]).ToList();
        List<TileArrayEntry> visibleTiles = new();
        foreach (int taeID in visibleTileIDs)
            visibleTiles.Add(MapArrayScript.Instance.MapTileArrayDict[taeID]);

        List<int> exploredTileIDs = tilesInVisionRange.Keys.Where(x => showExploredDict[x]).ToList();
        List<TileArrayEntry> exploredTiles = new();
        foreach (int taeID in exploredTileIDs)
            exploredTiles.Add(MapArrayScript.Instance.MapTileArrayDict[taeID]);

        return new List<TileArrayEntry>[] { visibleTiles, exploredTiles };
    }
    public void UpdateUnitVision()
    {
        foreach (PlayerProperties player in PlayerProperties.playersById.Values)
        {
            // turn all Visible tiles' visibility to Hidden
            foreach (TileArrayEntry tae in mapArrayScript.MapTileArray)
            {
                if (tae.GetVisibilityByPlayerID(player.playerID) == TileVisibility.Visible)
                {
                    tae.SetVisibilityByPlayerID(player.playerID, TileVisibility.Explored);
                }
            }

            // for each of the player's units, mark tiles they can see Visible
            foreach (int locatableId in player.ownedObjectIds)
            {
                LocatableObject locatable = LocatableObject.locatableObjectsById[locatableId];
                if (locatable == null) throw new System.Exception($"No locatable with id {locatableId}");
                // get tiles within 3
                List<TileArrayEntry>[] visibleTiles = getVisibleTiles(locatable);
                Debug.Log($"Unit {locatableId} can see {visibleTiles[0].Count} tiles");
                // set them to Visible
                foreach (TileArrayEntry tae in visibleTiles[0]) 
                    tae.SetVisibilityByPlayerID(player.playerID, TileVisibility.Visible);
                // set not-quite-visible tiles to Explored
                foreach (TileArrayEntry tae in visibleTiles[1])
                    if (tae.GetVisibilityByPlayerID(player.playerID) == TileVisibility.Hidden)
                        tae.SetVisibilityByPlayerID(player.playerID, TileVisibility.Explored);
            }
        }
        // update all tiles' visibility graphics
        foreach (TileArrayEntry tae in mapArrayScript.MapTileArray) 
            tae.SetTileVisibilityGraphic(PlayerProperties.humanPlayerID);
    }
}
