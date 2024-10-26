using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;

/// <summary>
/// This class is for unit movement functionality. 
/// It contains methods that let you move units in different ways.
/// It's attached to ScriptBucket.
/// </summary>
public class UnitMovement : MonoBehaviour
{
    private static UnitMovement instance;
    public static UnitMovement Instance { get { return instance; } }
    private void Awake()
    {
        if (name == "ScriptBucket" && instance == null) instance = this;
    }

    public GameObject movementPreviewMarkerPrefab; // this is why this script is a MonoBehaviour
    public GameObject accessibleTileMarkerPrefab;

    public List<GameObject> DebugAccessibleTilesPreview(LocatableObject unit, int maxMoveDistance)
    {
        List<TileArrayEntry> highlightTiles = GetLocation1TurnReachableTiles(unit, maxMoveDistance, true);
        List<GameObject> previewMarkers = new List<GameObject>();

        bool ignoreThis = true;
        foreach (TileArrayEntry entry in highlightTiles)
        {
            if (ignoreThis) { ignoreThis = false; }
            else
            {
                GameObject marker = Instantiate(
                    accessibleTileMarkerPrefab,
                    MapArrayScript.Instance.tilemap.CellToWorld(entry.TileLoc),
                    Quaternion.identity);
                previewMarkers.Add(marker);
            }
        }
        return previewMarkers;
    }
    /// <summary>
    /// Note that 'accountForVisibility' doesn't affect whether the algo detects a tile fits the
    /// eligibility condition, only what it thinks the quickest path to it is
    /// </summary>
    /// <param name="unitLoco"></param>
    /// <param name="eligibilityCondition"></param>
    /// <param name="accountForVisibility"></param>
    /// <returns></returns>
    public TileArrayEntry GetQuickestReachableTileWithCondition(LocatableObject unitLoco,
        Predicate<TileArrayEntry> eligibilityCondition, bool accountForVisibility = true)
    {
        return GetQuickestReachableTileWithCondition(
            TileFinders.Instance.GetTileArrayEntryByID(unitLoco.assignedTAEID).TileLoc,
            eligibilityCondition,
            unitLoco.unitInfo.ownerID,
            accountForVisibility,
            MoveType.Foot);
    }
    /// <summary>
    /// Note that 'accountForVisibility' doesn't affect whether the algo detects a tile fits the
    /// eligibility condition, only what it thinks the quickest path to it is
    /// </summary>
    /// <param name="startLocation"></param>
    /// <param name="eligibilityCondition"></param>
    /// <param name="visibilityPlayerID"></param>
    /// <param name="accountForVisibility"></param>
    /// <param name="moveType"></param>
    /// <returns></returns>
    public TileArrayEntry GetQuickestReachableTileWithCondition(Vector3Int startLocation,
        Predicate<TileArrayEntry> eligibilityCondition, 
        int visibilityPlayerID, bool accountForVisibility = true, 
        MoveType moveType = MoveType.Foot)
    {
        Dictionary<int, bool> checkDict = new Dictionary<int, bool>();
        Dictionary<int, int> moveCostToTile = new();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            checkDict[t.taeID] = false;
            moveCostToTile[t.taeID] = int.MaxValue/2;
        }

        List<TileArrayEntry> reachableTiles = new List<TileArrayEntry>();
        TileArrayEntry startLocationTAE
            = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(startLocation);
        reachableTiles.Add(startLocationTAE);
        checkDict[startLocationTAE.taeID] = true;
        moveCostToTile[startLocationTAE.taeID] = 0;

        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        int distanceTravelled = 0;
        int loopBreaker = 0;
        while (loopBreaker++ < 2000)
        {
            tilesToAdd.Clear();
            // here we accommodate for variable move costs
            foreach (TileArrayEntry tile 
                in reachableTiles.Where(x => moveCostToTile[x.taeID] == distanceTravelled))
            {
                foreach (TileArrayEntry nextTile in tile.GetAccessibleTAEs())
                {
                    // see if it's in reachableTiles or tilesToAdd (i.e. have we checked it already?)
                    if (nextTile != null)
                    {
                        if (!checkDict[nextTile.taeID])
                        { 
                            // if not, see if it fits the condition; if not, add it to tilesToAdd
                            if (eligibilityCondition(nextTile)) return nextTile;
                            tilesToAdd.Add(nextTile);
                            checkDict[nextTile.taeID] = true;
                            if (accountForVisibility
                                && nextTile.IsTileHiddenWithOverrides(visibilityPlayerID))
                                moveCostToTile[nextTile.taeID]
                                    = Math.Min(moveCostToTile[nextTile.taeID],
                                    moveCostToTile[tile.taeID] + 1);
                            else moveCostToTile[nextTile.taeID]
                                = Math.Min(moveCostToTile[nextTile.taeID],
                                moveCostToTile[tile.taeID] + nextTile.MoveCost);
                        }
                    }
                }
                // if it's counting hidden tiles as passable, add adjacent hidden tiles
                if (accountForVisibility)
                {
                    foreach (TileArrayEntry nextTile 
                        in tile.GetAccessibleTAEs(false,!tile.IsTileHiddenWithOverrides(visibilityPlayerID)))
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (nextTile != null
                            && nextTile.IsTileHiddenWithOverrides(visibilityPlayerID))
                        {
                            if (!checkDict[nextTile.taeID])
                            { // if not, add it to tilesToAdd
                                tilesToAdd.Add(nextTile);
                                checkDict[nextTile.taeID] = true;
                                moveCostToTile[nextTile.taeID] 
                                    = Math.Min(moveCostToTile[nextTile.taeID], 
                                    moveCostToTile[tile.taeID] + 1);
                            }
                        }
                    }
                }
            }
            reachableTiles.AddRange(tilesToAdd);
            distanceTravelled++;
            if (tilesToAdd.Count == 0) break;
        }
        return null;
    }
    /// <summary>
    /// This uses the designated unit's owner to calculate visibility.
    /// </summary>
    /// <param name="unitLoco"></param>
    /// <param name="maxMoveDistance"></param>
    /// <param name="accountForVisibility"></param>
    /// <returns></returns>
    public List<TileArrayEntry> GetLocation1TurnReachableTiles(
        LocatableObject unitLoco, int maxMoveDistance, bool accountForVisibility = true)
    {
        return GetLocation1TurnReachableTiles(
            TileFinders.Instance.GetTileArrayEntryByID(unitLoco.assignedTAEID).TileLoc,
            maxMoveDistance,
            unitLoco.unitInfo.ownerID,
            accountForVisibility
            );
    }
    public List<TileArrayEntry> GetLocation1TurnReachableTiles(
        Vector3Int startLocation, int maxMoveDistance, int visibilityPlayerID, 
        bool accountForVisibility = true)
    {
        Dictionary<int, bool> checkDict = new Dictionary<int, bool>();
        Dictionary<int, int> distanceDict = new();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            checkDict[t.taeID] = false;
            distanceDict[t.taeID] = int.MaxValue/2;
        }

        List<TileArrayEntry> reachableTiles = new List<TileArrayEntry>();
        TileArrayEntry startLocationTAE 
            = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(startLocation);
        reachableTiles.Add(startLocationTAE);
        checkDict[ startLocationTAE.taeID] = true;
        distanceDict[startLocationTAE.taeID] = 0;

        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        int minDistanceTravelled = 0;
        while (minDistanceTravelled < maxMoveDistance)
        {
            tilesToAdd.Clear();
            // note we're not propagating from tiles that are at maximum move range!
            foreach (TileArrayEntry tile 
                in reachableTiles.Where(x => distanceDict[x.taeID] < maxMoveDistance))
            {
                foreach (TileArrayEntry nextTile in tile.GetAccessibleTAEs())
                {
                    if (nextTile != null)
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (!checkDict[ nextTile.taeID])
                        { // if not, add it to tilesToAdd
                            tilesToAdd.Add(nextTile);
                            checkDict[nextTile.taeID] = true;
                            if (accountForVisibility 
                                && nextTile.IsTileHiddenWithOverrides(visibilityPlayerID))
                                distanceDict[nextTile.taeID]
                                    = Math.Min(distanceDict[nextTile.taeID],
                                    distanceDict[tile.taeID] + 1);
                            else distanceDict[nextTile.taeID]
                                    = Math.Min(distanceDict[nextTile.taeID],
                                    distanceDict[tile.taeID] + nextTile.MoveCost);
                        }
                    }
                }
                // if it's counting hidden tiles as passable, add adjacent hidden tiles
                if (accountForVisibility)
                {
                    foreach (TileArrayEntry nextTile 
                        in tile.GetAccessibleTAEs(false, !tile.IsTileHiddenWithOverrides(visibilityPlayerID)))
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (nextTile != null
                            && nextTile.IsTileHiddenWithOverrides(visibilityPlayerID))
                        {
                            if (!checkDict[ nextTile.taeID])
                            { // if not, add it to tilesToAdd
                                tilesToAdd.Add(nextTile);
                                checkDict[nextTile.taeID] = true;
                                distanceDict[nextTile.taeID]
                                    = Math.Min(distanceDict[nextTile.taeID],
                                    distanceDict[tile.taeID] + 1);
                            }
                        }
                    }
                }
            }
            reachableTiles.AddRange(tilesToAdd);
            minDistanceTravelled++;
            if (tilesToAdd.Count == 0) break;
        }
        return reachableTiles;
    }
    public bool MoveUnitDefault(LocatableObject unit, TileArrayEntry target, int maxMoveDistance,
        bool costsNoUnitAP = false)
    {
        UnitInfo unitInfo = unit.GetComponent<UnitInfo>();
        PlayerProperties playerProperties =
            PlayerSetupScript.Instance.playerList.Find(x => x.playerID == unitInfo.ownerID);

        List<TileArrayEntry> movementPath = 
            AStarPathCalculator(unit.GetLocatableLocationTAE(), target, playerProperties.playerID, 
            true);
        // if (movementPath == null) { Debug.Log("No path returned!"); return; }
        // Debug.Log("movementPath returned");

        if (movementPath != null)
        {
            if (!costsNoUnitAP)
            {
                unit.GetComponent<UnitBehaviour>().ExpendAction();
            }
            StartCoroutine(MoveAlongPath(unit, movementPath, maxMoveDistance));
            return true;
        }
        else { return false; }
    }
    public List<GameObject> MoveUnitPreview(LocatableObject unit, TileArrayEntry target, 
        int maxMoveDistance)
    {
        List<GameObject> previewMarkers = new List<GameObject>();
        int maxDrawDistance = 30;

        // improving performance on visible impassable tiles
        if (TileFinders.Instance.GetTileDistanceToTiles(unit.GetLocatableLocationTAE(),
            new List<int> { target.taeID })[target.taeID] > maxDrawDistance)
            return previewMarkers;
        if (!target.isPassable 
            && target.IsTileHiddenWithOverrides(PlayerProperties.humanPlayerID)) 
            return previewMarkers;

        // Get the movement path
        List<TileArrayEntry> movementPath = AStarPathCalculator(
            unit.GetLocatableLocationTAE(), target, PlayerProperties.humanPlayerID, 
            true, true, maxDrawDistance);
        
        if (movementPath == null) return previewMarkers;

        bool ignoreThis = true;
        int distanceCounter = 0;
        foreach (TileArrayEntry entry in movementPath)
        {
            if (ignoreThis) { ignoreThis = false; }
            else if(distanceCounter <= maxMoveDistance)
            {
                GameObject marker = Instantiate(
                    movementPreviewMarkerPrefab,
                    MapArrayScript.Instance.tilemap.CellToWorld(entry.TileLoc),
                    Quaternion.identity);
                previewMarkers.Add(marker);
            } 
            // add future turn move preview functionality here
            distanceCounter++;
        }
        return previewMarkers;
    }

    #region static movement calculation stuff
    private static IEnumerator MoveAlongPath(
        LocatableObject unit, List<TileArrayEntry> movementPath, int maxSteps)
    {
        PlayerAIMasterScript.Instance.ActionsAIWaitingToFinish++;
        if (movementPath.Count < 2) 
        { 
            Debug.Log("Path too short!");
            PlayerAIMasterScript.Instance.ActionsAIWaitingToFinish--;
            yield break; 
        }
        int tilesMoved = 1;
        int moveExpended = 0;
        while (moveExpended < maxSteps && tilesMoved < movementPath.Count)
        {
            // stop if you can't access the next tile
            if (!movementPath[tilesMoved - 1].GetAccessibleTAEs().Contains(movementPath[tilesMoved])) 
            {
                Debug.Log("Path blocked!");
                PlayerAIMasterScript.Instance.ActionsAIWaitingToFinish--;
                yield break; 
            }
            // do something (probably attack) if you encounter an enemy unit
            else foreach (int id in movementPath[tilesMoved].tileContentsIds)
            {
                if (LocatableObject.locatableObjectsById[id].isUnit)
                    {
                        UnitInfo encounteredUnitInfo 
                            = LocatableObject.locatableObjectsById[id].GetComponent<UnitInfo>();
                        UnitInfo movingUnitInfo = unit.GetComponent<UnitInfo>();
                        if (encounteredUnitInfo.ownerID != movingUnitInfo.ownerID)
                        {
                            unit.GetComponent<UnitBehaviour>().MeleeAttackTile(movementPath[tilesMoved]);
                            // Debug.Log("Unit " + movingUnitInfo.unitInfoID + " attacked tile at " + movementPath[i].TileLoc);
                            PlayerAIMasterScript.Instance.ActionsAIWaitingToFinish--;
                            yield break;
                        }
                    }
            }
            unit.DebugMoveToTile(movementPath[tilesMoved]); // this'll need replacing with a movement animation
            // this should allow moving to any adjacent accessible tile,
            // even if move cost > remaining moves
            moveExpended += movementPath[tilesMoved].MoveCost;
            tilesMoved++;
            yield return new WaitForSeconds(0.1f);
        }
        PlayerAIMasterScript.Instance.ActionsAIWaitingToFinish--;
        yield break;
    }
    public static List<TileArrayEntry> AStarPathCalculator(
        TileArrayEntry start, TileArrayEntry target, int visibilityPlayerID,
        bool accountForVisibility = true, bool distanceLimited = false, int limitDistance = 30)
    {
        return AStarPathCalculator(start, target, visibilityPlayerID, new CancellationToken(),
            accountForVisibility, distanceLimited, limitDistance);
    }
    public static List<TileArrayEntry> AStarPathCalculator(
        TileArrayEntry start, TileArrayEntry target, int visibilityPlayerID, CancellationToken cancelToken,
        bool accountForVisibility = true, bool distanceLimited = false, int limitDistance = 30)
    {
        if (target == null) 
        {
            Debug.Log("Null target passed to AStarPathCalculator");
            return null; 
        }
        Dictionary<int, bool> openSetDict = new Dictionary<int, bool>();
        Dictionary<int, bool> tilesToTryDict = new Dictionary<int, bool>();
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            openSetDict[ t.taeID] = false;
            tilesToTryDict[ t.taeID] = false;
        }

        Func<CancellationToken, bool> DetectCancellationAndCleanUp = new Func<CancellationToken, bool>(
            cToke => {
                if (cToke.IsCancellationRequested)
                {
                    return true;
                }
                else return false;
            });

        ICollection tilesToTry;
        if (!distanceLimited) tilesToTry = MapArrayScript.Instance.MapTileArray;
        else tilesToTry = TileFinders.Instance.GetTilesWithinDistance(start, limitDistance);

        foreach (TileArrayEntry t in tilesToTry)
        {
            tilesToTryDict[ t.taeID] = true;
        }

        // this is the set of tiles to check next, whether moving from them
        // to their neighbours might reduce the neighbours' estdStepsViaHere
        List<TileArrayEntry> openSet = new List<TileArrayEntry>() { start };
        openSetDict[start.taeID] = true;

        Dictionary<int, TileArrayEntry> cameFrom = new Dictionary<int, TileArrayEntry>();
        Dictionary<int, int> leastStepsFromStart = new Dictionary<int, int>();
        Dictionary<int, int> estdStepsViaHere = new Dictionary<int, int>();

        if (DetectCancellationAndCleanUp(cancelToken)) return null;
        // Debug.Log("Beginning leastStepsFromStart dictionary assembly");
        foreach (TileArrayEntry tae in tilesToTry)
        {
            leastStepsFromStart[tae.taeID] = int.MaxValue / 2;
        }
        leastStepsFromStart[start.taeID] = 0;
        // Debug.Log("leastStepsFromStart[start] = " + leastStepsFromStart[start]);

        if (DetectCancellationAndCleanUp(cancelToken)) return null;
        // Debug.Log("Beginning estdStepsViaHere dictionary assembly");
        foreach (TileArrayEntry tae in tilesToTry)
        {
            estdStepsViaHere[tae.taeID]
                = leastStepsFromStart[tae.taeID] + DistanceEstimate(tae, target);
        }
        // Debug.Log("Estd distance to target: " + DistanceEstimate(start, target));

        // Debug.Log("openSet.Count = " + openSet.Count);

        // loop breaker
        int iterCount = 0;

        while (openSet.Count > 0)
        {
            if (DetectCancellationAndCleanUp(cancelToken)) return null;
            // Debug.Log("Beginning A* iteration " + iterCount);
            iterCount++;
            if (iterCount > 2000) { Debug.Log("A* while loop broken"); break; }
            /// string estdStepsList = "";
            /// foreach (var x in estdStepsViaHere.Keys) estdStepsList += x + ", ";

            TileArrayEntry current = openSet[0];
            if (openSet[0] == null) 
                throw new System.Exception($"openSet[0] was null! openSet.Count == {openSet.Count()}");
            int currentLowestEstdSteps = estdStepsViaHere[openSet[0].taeID];
            /// if (!estdStepsViaHere.ContainsKey(current.taeID)) 
            ///    Debug.LogError("Could not find current.taeID " + current.taeID 
            ///        + " in estdStepsViaHere, which contains: " 
            ///        + estdStepsList);

            // Each iteration of the while loop, pick the lowest-estdStepsViaHere tile and
            // check its accessible neighbours to see if we can lower their estdStepsViaHere
            foreach (TileArrayEntry tae in openSet)
            {
                if (tae == null) throw new System.Exception("Null entry found in openSet!");
                ///if (!estdStepsViaHere.ContainsKey(tae.taeID))
                ///    Debug.LogError("Could not find tae.taeID " + tae.taeID
                ///        + " in estdStepsViaHere, which contains: " 
                ///        + estdStepsList);
                /// Debug.Log("estdStepsViaHere " + tae.tileLoc + ": " + estdStepsViaHere[tae]);
                try
                {
                    if (estdStepsViaHere[tae.taeID] < currentLowestEstdSteps)
                    {
                        current = tae;
                        currentLowestEstdSteps = estdStepsViaHere[tae.taeID];
                    }
                }
                catch(System.Exception e) {
                    throw new System.Exception(
                    $"Couln't find estdStepsViaHere[taeID {tae.taeID}]" 
                    + $", relevant check bool: {tilesToTryDict[tae.taeID]}", e); }
            }
            if (current.taeID == target.taeID)
            {
                return ReconstructPath(cameFrom, target); 
            }

            /* Debug.Log("Iteration count " + iterCount + ", current tileLoc = " + current.tileLoc + 
                "; openSet.Count after culling current = " + openSet.Count);*/
            openSet.Remove(current);
            openSetDict[current.taeID] = false;

            // to avoid race conditions if current is also another currentList member's neighbour
            int cachedLeastStepsCurrent = leastStepsFromStart[current.taeID];
                
            // get different lists of neighbours depending on whether accpuntForVisibility is true
            List<TileArrayEntry> validNeighbours = current.GetAccessibleTAEs().Where(
            x => tilesToTryDict[x.taeID]).ToList();
            if (accountForVisibility) validNeighbours.AddRange(
                current.GetAdjacentTAEs().Where(
                    x => tilesToTryDict[x.taeID]
                    && !validNeighbours.Contains(x)
                    // this one'll cause race conditions if the whole method is multi-threaded
                    // best answer I guess is to make sure nothing is done with the output
                    // if tile visibility changes, using TileArrayEntry.tileUpdateNumber checks
                    && x.IsTileHiddenWithOverrides(visibilityPlayerID)
                    && !(current.hasCliffsByDirection[
                        current.AdjacentTileLocsByDirection.Where(
                            y => y.Value == x.TileLoc).ToList().First().Key]
                        && current.IsTileHiddenWithOverrides(visibilityPlayerID))
                    ).ToList());

            foreach (TileArrayEntry neighbour in validNeighbours)
            {
                int tentativeGScore;
                // this is where movement costs are factored in
                if (accountForVisibility && neighbour.IsTileHiddenWithOverrides(visibilityPlayerID))
                    tentativeGScore = cachedLeastStepsCurrent + 1;
                else tentativeGScore = cachedLeastStepsCurrent + neighbour.MoveCost;
                        
                // if you've just found a faster way to get to neighbour than previously,
                // update everything to reflect that
                if (tentativeGScore < leastStepsFromStart[neighbour.taeID])
                {
                    // because going via current is the fastest known way to get to neighbour
                    cameFrom[neighbour.taeID] = current;
                    leastStepsFromStart[neighbour.taeID] = tentativeGScore;
                    estdStepsViaHere[neighbour.taeID]
                        = tentativeGScore + DistanceEstimate(neighbour, target);
                    // you just found a faster way to get to neighbour,
                    // so you have to look at onward connections from there
                    if (!openSetDict[neighbour.taeID])
                    {
                        openSet.Add(neighbour);
                        openSetDict[neighbour.taeID] = true;
                    }
                }
            }
        }

        // stuff to fire if the destination can't be reached
        // Debug.Log("Can't get to " + target.tileLoc + " from " + start.tileLoc + "!");
        
        return null;
    }
    private static int DistanceEstimate(TileArrayEntry origin, TileArrayEntry target)
    {
        // doesn't have to be great right now, just has to work
        return Mathf.Abs(origin.TileLoc.x - target.TileLoc.x) 
            + Mathf.Abs(origin.TileLoc.y - target.TileLoc.y);
    }
    private static List<TileArrayEntry> ReconstructPath(
        Dictionary<int, TileArrayEntry> cameFrom,
        TileArrayEntry destination)
    {
        TileArrayEntry current = destination;
        List<TileArrayEntry> totalPath = new List<TileArrayEntry>();
        totalPath.Add(destination);

        while (cameFrom.ContainsKey(current.taeID)) 
        {
            current = cameFrom[current.taeID];
            totalPath.Add(current);
        }
        totalPath.Reverse();
        return totalPath;
    }
    #endregion
}
