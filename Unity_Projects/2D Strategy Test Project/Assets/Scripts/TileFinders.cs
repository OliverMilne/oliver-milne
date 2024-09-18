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
            int pathDistance = UnitMovement.AStarPathCalculatorMultithreaded(
                originTAE, targetTile, visibilityPlayerID, accountForVisibility).Count;
            if (pathDistance < minDistance) { destination = targetTile; minDistance = pathDistance; }
        }
        return destination;
    }
    public Dictionary<TileArrayEntry, int> GetNearestXTilesAndDistancesWithCondition(
        TileArrayEntry originTAE, int numberRequired, Predicate<TileArrayEntry> eligibilityCondition)
    {
        string uniqueCheckKey = "GetNearestXTilesAndDistancesWithCondition";
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.AddUtilityCheckBoolDictEntry(uniqueCheckKey, false);
        }

        Dictionary<TileArrayEntry, int> returnDict = new Dictionary<TileArrayEntry, int>();

        // iterate out by ranks until you've got enough TAEs & distances in your dictionary to send off
        List<List<TileArrayEntry>> ranksList = new List<List<TileArrayEntry>>();
        ranksList.Add(new List<TileArrayEntry> { originTAE });
        originTAE.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);

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
                    t.GetAdjacentTAEs().Where(x => !x.GetUtilityCheckBoolDictEntry(uniqueCheckKey)).ToList())
                {
                    thisRank.Add(adj);
                    if (eligibilityCondition(adj))
                    {
                        returnDict[adj] = rankCounter;
                        foundCount++;
                        if (foundCount == numberRequired) break;
                    }
                    adj.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
                }
                if (foundCount == numberRequired) break;
            }
            if (thisRank.Count == 0 || foundCount == numberRequired) break;
            ranksList.Add(thisRank);
        }

        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.RemoveUtilityCheckBoolDictEntry(uniqueCheckKey);
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
        string uniqueCheckKey = "GetTileDistanceToTiles";
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.AddUtilityCheckBoolDictEntry(uniqueCheckKey, false);
        }

        Dictionary<int, int> outputDictionary = new Dictionary<int, int>();
        List<List<TileArrayEntry>> ranksList = new List<List<TileArrayEntry>>();
        ranksList.Add(new List<TileArrayEntry> { tae });
        if (taeIDs.Contains(tae.taeID)) outputDictionary[tae.taeID] = 0;
        tae.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);

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
                    t.GetAdjacentTAEs().Where(x => !x.GetUtilityCheckBoolDictEntry(uniqueCheckKey)).ToList())
                {
                    thisRank.Add(adj);
                    if (taeIDs.Contains(adj.taeID)) outputDictionary[adj.taeID] = rankCounter;
                    adj.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
                }
            }
            if (thisRank.Count == 0) break;
            ranksList.Add(thisRank);
        }

        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.RemoveUtilityCheckBoolDictEntry(uniqueCheckKey);
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
        string uniqueCheckKey = $"GetTilesWithinDistance {originTAE.taeID} {distance}";
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.AddUtilityCheckBoolDictEntry(uniqueCheckKey, false);
        }

        List<TileArrayEntry> tilesWithinDistance = new List<TileArrayEntry>();
        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        tilesWithinDistance.Add(originTAE);
        originTAE.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
        for (int i = 0; i < distance; i++)
        {
            foreach (TileArrayEntry t in tilesWithinDistance)
            {
                tilesToAdd.AddRange(
                    t.GetAdjacentTAEs().Where(x => !x.GetUtilityCheckBoolDictEntry(uniqueCheckKey)));
                foreach (TileArrayEntry t2 in tilesToAdd) 
                    t2.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
            }
            tilesWithinDistance.AddRange(tilesToAdd);
            tilesToAdd.Clear();
        }
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.RemoveUtilityCheckBoolDictEntry(uniqueCheckKey);
        }
        return tilesWithinDistance;
    }
}
