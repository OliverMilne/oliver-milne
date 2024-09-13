using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        List<TileArrayEntry> highlightTiles = GetLocatable1TurnReachableTiles(unit, maxMoveDistance, true);
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
    public List<TileArrayEntry> GetLocatable1TurnReachableTiles(
        LocatableObject unitLoco, int maxMoveDistance, bool accountForVisibility = true)
    {
        UnitInfo unitInfo = unitLoco.GetComponent<UnitInfo>();

        List<TileArrayEntry> reachableTiles = new List<TileArrayEntry>();
        TileArrayEntry unitLocation = unitLoco.GetLocatableLocationTAE();
        reachableTiles.Add(unitLocation);

        List<TileArrayEntry> tilesToAdd = new List<TileArrayEntry>();
        int distanceTravelled = 0;
        while (distanceTravelled < maxMoveDistance)
        {
            tilesToAdd.Clear();
            foreach(TileArrayEntry tile in reachableTiles)
            {
                foreach (TileArrayEntry nextTile in tile.GetAccessibleTAEs())
                {
                    // see if it's in reachableTiles or tilesToAdd
                    if (nextTile != null)
                    {
                        if (reachableTiles.TrueForAll(x => x.taeID != nextTile.taeID)
                            && tilesToAdd.TrueForAll(x => x.taeID != nextTile.taeID))
                            // if not, add it to tilesToAdd
                            tilesToAdd.Add(nextTile);
                    }
                }
                // if it's for display, add adjacent hidden tiles
                if (accountForVisibility)
                {
                    foreach (TileArrayEntry nextTile in tile.GetAdjacentTAEs())
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (nextTile != null 
                            && nextTile.visibilityByPlayerID[unitInfo.ownerID] 
                            == TileVisibility.Hidden
                            && !(nextTile.forceVisible 
                            && unitInfo.ownerID == PlayerProperties.humanPlayerID))
                        {
                            if (reachableTiles.TrueForAll(x => x.taeID != nextTile.taeID)
                                && tilesToAdd.TrueForAll(x => x.taeID != nextTile.taeID))
                                // if not, add it to tilesToAdd
                                tilesToAdd.Add(nextTile);
                        }
                    }
                }
            }
            reachableTiles.AddRange(tilesToAdd);
            distanceTravelled++;
        }
        return reachableTiles;
    }
    public List<TileArrayEntry> GetLocation1TurnReachableTiles(
        Vector3Int targetLocation, int maxMoveDistance, int visibilityPlayerID, 
        bool accountForVisibility = true)
    {
        List<TileArrayEntry> reachableTiles = new List<TileArrayEntry>();
        TileArrayEntry unitLocation 
            = TileFinders.Instance.GetTileArrayEntryAtLocationQuick(targetLocation);
        reachableTiles.Add(unitLocation);

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
                        if (reachableTiles.TrueForAll(x => x.taeID != nextTile.taeID)
                            && tilesToAdd.TrueForAll(x => x.taeID != nextTile.taeID))
                            // if not, add it to tilesToAdd
                            tilesToAdd.Add(nextTile);
                    }
                }
                // if it's for display, add adjacent hidden tiles
                if (accountForVisibility)
                {
                    foreach (TileArrayEntry nextTile in tile.GetAdjacentTAEs())
                    {
                        // see if it's in reachableTiles or tilesToAdd
                        if (nextTile != null
                            && nextTile.visibilityByPlayerID[visibilityPlayerID]
                            == TileVisibility.Hidden
                            && !(nextTile.forceVisible
                            && visibilityPlayerID == PlayerProperties.humanPlayerID))
                        {
                            if (reachableTiles.TrueForAll(x => x.taeID != nextTile.taeID)
                                && tilesToAdd.TrueForAll(x => x.taeID != nextTile.taeID))
                                // if not, add it to tilesToAdd
                                tilesToAdd.Add(nextTile);
                        }
                    }
                }
            }
            reachableTiles.AddRange(tilesToAdd);
            distanceTravelled++;
        }
        return reachableTiles;
    }
    public bool MoveUnitDefault(LocatableObject unit, TileArrayEntry target, int maxMoveDistance)
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
            StartCoroutine(MoveAlongPath(unit, movementPath, maxMoveDistance));
            return true;
        }
        else return false;
    }
    public List<GameObject> MoveUnitPreview(LocatableObject unit, TileArrayEntry target, int maxMoveDistance)
    {
        List<GameObject> previewMarkers = new List<GameObject>();

        // improving performance on visible impassable tiles
        if (!target.isPassable 
            && (target.visibilityByPlayerID[PlayerProperties.humanPlayerID] != TileVisibility.Hidden 
            || target.forceVisible)) 
            return previewMarkers;
        List<TileArrayEntry> movementPath = AStarPathCalculator(unit.GetLocatableLocationTAE(), target,
            PlayerProperties.humanPlayerID, true);
        
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
    
    // static movement calculation stuff
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
    public static List<TileArrayEntry> AStarPathCalculator(
        TileArrayEntry start, TileArrayEntry target, int visibilityPlayerID, 
        bool accountForVisibility = true)
    {
        List<TileArrayEntry> openSet = new List<TileArrayEntry>();
        openSet.Add(start);

        Dictionary<int, TileArrayEntry> cameFrom = new Dictionary<int, TileArrayEntry>();
        Dictionary<int, int> leastStepsFromStart = new Dictionary<int, int>();
        Dictionary<int, int> estdStepsViaHere = new Dictionary<int, int>();

        // Debug.Log("Beginning leastStepsFromStart dictionary assembly");
        foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
        {
            leastStepsFromStart[tae.taeID] = int.MaxValue/2;
        }
        leastStepsFromStart[start.taeID] = 0;
        // Debug.Log("leastStepsFromStart[start] = " + leastStepsFromStart[start]);

        // Debug.Log("Beginning estdStepsViaHere dictionary assembly");
        foreach (TileArrayEntry tae in MapArrayScript.Instance.MapTileArray)
        {
            estdStepsViaHere[tae.taeID] 
                = leastStepsFromStart[tae.taeID] + DistanceEstimate(tae, target);
        }
        // Debug.Log("Estd distance to target: " + DistanceEstimate(start, target));

        TileArrayEntry current = start;
        // Debug.Log("openSet.Count = " + openSet.Count);

        int iterCount = 0;
        
        while (openSet.Count > 0)
        {
            // Debug.Log("Beginning A* iteration " + iterCount);
            iterCount++;
            if (iterCount > 2000) { Debug.Log("A* while loop broken"); break; }
            string estdStepsList = "";
            foreach (var x in estdStepsViaHere.Keys) estdStepsList += x + ", ";

            current = openSet[0];
            if (!estdStepsViaHere.ContainsKey(current.taeID)) 
                Debug.LogError("Could not find current.taeID " + current.taeID 
                    + " in estdStepsViaHere, which contains: " 
                    + estdStepsList);
            foreach (TileArrayEntry tae in openSet) 
            {
                if (!estdStepsViaHere.ContainsKey(tae.taeID))
                    Debug.LogError("Could not find tae.taeID " + tae.taeID
                        + " in estdStepsViaHere, which contains: " 
                        + estdStepsList);
                // Debug.Log("estdStepsViaHere " + tae.tileLoc + ": " + estdStepsViaHere[tae]);
                if (estdStepsViaHere[tae.taeID] < estdStepsViaHere[current.taeID])
                {
                    current = tae;
                }
            }
            if (current.taeID == target.taeID) return ReconstructPath(cameFrom, target);

            /* Debug.Log("Iteration count " + iterCount + ", current tileLoc = " + current.tileLoc + 
                "; openSet.Count after culling current = " + openSet.Count);*/
            openSet = openSet.Where(x => x.taeID != current.taeID).ToList();

            // get different lists of neighbours depending on whether canMislead is true
            List<TileArrayEntry> validNeighbours = current.GetAccessibleTAEs();
            if (accountForVisibility) validNeighbours.AddRange(
                current.GetAdjacentTAEs().Where(
                    x => x.visibilityByPlayerID[visibilityPlayerID] == TileVisibility.Hidden 
                    && !x.forceVisible
                    && !(current.hasCliffsByDirection[
                        current.AdjacentTileLocsByDirection.Where(
                            y => y.Value == x.TileLoc).ToList().First().Key]
                            && current.visibilityByPlayerID[visibilityPlayerID] != TileVisibility.Hidden)
                    ).ToList());

            foreach (TileArrayEntry neighbour in validNeighbours)
            {
                // when there are tile weights, substitute that here
                int tentativeGScore = leastStepsFromStart[current.taeID] + 1; 

                if (tentativeGScore < leastStepsFromStart[neighbour.taeID])
                {
                    cameFrom[neighbour.taeID] = current;
                    leastStepsFromStart[neighbour.taeID] = tentativeGScore;
                    estdStepsViaHere[neighbour.taeID] 
                        = tentativeGScore + DistanceEstimate(neighbour, target);
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
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
        return Mathf.Abs(origin.TileLoc.x - target.TileLoc.x) + Mathf.Abs(origin.TileLoc.y - target.TileLoc.y);
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
}
