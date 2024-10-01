using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileFinders : MonoBehaviour
{
    public static TileFinders Instance { get => instance; }
    private static TileFinders instance;

    public void Awake()
    {
        if (this.name == "ScriptBucket") instance = this;
    }

    public TileArrayEntry GetClosestTileFromList(TileArrayEntry originTAE, List<TileArrayEntry> listToSearch,
        int visibilityPlayerID, bool accountForVisibility = true)
    {
        TileArrayEntry destination = null;
        int minDistance = int.MaxValue;
        // Pick a closest one
        foreach (TileArrayEntry targetTile in listToSearch)
        {
            int pathDistance = UnitMovement.AStarPathCalculator(
                originTAE, targetTile, visibilityPlayerID, accountForVisibility).Count;
            if (pathDistance < minDistance) { destination = targetTile; minDistance = pathDistance; }
        }
        return destination;
    }
    public Dictionary<TileArrayEntry, int> GetNearestXTilesAndDistancesWithCondition(
        TileArrayEntry originTAE, int numberRequired, Predicate<TileArrayEntry> eligibilityCondition)
    {
        Dictionary<int, bool> checkDict = new Dictionary<int, bool>();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            checkDict.Add(t.taeID, false);
        }

        Dictionary<TileArrayEntry, int> returnDict = new Dictionary<TileArrayEntry, int>();

        // iterate out by ranks until you've got enough TAEs & distances in your dictionary to send off
        List<List<TileArrayEntry>> ranksList = new List<List<TileArrayEntry>>();
        ranksList.Add(new List<TileArrayEntry> { originTAE });
        checkDict[originTAE.taeID] = true;

        int foundCount = 0;
        int rankCounter = 0;
        while (rankCounter < 2000)
        {
            rankCounter++;
            if (rankCounter == 2000)
                Debug.Log("MapArrayScript.GetTileDistanceToTiles: RankCounter hit 2000!");
            List<TileArrayEntry> thisRank = new List<TileArrayEntry>();
            foreach (TileArrayEntry t in ranksList.Last())
            {
                foreach (TileArrayEntry adj in
                    t.GetAdjacentTAEs().Where(x => !checkDict[x.taeID]).ToList())
                {
                    thisRank.Add(adj);
                    if (eligibilityCondition(adj))
                    {
                        returnDict[adj] = rankCounter;
                        foundCount++;
                        if (foundCount == numberRequired) break;
                    }
                    checkDict[adj.taeID] = true;
                }
                if (foundCount == numberRequired) break;
            }
            if (thisRank.Count == 0 || foundCount == numberRequired) break;
            ranksList.Add(thisRank);
        }

        return returnDict;
    }
    /// <summary>
    /// This purposefully throws an error if you try to pass it a location that's not on the map.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public TileArrayEntry GetTileArrayEntryAtLocationQuick(Vector3Int location)
    {
        return MapArrayScript.Instance.MapTileArray[
            location.x - MapArrayScript.Instance.mapXOffset, location.y - MapArrayScript.Instance.mapYOffset];
    }
    public TileArrayEntry GetTileArrayEntryByID(int id)
    {
        try { return MapArrayScript.Instance.MapTileArrayDict[id]; }
        catch { return null; }
    }
    public Dictionary<int, int> GetTileDistanceToTiles(TileArrayEntry tae, List<int> taeIDs)
    {
        Dictionary<int, bool> checkDict = new Dictionary<int, bool>();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            checkDict[t.taeID] = false;
        }

        Dictionary<int, int> outputDictionary = new Dictionary<int, int>();
        List<List<TileArrayEntry>> ranksList = new List<List<TileArrayEntry>>();
        ranksList.Add(new List<TileArrayEntry> { tae });
        if (taeIDs.Contains(tae.taeID)) outputDictionary[tae.taeID] = 0;
        checkDict[tae.taeID] = true;

        int rankCounter = 0;
        while (rankCounter < 2000)
        {
            rankCounter++;
            if (rankCounter == 2000)
                Debug.Log("MapArrayScript.GetTileDistanceToTiles: RankCounter hit 2000!");
            List<TileArrayEntry> thisRank = new List<TileArrayEntry>();
            foreach (TileArrayEntry t in ranksList.Last())
            {
                foreach (TileArrayEntry adj in
                    t.GetAdjacentTAEs().Where(x => !checkDict[x.taeID]).ToList())
                {
                    thisRank.Add(adj);
                    if (taeIDs.Contains(adj.taeID)) outputDictionary[adj.taeID] = rankCounter;
                    checkDict[adj.taeID] = true;
                }
            }
            if (thisRank.Count == 0) break;
            ranksList.Add(thisRank);
        }

        return outputDictionary;
    }
    public HexDir GetTileHexDirToTile(TileArrayEntry origin, TileArrayEntry destination)
    {
        return HexOrientation.HexDirPointAToPointB(
            origin.GetTileWorldLocation(), destination.GetTileWorldLocation());
    }
    public List<TileArrayEntry> GetTilesWithinDistance(TileArrayEntry originTAE, int distance)
    {
        Dictionary<int, bool> checkDict = new Dictionary<int, bool>();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            checkDict[t.taeID] = false;
        }

        List<TileArrayEntry> tilesWithinDistance = new List<TileArrayEntry>();
        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        tilesWithinDistance.Add(originTAE);
        checkDict[originTAE.taeID] = true;
        for (int i = 0; i < distance; i++)
        {
            foreach (TileArrayEntry t in tilesWithinDistance)
            {
                tilesToAdd.AddRange(
                    t.GetAdjacentTAEs().Where(x => !checkDict[x.taeID]));
                foreach (TileArrayEntry t2 in tilesToAdd) 
                    checkDict[t2.taeID] = true;
            }
            tilesWithinDistance.AddRange(tilesToAdd);
            tilesToAdd.Clear();
        }
        return tilesWithinDistance;
    }
}
