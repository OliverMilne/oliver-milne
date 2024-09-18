using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading;

public class TileArrayEntry
{
    // id stuff
    public static int nextTileArrayEntryID = 0;
    public int taeID { get; }

    // tile change tracker stuff
    private static int _tileUpdateNumberBehind = 0;
    /// <summary>
    /// This is for use when reading changeable TAE data asynchronously without getting weird behaviour
    /// due to race conditions. If it's changed, the TAE data has changed while your thread's been
    /// busy, and you should toss out the work it's done with them.
    /// </summary>
    public static int tileUpdateNumber 
    { 
        get => _tileUpdateNumberBehind; 
        private set { _tileUpdateNumberBehind = value; } 
    }

    // info not saved
    public Dictionary<HexDir, int[]> adjacentTileLocsBehind { get; private set; }
    public Dictionary<HexDir, Vector3Int> AdjacentTileLocsByDirection
    {
        get
        {
            Dictionary<HexDir, Vector3Int> returnDir = new Dictionary<HexDir, Vector3Int>();
            foreach (HexDir hd in adjacentTileLocsBehind.Keys)
            {
                returnDir.Add(hd, new Vector3Int(
                    adjacentTileLocsBehind[hd][0],
                    adjacentTileLocsBehind[hd][1],
                    adjacentTileLocsBehind[hd][2]));
            }
            return returnDir;
        }
    }

    // core info
    public string tileBaseKey
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileBaseKey;
        set 
        { 
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileBaseKey = value;
            tileUpdateNumber++;
        }
    }
    public Vector3Int TileLoc 
    {
        get => new Vector3Int(
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[0],
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[1],
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[2]);
        set
        {
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[0] = value.x;
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[1] = value.y;
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileActualLoc[2] = value.z;
        }
    }
    public float terrainHeight
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].terrainHeight;
        set { CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].terrainHeight = value; }
    }
    public bool isPassable
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].isPassable;
        set 
        { 
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].isPassable = value;
            tileUpdateNumber++;
        }
    }
    // utilityCheckBoolDict apparatus
    /// <summary>
    /// You can change these without changing tileUpdateNumber. 
    /// Always use the thread ID in the name.
    /// </summary>
    private Dictionary<string, bool> utilityCheckBoolDict
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].utilityCheckBoolDict;
        set { CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].utilityCheckBoolDict 
                = value; }
    }
    /// <summary>
    /// Always use System.Environment.CurrentManagedThreadId in the string key!
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void AddUtilityCheckBoolDictEntry(string key, bool value)
    {
        utilityCheckBoolDictSemaphore.Wait();
        utilityCheckBoolDict.Add(key, value);
        utilityCheckBoolDictSemaphore.Release();
    }
    public bool GetUtilityCheckBoolDictEntry(string key)
    {
        utilityCheckBoolDictSemaphore.Wait();
        bool answer = utilityCheckBoolDict[key];
        utilityCheckBoolDictSemaphore.Release();
        return answer;
    }
    public void RemoveUtilityCheckBoolDictEntry(string key)
    {
        utilityCheckBoolDictSemaphore.Wait();
        utilityCheckBoolDict.Remove(key);
        utilityCheckBoolDictSemaphore.Release();
    }
    /// <summary>
    /// Can ONLY set existing values here, adding a new value this way will throw an error
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <exception cref="System.Exception"></exception>
    public void SetUtilityCheckBoolDictEntry(string key, bool value)
    {
        utilityCheckBoolDictSemaphore.Wait();
        try { bool _ = utilityCheckBoolDict[key]; }
        catch (System.Exception e)
        {
            throw new System.Exception($"Trying to set a key that does not exist! taeID {taeID}, key {key}; "
                + e.Message);
        }
        utilityCheckBoolDict[key] = value;
        utilityCheckBoolDictSemaphore.Release();
    }
    private readonly SemaphoreSlim utilityCheckBoolDictSemaphore = new SemaphoreSlim(1,1);

    // associational info
    public List<LocatableObject> TileContents
    {
        get
        {
            List<LocatableObject> tileContents = new List<LocatableObject>();
            foreach (int locID in tileContentsIds) 
            {
                tileContents.Add(LocatableObject.locatableObjectsById[locID]);
            }
            return tileContents;
        }
    }
    public List<int> tileContentsIds
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileContentsIds;
        set { CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].tileContentsIds = value; }
    }
    public Dictionary<HexDir, bool> hasCliffsByDirection
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].HasCliffsByDirection;
        set { CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].HasCliffsByDirection 
                = value; }
    }    // this'll eventually be broadened to a dictionary of tile boundary effects
    public int visibleUnitID
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].visibleUnitID;
        set { CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].visibleUnitID = value; }
    }
    public bool forceVisible 
    {
        get => CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].forceVisible;
        set 
        { 
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].forceVisible = value;
            tileUpdateNumber++;
        }
    }

    // constructors
    /// <summary>
    /// This constructor is for creating a TileArrayEntry corresponding to an already-existing TileData,
    /// which should specified by setting nextTileArrayEntryID to match the TileData's index.
    /// </summary>
    public TileArrayEntry() 
    {
        taeID = nextTileArrayEntryID;
        nextTileArrayEntryID++;

        if (!CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict.ContainsKey(taeID))
            throw new System.Exception("TileData with key " + taeID + " does not exist!");

        adjacentTileLocsBehind = new Dictionary<HexDir, int[]>();
        InitialiseAdjacentTileLocs();
    }
    /// <summary>
    /// This constructor is for creating a new TileArrayEntry.
    /// </summary>
    public TileArrayEntry(Vector3Int tileLocation, string basekey, bool passability = true)
    {
        taeID = nextTileArrayEntryID;
        nextTileArrayEntryID++;

        if (CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict.ContainsKey(taeID))
            throw new System.Exception("TileData with key " + taeID + " already exists!");
            
        CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID] = new TileData();

        TileLoc = tileLocation;
        tileBaseKey = basekey;
        isPassable = passability;
        tileContentsIds = new List<int>();
        terrainHeight = 0;
        utilityCheckBoolDict = new Dictionary<string, bool>();

        // _accessibleTileLocs = new List<Vector3Int>();
        adjacentTileLocsBehind = new Dictionary<HexDir, int[]>();
        foreach (int playerID in PlayerProperties.playersById.Keys)
            SetVisibilityByPlayerID(playerID, TileVisibility.Hidden);
        hasCliffsByDirection = new Dictionary<HexDir, bool>
        {
            { HexDir.W, false },
            { HexDir.NW, false },
            { HexDir.NE, false },
            { HexDir.SE, false },
            { HexDir.SW, false },
            { HexDir.E, false }
        };

        InitialiseAdjacentTileLocs();
    }

    // methods
    public void AssignTileContents(LocatableObject assignedLocatableObject)
    {
        // assign the gameObject to this TileArrayEntry
        tileContentsIds.Add(assignedLocatableObject.locatableID);
        try
        {
            // clearing assignedLocatableObject out of its previous tile contents
            assignedLocatableObject.GetLocatableLocationTAE().tileContentsIds =
                assignedLocatableObject.GetLocatableLocationTAE().tileContentsIds.Where(
                    x => x != assignedLocatableObject.locatableID).ToList();
            assignedLocatableObject.GetLocatableLocationTAE().SetDefaultVisibleUnit();
        } catch { }
        try
        {
            assignedLocatableObject.assignedTAEID = this.taeID;
            // if assignedLocatableObject is selected, set _selectionIndex to its index
            if (SelectorScript.Instance.selectedObject != null)
            {
                if (SelectorScript.Instance.selectedObject.TryGetComponent<LocatableObject>(out LocatableObject _))
                {
                    if (assignedLocatableObject.locatableID ==
                        SelectorScript.Instance.selectedObject.GetComponent<LocatableObject>().locatableID)
                    {
                        SelectorScript.Instance.selectedIndex = 
                            tileContentsIds.FindIndex(0, 
                                x => x == assignedLocatableObject.locatableID);
                    }
                }
            }
        }
        catch
        {
            Debug.LogError("Could not assign TileArrayEntry " + taeID + " at " + TileLoc + " to target LocatableObject");
        }
        UnitVisionScript.Instance.UpdateUnitVision();
    }
    public List<TileArrayEntry> GetAccessibleTAEs()
    {
        List<TileArrayEntry> outputList = new List<TileArrayEntry>();
        foreach (HexDir dir in AdjacentTileLocsByDirection.Keys)
        {
            // see if there are cliffs here in that direction
            if (hasCliffsByDirection[dir]) continue;
            // try and get a tile there
            try 
            {
                TileArrayEntry neighbour =
                    TileFinders.Instance.GetTileArrayEntryAtLocationQuick(
                        AdjacentTileLocsByDirection[dir]);
                // if there's a tile there, see if it is impassable or has cliffs in this direction
                if (neighbour.isPassable && !neighbour.hasCliffsByDirection[HexOrientation.Opposite(dir)])
                    outputList.Add(neighbour);
            }
            catch { }
        }
        return outputList;
    }
    public List<TileArrayEntry> GetAdjacentTAEs()
    {
        // MapArrayScript mapAS = GameObject.FindObjectOfType<MapArrayScript>();
        List<TileArrayEntry> outputList = new List<TileArrayEntry>();
        foreach (Vector3Int v in AdjacentTileLocsByDirection.Values)
        {
            try { outputList.Add(
                TileFinders.Instance.GetTileArrayEntryAtLocationQuick(v)); }
            catch {/* Debug.LogError("No adjacent tile found at " + v +" for tile at " + tileLoc);*/ }
        }
        if (outputList.Count == 0) Debug.LogError("Tile at " + TileLoc + " returns no neighbours!");
        if (outputList.Count > 6) Debug.LogError("Tile at " + TileLoc + " returns " + outputList.Count + " neighbours!");
        return outputList;
    }
    public TileArrayEntry GetAnticlockwiseTile(TileArrayEntry startingNeighbour)
    {
        try
        {
            var startingNeighbourTileLocByDirection =
                AdjacentTileLocsByDirection.Where(x => x.Value == startingNeighbour.TileLoc);
            HexDir startingNeighbourKey = startingNeighbourTileLocByDirection.First().Key;
            HexDir destinationKey = HexOrientation.Anticlockwise1(startingNeighbourKey);
            Vector3Int destinationLoc = AdjacentTileLocsByDirection[destinationKey];
            return TileFinders.Instance.GetTileArrayEntryAtLocationQuick(destinationLoc);
        }
        catch { return null; }
    }
    public TileArrayEntry GetClockwiseTile(TileArrayEntry startingNeighbour)
    {
        try
        {
            var startingNeighbourTileLocByDirection = 
                AdjacentTileLocsByDirection.Where(x => x.Value == startingNeighbour.TileLoc);
            HexDir startingNeighbourKey = startingNeighbourTileLocByDirection.First().Key;
            HexDir destinationKey = HexOrientation.Clockwise1(startingNeighbourKey);
            Vector3Int destinationLoc = AdjacentTileLocsByDirection[destinationKey];
            return TileFinders.Instance.GetTileArrayEntryAtLocationQuick(destinationLoc);
        }
        catch { return null; }
    }
    public HexDir GetOrientationKey(TileArrayEntry neighbour)
    {
        var neighbourDictionaryEntryHolder = 
            AdjacentTileLocsByDirection.Where(x => x.Value == neighbour.TileLoc);
        return neighbourDictionaryEntryHolder.First().Key;
    }
    public List<int> GetPlayersForWhomTileVisible()
    {
        List<int> result = new List<int>();
        foreach (int playerID in
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].visibilityDict.Keys)
            if (CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID]
                .visibilityDict[playerID] == TileVisibility.Visible)
                result.Add(playerID);
        return result;
    }
    public List<LocatableObject> GetTileContents()
    {
        List<LocatableObject> returnList = new List<LocatableObject>();
        foreach (int i in tileContentsIds) returnList.Add(LocatableObject.locatableObjectsById[i]);
        return returnList;
    }
    public Vector3 GetTileWorldLocation()
    {
        return MapArrayScript.Instance.tilemap.CellToWorld(TileLoc);
    }
    public TileVisibility GetVisibilityByPlayerID(int playerID)
    {
        return CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].visibilityDict[playerID];
    }
    private void InitialiseAdjacentTileLocs()
    {
        adjacentTileLocsBehind[HexDir.W] = new int[] { TileLoc.x - 1, TileLoc.y, TileLoc.z };
        adjacentTileLocsBehind[HexDir.E] = new int[] { TileLoc.x + 1, TileLoc.y, TileLoc.z };
        if (TileLoc.y % 2 == 0)
        {
            adjacentTileLocsBehind[HexDir.NW] = new int[] { TileLoc.x - 1, TileLoc.y + 1, TileLoc.z };
            adjacentTileLocsBehind[HexDir.NE] = new int[] { TileLoc.x, TileLoc.y + 1, TileLoc.z };

            adjacentTileLocsBehind[HexDir.SE] = new int[] { TileLoc.x, TileLoc.y - 1, TileLoc.z };
            adjacentTileLocsBehind[HexDir.SW] = new int[] { TileLoc.x - 1, TileLoc.y - 1, TileLoc.z };
        }
        else
        {
            adjacentTileLocsBehind[HexDir.NW] = new int[] { TileLoc.x, TileLoc.y + 1, TileLoc.z };
            adjacentTileLocsBehind[HexDir.NE] = new int[] { TileLoc.x + 1, TileLoc.y + 1, TileLoc.z };

            adjacentTileLocsBehind[HexDir.SE] = new int[] { TileLoc.x + 1, TileLoc.y - 1, TileLoc.z };
            adjacentTileLocsBehind[HexDir.SW] = new int[] { TileLoc.x, TileLoc.y - 1, TileLoc.z };
        }
    }
    public void InstantMoveContentsToTile()
    {
        foreach (var locId in tileContentsIds)
        {
            InstantMoveTransformToTile(LocatableObject.locatableObjectsById[locId].transform);
        }  
    }
    public void InstantMoveTransformToTile(Transform t)
    {
        t.position = MapArrayScript.Instance.tilemap.CellToWorld(TileLoc);
        // Debug.Log("Moved transform " + t.ToString() + " to tile " + tileLoc);
    }
    public void InstantiateListedScenery()
    {
        foreach (HexDir dir in hasCliffsByDirection.Keys)
        {
            if (hasCliffsByDirection[dir])
            {
                SceneryManager.Instance.AddCliffs(this, dir);
            }
        }
    }
    public void RectifyScenery(bool contentsAlreadyInstantiated = true)
    {
        if (contentsAlreadyInstantiated)
        {
            foreach (var locId in tileContentsIds)
            {
                LocatableObject loco = LocatableObject.locatableObjectsById[locId];
                if (loco.isScenery)
                {
                    loco.PreDestructionProtocols();
                    GameObject.Destroy(loco);
                }
            }
        }
        InstantiateListedScenery();
    }
    public void RectifyTileGraphics(bool contentsAlreadyInstantiated = true)
    {
        SetTileGraphicToListedType();
        if (contentsAlreadyInstantiated)
        {
            InstantMoveContentsToTile();
        }
        else { RectifyScenery(false); }
        SetTileVisibilityGraphic(PlayerProperties.humanPlayerID);
        // ShowVisibleUnitIfTileVisible();
    }
    public void SetTileGraphicToListedType()
    {
        MapArrayScript.Instance.tilemap.SetTile(TileLoc, MapArrayScript.Instance.tileBaseDict[tileBaseKey]);
    }
    public void SetTileVisibilityGraphic(int playerId)
    {
        if (!PlayerProperties.playersById.ContainsKey(playerId)) 
            throw new System.Exception("No player with playerID " + playerId + " found!");

        // Debug.Log("SetTileVisibilityGraphic on tile " + taeID);
        if (forceVisible) MapArrayScript.Instance.fowTilemap.SetTile(
            TileLoc, MapArrayScript.Instance.fowVisibleTile);
        else if (GetVisibilityByPlayerID(playerId) == TileVisibility.Visible)
            MapArrayScript.Instance.fowTilemap.SetTile(
                TileLoc, MapArrayScript.Instance.fowVisibleTile);
        else if (GetVisibilityByPlayerID(playerId) == TileVisibility.Explored)
            MapArrayScript.Instance.fowTilemap.SetTile(
                TileLoc, MapArrayScript.Instance.fowExploredTile);
        else MapArrayScript.Instance.fowTilemap.SetTile(
            TileLoc, MapArrayScript.Instance.fowHiddenTile);

        
        if (GetVisibilityByPlayerID(playerId) != TileVisibility.Visible && !forceVisible)
        {
            List<UnitInfo> units = new List<UnitInfo>();
            foreach (int locatableId in tileContentsIds)
            {
                // Debug.Log("Hiding everything: locatableId " + locatableId);
                LocatableObject locatableObject = LocatableObject.locatableObjectsById[locatableId];
                if (!locatableObject.isUnit) continue;

                UnitInfo unitInfo = locatableObject.GetComponent<UnitInfo>();
                units.Add(unitInfo);
            }
            foreach (UnitInfo unitInfo in units)
            {
                unitInfo.GetComponent<UnitGraphicsController>().HideSprite();
            }
        }
        else
        {
            SetVisibleUnit(visibleUnitID);
        }
    }
    private void SetDefaultVisibleUnit()
    {
        foreach (int locatableId in tileContentsIds)
        {
            LocatableObject locatable = LocatableObject.locatableObjectsById[locatableId];
            if (locatable.isUnit) 
            {
                visibleUnitID = locatable.GetComponent<UnitInfo>().unitInfoID;
                if (GetVisibilityByPlayerID(PlayerProperties.humanPlayerID) == TileVisibility.Visible 
                    || forceVisible)
                    locatable.GetComponent<UnitGraphicsController>().ShowSprite();
                break;
            }
        }
    }
    public void SetVisibilityByPlayerID(int playerID, TileVisibility visibility)
    {
        try
        {
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID].visibilityDict[playerID]
                = visibility;
        }
        catch
        {
            CurrentGameState.Instance.gameStateInfo.mapData.MapTileDataDict[taeID]
                .visibilityDict.Add(playerID, visibility);
        }
        tileUpdateNumber++;
    }
    public void SetVisibleUnit(int unitInfoID)
    {
        List <UnitInfo> units = new List <UnitInfo>();
        bool designatedUnitIsHere = false;

        // check if the designated unit is here
        foreach (int locatableId in tileContentsIds)
        {
            LocatableObject locatable = LocatableObject.locatableObjectsById[locatableId];
            if (!locatable.isUnit) continue;

            UnitInfo unitInfo = locatable.GetComponent<UnitInfo>();
            units.Add(unitInfo);
            if (unitInfo.unitInfoID == unitInfoID) designatedUnitIsHere = true;
        }

        if (GetVisibilityByPlayerID(PlayerProperties.humanPlayerID) != TileVisibility.Visible 
            && !forceVisible)
        {
            // don't show anything, but set _visibleUnitID to the correct one;
            foreach (UnitInfo unitInfo in units)
            {
                unitInfo.GetComponent<UnitGraphicsController>().HideSprite();
            }
            visibleUnitID = unitInfoID;
        }
        else if (designatedUnitIsHere) 
        {
            foreach (UnitInfo unitInfo in units)
            {
                if (unitInfo.unitInfoID != unitInfoID)
                    unitInfo.GetComponent<UnitGraphicsController>().HideSprite();
                else unitInfo.GetComponent<UnitGraphicsController>().ShowSprite();
            }
            visibleUnitID = unitInfoID;
        }
        else SetDefaultVisibleUnit();
    }
    public void ShowVisibleUnitIfTileVisible()
    {
        SetVisibleUnit(visibleUnitID);
    }
    public bool TileIsAdjacentToHere(TileArrayEntry targetTAE)
    {
        if (AdjacentTileLocsByDirection.Values.Contains(targetTAE.TileLoc)) return true;
        else return false;
    }
}

public class TileData
{
    public string tileBaseKey;
    public int[] tileActualLoc = new int[3];
    public float terrainHeight;
    public bool isPassable;
    public int visibleUnitID = -1;
    public Dictionary<int, TileVisibility> visibilityDict = new Dictionary<int, TileVisibility>();
    public bool forceVisible = false;
    public List<int> tileContentsIds;
    public Dictionary<HexDir, bool> HasCliffsByDirection;
    public Dictionary<string, bool> utilityCheckBoolDict = new Dictionary<string, bool>();
}
