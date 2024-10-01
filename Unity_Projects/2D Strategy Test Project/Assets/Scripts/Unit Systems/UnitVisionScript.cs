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
    private List<TileArrayEntry> GetNeighboursBehind(
        Dictionary<TileArrayEntry, int> tilesInVisionRange, TileArrayEntry tileInFront)
    {
        if (!tilesInVisionRange.ContainsKey(tileInFront)) return null;
        List<TileArrayEntry> overlap = tileInFront.GetAdjacentTAEs().Where(
            x => tilesInVisionRange.ContainsKey(x)).ToList();
        return overlap.Where(
            x => tilesInVisionRange[x] > tilesInVisionRange[tileInFront]).ToList();
    }
    private List<TileArrayEntry>[] getVisibleTiles(LocatableObject locatable, int visionRange = 5)
    {
        return getVisibleTiles(locatable.GetLocatableLocationTAE(), visionRange);
    }
    private List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange = 5)
    {
        return getVisibleTiles(vantagePoint, visionRange, false, -2);
    }
    public List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange, int playerID)
    {
        return getVisibleTiles(vantagePoint, visionRange, true, playerID);
    }
    private List<TileArrayEntry>[] getVisibleTiles(
        TileArrayEntry vantagePoint, int visionRange, 
        bool ignoreHiddenScenery, int playerID)
    {
        Dictionary<TileArrayEntry, int> tilesInVisionRange = new Dictionary<TileArrayEntry, int>();
        List<TileArrayEntry> nextTiles = new List<TileArrayEntry>();
        tilesInVisionRange.Add(vantagePoint, 0);

        // Get tiles in range with distances
        for (int i = 0; i < visionRange; i++)
        {
            nextTiles.Clear();
            foreach (TileArrayEntry tae in tilesInVisionRange.Keys) 
                nextTiles.AddRange(
                    tae.GetAdjacentTAEs().Where(
                        x => !tilesInVisionRange.Keys.Contains(x) && !nextTiles.Contains(x)));
            // Debug.Log("at range i = " + i + ", nextTiles length = " + nextTiles.Count);
            foreach (TileArrayEntry tae in nextTiles) tilesInVisionRange.Add(tae, i+1);
        }

        Dictionary<int, bool> isObscuredDict = new Dictionary<int, bool>();
        Dictionary<int, bool> showExploredDict = new Dictionary<int, bool>();

        foreach (TileArrayEntry tae in tilesInVisionRange.Keys)
        {
            isObscuredDict[tae.taeID] = false;
            showExploredDict[tae.taeID] = false;
        }
        
        // Using tile utility check bools, set tiles with blockers to be invisible
        foreach (TileArrayEntry tae 
            in tilesInVisionRange.Keys.Where(x => tilesInVisionRange[x] > 0))
        {
            // check for cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae,
                vantagePoint);
            if (tae.hasCliffsByDirection[directionTaeToLocatable] 
                && !(ignoreHiddenScenery 
                    && tae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
                isObscuredDict[tae.taeID] = true;
        }

        // Cascade invisibility to those behind them
        bool visibilityChanges = true;
        while (visibilityChanges) 
        {
            visibilityChanges = false;
            List<TileArrayEntry> obscuredTiles = tilesInVisionRange.Keys.Where(
                x => isObscuredDict[x.taeID]).ToList();
            foreach (TileArrayEntry tae in obscuredTiles)
            {
                List<TileArrayEntry> visibleNeighboursToObscure = GetNeighboursBehind(
                    tilesInVisionRange, tae).Where(
                    x => !isObscuredDict[x.taeID]).ToList();
                if (visibleNeighboursToObscure.Count > 0) 
                {
                    visibilityChanges = true;
                    foreach (TileArrayEntry neigh in visibleNeighboursToObscure)
                        isObscuredDict[neigh.taeID] = true;
                }
            }
        }

        // Cascade visibility once from visible tiles
        List<TileArrayEntry> unobscuredTiles = tilesInVisionRange.Keys.Where(
                x => !isObscuredDict[x.taeID]).ToList();
        foreach (TileArrayEntry tae in unobscuredTiles)
        {
            List<TileArrayEntry> obscuredNeighboursToReveal = GetNeighboursBehind(
                tilesInVisionRange, tae).Where(
                x => isObscuredDict[x.taeID]).ToList();
            if (obscuredNeighboursToReveal.Count > 0)
                foreach (TileArrayEntry neigh in obscuredNeighboursToReveal)
                    isObscuredDict[neigh.taeID] = false;
        }

        // Hide visible tiles immediately behind nonadjacent cliffs down
        foreach (TileArrayEntry tae
            in tilesInVisionRange.Keys.Where(
                x => tilesInVisionRange[x] > 1 && !isObscuredDict[x.taeID]))
        {
            // check for cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae,
                vantagePoint);
            try
            {
                TileArrayEntry neighbourTae 
                    = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                    tae.AdjacentTileLocsByDirection[directionTaeToLocatable]);
                if (neighbourTae.hasCliffsByDirection[
                    HexOrientation.Opposite(directionTaeToLocatable)]
                    && !(ignoreHiddenScenery
                    && neighbourTae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
                    isObscuredDict[tae.taeID] = true;
            }
            catch { }
        }

        // Switch adjacent tiles behind cliffs to explored instead of visible
        foreach (TileArrayEntry tae
            in tilesInVisionRange.Keys.Where(
                x => tilesInVisionRange[x] == 1 && !isObscuredDict[x.taeID]))
        {
            // check for cliffs pointing at locatable's tile
            HexDir directionTaeToLocatable = TileFinders.Instance.GetTileHexDirToTile(tae,
                vantagePoint);
            if (tae.hasCliffsByDirection[directionTaeToLocatable]
                && !(ignoreHiddenScenery
                    && tae.GetVisibilityByPlayerID(playerID) == TileVisibility.Hidden))
            {
                isObscuredDict[tae.taeID] = true;
                showExploredDict[tae.taeID] = true;
            }
        }

        List<TileArrayEntry> visibleTiles
            = tilesInVisionRange.Keys.Where(
                x => !isObscuredDict[x.taeID]).ToList();
        List<TileArrayEntry> exploredTiles
            = tilesInVisionRange.Keys.Where(
                x => showExploredDict[x.taeID]).ToList();

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
                // get tiles within 3
                List<TileArrayEntry>[] visibleTiles = getVisibleTiles(locatable);
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
