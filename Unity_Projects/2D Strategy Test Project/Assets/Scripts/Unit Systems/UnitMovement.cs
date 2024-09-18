using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class is for unit movement functionality. It contains methods that let you move units in different ways.
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
        string uniqueCheckKey = $"GetLocation1TurnReachableTiles {System.Environment.CurrentManagedThreadId}";
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.AddUtilityCheckBoolDictEntry(uniqueCheckKey, false);
        }

        List<TileArrayEntry> reachableTiles = new List<TileArrayEntry>();
        TileArrayEntry startLocationTAE 
            = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(startLocation);
        reachableTiles.Add(startLocationTAE);
        startLocationTAE.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);

        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        int distanceTravelled = 0;
        while (distanceTravelled < maxMoveDistance)
        {
            tilesToAdd.Clear();
            foreach (TileArrayEntry tile in reachableTiles)
            {
                foreach (TileArrayEntry nextTile in tile.GetAccessibleTAEs())
                {
                    // see if it's in reachableTiles or tilesToAdd
                    if (nextTile != null)
                    {
                        if (!nextTile.GetUtilityCheckBoolDictEntry(uniqueCheckKey))
                        { // if not, add it to tilesToAdd
                            tilesToAdd.Add(nextTile);
                            nextTile.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
                        }
                    }
                }
                // if it's for display, add adjacent hidden tiles
                if (accountForVisibility)
                {
                    foreach (TileArrayEntry nextTile in tile.GetAdjacentTAEs())
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (nextTile != null
                            && nextTile.GetVisibilityByPlayerID(visibilityPlayerID)
                            == TileVisibility.Hidden
                            && !(nextTile.forceVisible
                            && visibilityPlayerID == PlayerProperties.humanPlayerID))
                        {
                            if (!nextTile.GetUtilityCheckBoolDictEntry(uniqueCheckKey))
                            { // if not, add it to tilesToAdd
                                tilesToAdd.Add(nextTile);
                                nextTile.SetUtilityCheckBoolDictEntry(uniqueCheckKey, true);
                            }
                        }
                    }
                }
            }
            reachableTiles.AddRange(tilesToAdd);
            distanceTravelled++;
            if (tilesToAdd.Count == 0) break;
        }
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
            t.RemoveUtilityCheckBoolDictEntry(uniqueCheckKey);
        return reachableTiles;
    }
    public bool MoveUnitDefault(LocatableObject unit, TileArrayEntry target, int maxMoveDistance,
        bool isFreeAction = false)
    {
        UnitInfo unitInfo = unit.GetComponent<UnitInfo>();
        PlayerProperties playerProperties =
            PlayerSetupScript.Instance.playerList.Find(x => x.playerID == unitInfo.ownerID);

        List<TileArrayEntry> movementPath = 
            AStarPathCalculatorMultithreaded(unit.GetLocatableLocationTAE(), target, playerProperties.playerID, 
            true);
        // if (movementPath == null) { Debug.Log("No path returned!"); return; }
        // Debug.Log("movementPath returned");

        if (movementPath != null)
        {
            // this, I guess, is the point at which the thread should rejoin the main thread?
            if (!isFreeAction) 
            { 
                if (unit.GetComponent<UnitInfo>().currentActions <= 0) 
                    unit.GetComponent<UnitInfo>().currentReadiness -= 0.2f;
                unit.GetComponent<UnitInfo>().currentActions--;
            }
            StartCoroutine(MoveAlongPath(unit, movementPath, maxMoveDistance));
            return true;
        }
        else return false;
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
            && (target.GetVisibilityByPlayerID(PlayerProperties.humanPlayerID) 
            != TileVisibility.Hidden || target.forceVisible)) 
            return previewMarkers;

        // Get the movement path
        List<TileArrayEntry> movementPath = AStarPathCalculatorMultithreaded(
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
        if (movementPath.Count < 2) { Debug.Log("Path too short!"); yield break; }
        int i = 1;
        while (i <= maxSteps && i < movementPath.Count)
        {
            // stop if you can't access the next tile
            if (!movementPath[i - 1].GetAccessibleTAEs().Contains(movementPath[i])) 
            {
                Debug.Log("Path blocked!");
                yield break; 
            }
            // do something (probably attack) if you encounter an enemy unit
            else foreach (int id in movementPath[i].tileContentsIds)
            {
                if (LocatableObject.locatableObjectsById[id].isUnit)
                    {
                        UnitInfo encounteredUnitInfo 
                            = LocatableObject.locatableObjectsById[id].GetComponent<UnitInfo>();
                        UnitInfo movingUnitInfo = unit.GetComponent<UnitInfo>();
                        if (encounteredUnitInfo.ownerID != movingUnitInfo.ownerID)
                        {
                            unit.GetComponent<UnitBehaviour>().MeleeAttackTile(movementPath[i]);
                            // Debug.Log("Unit " + movingUnitInfo.unitInfoID + " attacked tile at " + movementPath[i].TileLoc);
                            yield break;
                        }
                    }
            }
            unit.DebugMoveToTile(movementPath[i]); // this'll need replacing with a movement animation
            i++;
            yield return new WaitForSeconds(0.1f);
        }
    }
    public static List<TileArrayEntry> AStarPathCalculatorMultithreaded(
        TileArrayEntry start, TileArrayEntry target, int visibilityPlayerID,
        bool accountForVisibility = true, bool distanceLimited = false, int limitDistance = 30)
    {
        string openSetMemberKey = $"AStarPathCalculator_openSetMemberKey {start.taeID} {target.taeID} " +
            $"{System.Environment.CurrentManagedThreadId}";
        string tilesToTryMemberKey = $"AStarPathCalculator_tilesToTryMemberKey {start.taeID} " +
            $"{target.taeID} {System.Environment.CurrentManagedThreadId}";
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.AddUtilityCheckBoolDictEntry(openSetMemberKey, false);
            t.AddUtilityCheckBoolDictEntry(tilesToTryMemberKey, false);
        }

        ICollection tilesToTry;
        if (!distanceLimited) tilesToTry = MapArrayScript.Instance.MapTileArray;
        else tilesToTry = TileFinders.Instance.GetTilesWithinDistance(start, limitDistance);

        // Values: 0 = available, 1 = taken
        Dictionary<int, SemaphoreSlim> tileAvailabilitySemaphores 
            = new Dictionary<int, SemaphoreSlim>();
        int taeArrayIndex = 0;
        foreach (TileArrayEntry t in tilesToTry)
        { 
            t.SetUtilityCheckBoolDictEntry(tilesToTryMemberKey, true);
            tileAvailabilitySemaphores.Add(t.taeID, new SemaphoreSlim(1, 1));
            taeArrayIndex++;
        }

        // this is the set of tiles to check next, whether moving from them
        // to their neighbours might reduce the neighbours' estdStepsViaHere
        List<TileArrayEntry> openSet = new List<TileArrayEntry>() { start };
        SemaphoreSlim openSetSemaphore = new SemaphoreSlim(1, 1);
        start.SetUtilityCheckBoolDictEntry(openSetMemberKey, true);

        Dictionary<int, TileArrayEntry> cameFrom = new Dictionary<int, TileArrayEntry>();
        Dictionary<int, int> leastStepsFromStart = new Dictionary<int, int>();
        Dictionary<int, int> estdStepsViaHere = new Dictionary<int, int>();

        // Debug.Log("Beginning leastStepsFromStart dictionary assembly");
        foreach (TileArrayEntry tae in tilesToTry)
        {
            leastStepsFromStart[tae.taeID] = int.MaxValue / 2;
        }
        leastStepsFromStart[start.taeID] = 0;
        // Debug.Log("leastStepsFromStart[start] = " + leastStepsFromStart[start]);

        // Debug.Log("Beginning estdStepsViaHere dictionary assembly");
        foreach (TileArrayEntry tae in tilesToTry)
        {
            estdStepsViaHere[tae.taeID]
                = leastStepsFromStart[tae.taeID] + DistanceEstimate(tae, target);
        }
        // Debug.Log("Estd distance to target: " + DistanceEstimate(start, target));

        List<TileArrayEntry> currentList = new List<TileArrayEntry> { start };
        // Debug.Log("openSet.Count = " + openSet.Count);

        // loop breaker
        int iterCount = 0;

        while (openSet.Count > 0)
        {
            // Debug.Log("Beginning A* iteration " + iterCount);
            iterCount++;
            if (iterCount > 2000) { Debug.Log("A* while loop broken"); break; }
            /// string estdStepsList = "";
            /// foreach (var x in estdStepsViaHere.Keys) estdStepsList += x + ", ";

            currentList = new List<TileArrayEntry> { openSet[0] };
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
                        currentList.Clear();
                        currentList.Add(tae);
                        currentLowestEstdSteps = estdStepsViaHere[tae.taeID];
                    }
                    else if (estdStepsViaHere[tae.taeID] == currentLowestEstdSteps)
                    {
                        currentList.Add(tae);
                    }
                }
                catch(System.Exception e) {
                    throw new System.Exception(
                    $"Couln't find estdStepsViaHere[taeID {tae.taeID}]" 
                    + $", relevant check bool: {tae.GetUtilityCheckBoolDictEntry(tilesToTryMemberKey)}", e); }
            }
            foreach (var current in currentList)
            {
                if (current.taeID == target.taeID)
                {
                    foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
                    {
                        t.RemoveUtilityCheckBoolDictEntry(openSetMemberKey);
                        t.RemoveUtilityCheckBoolDictEntry(tilesToTryMemberKey);
                    }
                    return ReconstructPath(cameFrom, target); 
                }

                /* Debug.Log("Iteration count " + iterCount + ", current tileLoc = " + current.tileLoc + 
                    "; openSet.Count after culling current = " + openSet.Count);*/
                openSet.Remove(current);
                current.SetUtilityCheckBoolDictEntry( openSetMemberKey, false);
            }

            // This is where the internal multithreading starts
            Task[] neighbourCheckTasks = new Task[currentList.Count];
            for (int i = 0; i < currentList.Count; i++)
            {
                TileArrayEntry current = currentList[i];
                // to avoid race conditions if current is also another currentList member's neighbour
                int cachedLeastStepsCurrent = leastStepsFromStart[current.taeID];
                neighbourCheckTasks[i] = Task.Run(() =>
                {
                    // get different lists of neighbours depending on whether canMislead is true
                    List<TileArrayEntry> validNeighbours = current.GetAccessibleTAEs().Where(
                    x => x.GetUtilityCheckBoolDictEntry(tilesToTryMemberKey)).ToList();
                    if (accountForVisibility) validNeighbours.AddRange(
                        current.GetAdjacentTAEs().Where(
                            x => x.GetUtilityCheckBoolDictEntry(tilesToTryMemberKey)
                            && !validNeighbours.Contains(x)
                            // this one'll cause race conditions if the whole method is multi-threaded
                            // best answer I guess is to make sure nothing is done with the output
                            // if tile visibility changes, using TileArrayEntry.tileUpdateNumber checks
                            && x.GetVisibilityByPlayerID(visibilityPlayerID) == TileVisibility.Hidden
                            && !x.forceVisible
                            && !(current.hasCliffsByDirection[
                                current.AdjacentTileLocsByDirection.Where(
                                    y => y.Value == x.TileLoc).ToList().First().Key]
                                && current.GetVisibilityByPlayerID(visibilityPlayerID) 
                                != TileVisibility.Hidden)
                            ).ToList());

                    foreach (TileArrayEntry neighbour in validNeighbours)
                    {
                        // when there are tile weights, substitute that here
                        int tentativeGScore = cachedLeastStepsCurrent + 1;

                        // here's where we wait for neighbour to become available
                        tileAvailabilitySemaphores[neighbour.taeID].Wait();
                        
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
                            if (!neighbour.GetUtilityCheckBoolDictEntry(openSetMemberKey))
                            {
                                openSetSemaphore.Wait();
                                openSet.Add(neighbour);
                                openSetSemaphore.Release();
                                neighbour.SetUtilityCheckBoolDictEntry(openSetMemberKey, true);
                            }
                        }
                        tileAvailabilitySemaphores[neighbour.taeID].Release();
                    }
                });
            }
            // don't go back to the top of the while loop until all tasks are done!
            // Debug.Log($"neighbourCheckTasks.Count == {neighbourCheckTasks.Count()}");
            Task.WaitAll(neighbourCheckTasks);
        }

        // stuff to fire if the destination can't be reached
        // Debug.Log("Can't get to " + target.tileLoc + " from " + start.tileLoc + "!");
        foreach (TileArrayEntry t in MapArrayScript.Instance.MapTileArray)
        {
            t.RemoveUtilityCheckBoolDictEntry(openSetMemberKey);
            t.RemoveUtilityCheckBoolDictEntry(tilesToTryMemberKey);
        }
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
