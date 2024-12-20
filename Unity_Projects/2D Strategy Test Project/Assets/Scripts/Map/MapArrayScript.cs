using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading;

public class MapArrayScript : MonoBehaviour
{
    // pseudo-singleton apparatus
    public static MapArrayScript Instance { get => instance; }
    private static MapArrayScript instance;

    // stuff that doesn't get saved to CurrentGameState.GameStateInfo
    public Tilemap tilemap;
    public Tilemap fowTilemap;

    public TileBase genericTile;
    public TileBase impassableTile;
    public TileBase fowExploredTile;
    public TileBase fowHiddenTile;
    public TileBase fowVisibleTile;
    public Dictionary<string, TileBase> tileBaseDict = new Dictionary<string, TileBase>();

    public int mapXSize
    {
        get => CurrentGameState.Instance.gameStateData.mapData.mapXSize;
        set { CurrentGameState.Instance.gameStateData.mapData.mapXSize = value; }
    }
    public int mapYSize
    {
        get => CurrentGameState.Instance.gameStateData.mapData.mapYSize;
        set { CurrentGameState.Instance.gameStateData.mapData.mapYSize = value; }
    }
    public int mapXOffset
    {
        get => CurrentGameState.Instance.gameStateData.mapData.mapXOffset;
        set { CurrentGameState.Instance.gameStateData.mapData.mapXOffset = value; }
    }
    public int mapYOffset
    {
        get => CurrentGameState.Instance.gameStateData.mapData.mapYOffset;
        set { CurrentGameState.Instance.gameStateData.mapData.mapYOffset = value; }
    }

    private bool _fowOn
    {
        get => CurrentGameState.Instance.gameStateData.mapData.fowOn;
        set { CurrentGameState.Instance.gameStateData.mapData.fowOn = value; }
    }
    public TileArrayEntry[,] MapTileArray;
    public Dictionary<int, TileArrayEntry> MapTileArrayDict;
    public Dictionary<int, TileData> MapTileDataDict
    {
        get => CurrentGameState.Instance.gameStateData.mapData.MapTileDataDict;
        set { CurrentGameState.Instance.gameStateData.mapData.MapTileDataDict = value; }
    }

    private void Awake()
    {
        if (this.name == "ScriptBucket") instance = this;
        CurrentGameState.Instance.gameStateData.mapData = new MapData();
    }
    public void MapArrayScript_Initialise()
    {
        mapXSize = 20;
        mapYSize = 20;

        mapXOffset = mapXSize / 2;
        mapYOffset = mapYSize / 2;

        fowVisibleTile = ScriptableObject.CreateInstance<Tile>(); // this just has to have no graphics
        tileBaseDict["fowVisibleTile"] = fowVisibleTile;
        tileBaseDict["genericTile"] = genericTile;
        tileBaseDict["impassableTile"] = impassableTile;
        tileBaseDict["fowExploredTile"] = fowExploredTile;
        tileBaseDict["fowHiddenTile"] = fowHiddenTile;

        NewBlankMap(mapXSize, mapYSize, mapXOffset, mapYOffset);
        BasicMapGen();
    }
    public void MapArrayScript_InitialiseFromGameStateInfo()
    {
        LoadMapFromGameStateInfo();
    }
    private void AssignCliffsHeightDifferences(float heightRequirement)
    {
        // for each tile, find neighbours significantly lower than it, and put cliffs on those directions
        foreach (TileArrayEntry tae in MapTileArray)
        {
            foreach(HexDir dir in tae.AdjacentTileLocsByDirection.Keys)
            {
                try
                {
                    if (tae.terrainHeight > heightRequirement
                        + TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                            tae.AdjacentTileLocsByDirection[dir]).terrainHeight)
                    {
                        SceneryManager.Instance.AddCliffs(tae,dir);
                    }
                }
                catch { }
            }
        }
    }
    private void AssignCliffsHeightDifferenceSnakes(float spawnChance, float continueChance)
    {
        // as per RandomSnakes except the starting cliff direction must be downhill
        // and the choice which way to turn is based on which of the two tiles facing the corner is lower

        List<TileArrayEntry> snakingTiles = new List<TileArrayEntry>();
        List<TileArrayEntry> nextTiles = new List<TileArrayEntry>();
        // place initial cliff seeds
        foreach (TileArrayEntry tae in MapTileArray)
        {
            // randomise direction
            if (tae.isPassable && UnityEngine.Random.value < spawnChance) 
            {
                // find a downhill direction
                foreach (HexDir dir in HexDir.GetValues(typeof(HexDir))) // might want to randomise this order
                {
                    // see if the tile in this direction is downhill
                    try
                    {
                        if (tae.terrainHeight > TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                                tae.AdjacentTileLocsByDirection[dir]).terrainHeight)
                        {
                            SceneryManager.Instance.AddCliffs(tae, dir);
                            snakingTiles.Add(tae);
                            break;
                        }
                    }
                    catch { }
                }
            }
        }
        int loopBreaker = 0;
        while (snakingTiles.Count > 0 && loopBreaker < 2000)
        {
            loopBreaker++;
            nextTiles.Clear();
            foreach (TileArrayEntry tae in snakingTiles)
            {
                // find the next clockwise cliff end from west on tae
                HexDir cliffEnd = HexDir.W;
                bool foundCliffEnd = false;
                foreach (HexDir key in tae.hasCliffsByDirection.Keys)
                {
                    if (tae.hasCliffsByDirection[key]
                        && !tae.hasCliffsByDirection[HexOrientation.Anticlockwise1(key)])
                    {
                        cliffEnd = key;
                        foundCliffEnd = true;
                        break;
                    }
                }
                if (!foundCliffEnd) continue;
                // then have a chance to add a cliff leading from it, either on this tile, or on the adjacent
                if (UnityEngine.Random.value > continueChance) continue;
                // pick next tile
                bool toAdjacentTile = false;
                try
                {
                    if (TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                        tae.AdjacentTileLocsByDirection[cliffEnd]).terrainHeight
                        < TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                            tae.AdjacentTileLocsByDirection[
                                HexOrientation.Anticlockwise1(cliffEnd)]).terrainHeight)
                        toAdjacentTile = true;
                }
                catch { }
                if (toAdjacentTile)
                {
                    try
                    {
                        TileArrayEntry adjacentTAE =
                            TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                                tae.AdjacentTileLocsByDirection[HexOrientation.Anticlockwise1(cliffEnd)]);
                        if (adjacentTAE.isPassable)
                        {
                            SceneryManager.Instance.AddCliffs(
                                adjacentTAE, HexOrientation.Clockwise1(cliffEnd));
                            nextTiles.Add(adjacentTAE);
                        }
                    }
                    catch { }
                }
                else
                {
                    SceneryManager.Instance.AddCliffs(tae, HexOrientation.Anticlockwise1(cliffEnd));
                    nextTiles.Add(tae);
                }
            }
            snakingTiles.Clear();
            snakingTiles.AddRange(nextTiles);
        }
    }
    private void AssignCliffsRandomSnakes()
    {
        List<TileArrayEntry> snakingTiles = new List<TileArrayEntry>();
        List<TileArrayEntry> nextTiles = new List<TileArrayEntry>();
        // place initial cliff seeds
        foreach (TileArrayEntry tae in MapTileArray)
        {
            // randomise direction
            HexDir cliffDir = HexOrientation.RandomDir();
            if (tae.isPassable && UnityEngine.Random.value < 0.02)
            {
                SceneryManager.Instance.AddCliffs(tae, cliffDir);
                snakingTiles.Add(tae);
            }
        }
        int loopBreaker = 0;
        while (snakingTiles.Count > 0 && loopBreaker<2000)
        {
            loopBreaker++;
            nextTiles.Clear();
            foreach (TileArrayEntry tae in snakingTiles)
            {
                // find the next clockwise cliff end from west on tae
                HexDir cliffEnd = HexDir.W;
                bool foundCliffEnd = false;
                foreach (HexDir key in tae.hasCliffsByDirection.Keys)
                {
                    if (tae.hasCliffsByDirection[key] 
                        && !tae.hasCliffsByDirection[HexOrientation.Anticlockwise1(key)]) 
                    { 
                        cliffEnd = key; 
                        foundCliffEnd = true;
                        break; 
                    }
                }
                if (!foundCliffEnd) continue;
                // then have a chance to add a cliff leading from it, either on this tile, or on the adjacent
                if (UnityEngine.Random.value > 0.8) continue;
                // pick next tile
                bool toAdjacentTile = false;
                if (UnityEngine.Random.value > 0.5) toAdjacentTile = true;
                if (toAdjacentTile) 
                {
                    try
                    {
                        TileArrayEntry adjacentTAE =
                            TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                                tae.AdjacentTileLocsByDirection[HexOrientation.Anticlockwise1(cliffEnd)]);
                        if (adjacentTAE.isPassable)
                        {
                            SceneryManager.Instance.AddCliffs(adjacentTAE, HexOrientation.Clockwise1(cliffEnd));
                            nextTiles.Add(adjacentTAE);
                        }
                    } catch { }
                }
                else
                {
                    SceneryManager.Instance.AddCliffs(tae, HexOrientation.Anticlockwise1(cliffEnd));
                    nextTiles.Add(tae);
                }
            }
            snakingTiles.Clear();
            snakingTiles.AddRange(nextTiles);
        }
    }
    private void AssignForests(int iterations, float seedSpawnChance, float spreadChance, 
        float additionalSpread = 0.5f)
    {
        List<int> potentiallyForested = new();

        // randomly place some single forest tiles
        foreach (TileArrayEntry tae in MapTileArray)
        {
            if (UnityEngine.Random.value < seedSpawnChance && tae.isPassable)
            {
                SceneryManager.Instance.AddForest(tae);
                potentiallyForested.Add(tae.taeID);
            }
        }

        // add forest at random around those spawn points
        for (int i = 0; i < iterations; i++) 
        {
            List<int> treeRing = new();
            foreach(int taeID in potentiallyForested)
            {
                foreach (TileArrayEntry adj 
                    in TileFinders.Instance.GetTileArrayEntryByID(taeID).GetAdjacentTAEs())
                    if (!potentiallyForested.Contains(adj.taeID) && adj.isPassable)
                        treeRing.Add(adj.taeID);
            }
            foreach (int taeID in treeRing)
            {
                if (UnityEngine.Random.value < spreadChance)
                {
                    SceneryManager.Instance.AddForest(
                        TileFinders.Instance.GetTileArrayEntryByID(taeID)); 
                    potentiallyForested.Add(taeID);
                }
                // this should give it some sparsity
                else if (UnityEngine.Random.value < additionalSpread)
                    potentiallyForested.Add(taeID);
            }
        }
    }
    private void AssignImpassablesRandomBlobs(int iterations, float spawnChance)
    {
        // randomly place some single impassable tiles
        foreach (TileArrayEntry tae in MapTileArray)
        {
            if (UnityEngine.Random.value < spawnChance)
            { 
                // at some point I'm going to have to systematise my tileset stuff so it can just
                // take in a wodge of tile types and process them automatically
                MakeImpassable(tae);
                // tae.RectifyTile();
            }
        }

        // give them impassable neighbours at a certain probability
        for (int i = 0; i < iterations; i++)
        {
            foreach (TileArrayEntry tae in MapTileArray)
            {
                List<TileArrayEntry> adjacents = tae.GetAdjacentTAEs();
                bool hasAdjacentImpassables = false;
                foreach (TileArrayEntry adjacent in adjacents)
                {
                    if (!adjacent.isPassable) hasAdjacentImpassables = true;
                }
                if (hasAdjacentImpassables && UnityEngine.Random.value < 0.2)
                {
                    MakeImpassable(tae);
                    // tae.RectifyTile();
                }
            }
        }
    }
    private void AssignTileHeights_PeakStepdownMethod()
    {
        // This is meant to create a landscape of mountains and valleys.
        // Pick some peaks and make 'em tall (should be less likely with water borders)
        Dictionary<int, bool> assignedHeightsDict = new Dictionary<int, bool>();
        Dictionary<int, bool> isPeakDict = new Dictionary<int, bool>();
        foreach (TileArrayEntry t in MapTileArray)
        {
            assignedHeightsDict[t.taeID] = false;
            isPeakDict[t.taeID] = false;
        }

        // make some peaks
        foreach (TileArrayEntry tae in MapTileArray)
        {
            if (UnityEngine.Random.value < 0.05 
                && tae.isPassable && !assignedHeightsDict[tae.taeID])
            {
                tae.terrainHeight = UnityEngine.Random.Range(5, 10);
                isPeakDict[tae.taeID] = true;
                // get tiles within 3 and turn utilityCheckBoolDict[uniqueCheckKey] to true
                List<TileArrayEntry> nearbyTiles = TileFinders.Instance.GetTilesWithinDistance(tae, 3);
                foreach (TileArrayEntry tileArrayEntry in nearbyTiles.Where(x => x.taeID != tae.taeID))
                {
                    assignedHeightsDict[tileArrayEntry.taeID] = true;
                }
            }
        }
        // Step down to zero over a random number of steps per peak
        foreach (TileArrayEntry tae in MapTileArray)
        {
            if (tae.isPassable && !isPeakDict[tae.taeID])
            {
                // Get distance to nearest water tile
                Dictionary<TileArrayEntry, int> waterTiles =
                    TileFinders.Instance.GetNearestXTilesAndDistancesWithCondition(tae, 1, x => !x.isPassable);
                if (waterTiles.Count == 0) break;
                int waterTileDistance = waterTiles.First().Value;

                // Get distances to, heights of nearest two peaks
                Dictionary<TileArrayEntry, int> nearestPeaks =
                    TileFinders.Instance.GetNearestXTilesAndDistancesWithCondition(
                        tae, 2, x => isPeakDict[x.taeID]);
                if (nearestPeaks.Count < 2) break;
                float peak0Height = nearestPeaks.First().Key.terrainHeight;
                float peak1Height = nearestPeaks.Last().Key.terrainHeight;
                int peak0Distance = nearestPeaks.First().Value;
                int peak1Distance = nearestPeaks.Last().Value;

                float averageDistanceToCoast = (peak0Distance + peak1Distance + waterTileDistance) * 0.5f;
                float weightedAvgPeakHeight = (peak0Height * (peak1Distance)
                    + peak1Height * (peak0Distance))
                    / (peak0Distance + peak1Distance);
                float coastDistanceMultiplier = waterTileDistance / averageDistanceToCoast;

                tae.terrainHeight = (weightedAvgPeakHeight * coastDistanceMultiplier);
            }
        }
    }
    private void BasicMapGen()
    {
        if (MapTileArray == null) throw new System.Exception("No map array entries!");

        AssignImpassablesRandomBlobs(4, 0.045f);
        AssignTileHeights_PeakStepdownMethod();
        // place some cliffs
        AssignCliffsHeightDifferenceSnakes(0.03f, 0.9f);
        AssignForests(4, 0.025f, 0.5f, 0.25f);

        foreach (TileArrayEntry tae in MapTileArray)
        {
            tae.RectifyTileGraphics();
        }
    }
    /// <summary>
    /// Returns dictionary of taeIDs to assigned passable area IDs, which are generated afresh on
    /// calling this method; the out parameter yields a dictionary of these same area IDs and their
    /// sizes, not including the impassable area.
    /// </summary>
    /// <param name="areaSizes"></param>
    /// <returns></returns>
    public Dictionary<int,int> GetContiguousPassableAreas(out Dictionary<int,int> areaSizes)
    {
        Dictionary<int, int> outputDict = new();
        Dictionary<int, bool> checkDict = new();
        areaSizes = new();
        foreach (int i in MapTileArrayDict.Keys) checkDict[i] = false;

        IDDispenser areaIDDispenser = new();
        foreach (int i in MapTileArrayDict.Keys)
        {
            if (!MapTileArrayDict[i].isPassable) 
            { 
                outputDict[i] = -1;
                continue;
            }
            if (checkDict[i]) continue;

            int areaID = areaIDDispenser.DispenseID();
            areaSizes[areaID] = 0;
            List<int> openTiles = new() { i };
            while (openTiles.Count > 0) 
            {
                List<int> nextRound = new();
                foreach(int j in openTiles)
                {
                    outputDict[j] = areaID;
                    areaSizes[areaID]++;
                    checkDict[j] = true;
                    List<TileArrayEntry> openNeighbours = MapTileArrayDict[j].GetAccessibleTAEs()
                        .Where(x => !checkDict[x.taeID]).ToList();
                    foreach (TileArrayEntry tae in openNeighbours) 
                        if (!nextRound.Contains(tae.taeID)) 
                            nextRound.Add(tae.taeID);
                }
                openTiles = nextRound;
            }
        }
        return outputDict;
    }
    public bool IsVector3IntATileLocOnTheMap(Vector3Int loc)
    {
        try
        {
            TileFinders.Instance.GetTileArrayEntryAtLocationQuick(loc);
            return true;
        }
        catch { return false; }
    }
    private void LoadMapFromGameStateInfo()
    {
        Debug.Log("Clearing all tiles");
        tilemap.ClearAllTiles();
        float upperBound = 0;
        float lowerBound = 0;
        float leftBound = 0;
        float rightBound = 0;

        Debug.Log("Initialising new MapTileArray");
        // before disposing the semaphores, need to put a stop to all calculations going on
        // using them in parallel threads
        while (AsyncThreadsManager.AreAsyncThreadsStillRunning()) Thread.Sleep(10);
        Debug.Log("Waited for threads to stop successfully");
        MapTileArray = new TileArrayEntry[
            CurrentGameState.Instance.gameStateData.mapData.mapXSize,
            CurrentGameState.Instance.gameStateData.mapData.mapYSize];

        int xOffset = CurrentGameState.Instance.gameStateData.mapData.mapXOffset;
        int yOffset = CurrentGameState.Instance.gameStateData.mapData.mapYOffset;

        Debug.Log("Creating TAEs to match saved TileDatas");
        // create TileArrayEntries to match the TileData in the save file
        foreach (var entry in CurrentGameState.Instance.gameStateData.mapData.MapTileDataDict)
        {
            MapTileArray[
                entry.Value.tileActualLoc[0] - xOffset,
                entry.Value.tileActualLoc[1] - yOffset] = new TileArrayEntry(entry.Key);
        }

        Debug.Log("Doing graphical stuff");
        // do tilemap and camera stuff
        foreach (TileArrayEntry tae in MapTileArray)
        {
            // Debug.Log ("Rectifying tile " + tae.taeID);
            // set appropriate tile
            tae.RectifyTileGraphics();

            // set the map bounds for the camera
            Vector3 tileWorldPosition = tae.GetTileWorldLocation();
            if (tileWorldPosition.y > upperBound) upperBound = tileWorldPosition.y;
            if (tileWorldPosition.y < lowerBound) lowerBound = tileWorldPosition.y;
            if (tileWorldPosition.x < leftBound) leftBound = tileWorldPosition.x;
            if (tileWorldPosition.x > rightBound) rightBound = tileWorldPosition.x;
        }
        GameObject.Find("Main Camera").GetComponent<CameraMovementScript>().SetCameraPosition(
            CurrentGameState.Instance.gameStateData.cameraMovementData.cameraPosition[0],
            CurrentGameState.Instance.gameStateData.cameraMovementData.cameraPosition[1]);
    }
    private void MakeImpassable(TileArrayEntry tae)
    {
        // in future will evolve (& rename) this to assign various tile types
        tae.tileBaseKey = "impassableTile";
        tae.isPassable = false;
    }
    private void NewBlankMap(int xSize, int ySize, int xOffset, int yOffset)
    {
        tilemap.ClearAllTiles();
        // Debug.Log("Tiles cleared");
        MapTileArrayDict = new Dictionary<int, TileArrayEntry>();

        // create an x by y tile map whose bottom left corner is at (xOffset, yOffset)
        // and which is all generic tiles
        // can then overwrite this for actual map generation

        float upperBound = 0;
        float lowerBound = 0;
        float leftBound = 0;
        float rightBound = 0;

        MapTileArray = new TileArrayEntry[xSize, ySize];
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                Vector3Int tilePosition = new Vector3Int(xOffset + i, yOffset + j, 0);
                // Debug.Log("tilePosition set at " + tilePosition);

                tilemap.SetTile(tilePosition, genericTile);
                // Debug.Log("Tile set at " + tilePosition);

                MapTileArray[i, j] = new TileArrayEntry(
                    tilePosition,
                    "genericTile"
                    );
                // Debug.Log("Entry made for " + tilePosition);
                MapTileArrayDict[MapTileArray[i, j].taeID] = MapTileArray[i, j];

                // set the map bounds for the camera
                Vector3 tileWorldPosition = tilemap.CellToWorld(tilePosition);
                if (tileWorldPosition.y > upperBound) upperBound = tileWorldPosition.y;
                if (tileWorldPosition.y < lowerBound) lowerBound = tileWorldPosition.y;
                if (tileWorldPosition.x < leftBound) leftBound = tileWorldPosition.x;
                if (tileWorldPosition.x > rightBound) rightBound = tileWorldPosition.x;
            }
        }
        GameObject.Find("Main Camera").GetComponent<CameraMovementScript>().SetBounds(
            upperBound, lowerBound, leftBound, rightBound);
    }
    public void ToggleFOW()
    {
        if (_fowOn)
        {
            // turn fow off
            _fowOn = false;
            foreach(TileArrayEntry tae in MapTileArray)
            {
                // hide the fow tile
                tae.forceVisible = true;
                tae.SetTileVisibilityGraphic(PlayerProperties.humanPlayerID);
            }
            Debug.Log("FOW Off");
        }
        else
        {
            // turn fow on
            _fowOn = true;
            foreach (TileArrayEntry tae in MapTileArray)
            {
                // hide the fow tile
                tae.forceVisible = false;
                tae.SetTileVisibilityGraphic(PlayerProperties.humanPlayerID);
            }
            Debug.Log("FOW On");
        }
    }
}

public class MapData
{
    public int mapXSize;
    public int mapYSize;
    public int mapXOffset;
    public int mapYOffset;

    public bool fowOn = true;
    public Dictionary<int, TileData> MapTileDataDict = new Dictionary<int, TileData>();
}
