using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderIllustrator : MonoBehaviour
{
    /// <summary>
    /// This finds the borders of a given area and draws borders on them.
    /// </summary>
    // public GameObject edgeMarkerPrefab;
    // public GameObject lowerCornerMarkerPrefab;
    // public GameObject upperCornerMarkerPrefab;

    public static List<GameObject> DrawBorders(
        List<TileArrayEntry> area,
        GameObject edgeMarkerPrefab,
        GameObject lowerCornerMarkerPrefab,
        GameObject upperCornerMarkerPrefab)
    {
        // Debug.Log("BorderIllustrator: DrawBorders called");
        List<TileArrayEntry> borders = GetBorderTiles(area);
        List<GameObject> borderMarkers = new List<GameObject>();

        /// Process:
        /// Per border tile, identify neighbours outside of area
        /// Identify direction of each such neighbour using Quaternion.FromToRotation
        /// Draw an edge marker for each one at the correct angle

        foreach (TileArrayEntry borderTile in borders)
        {
            HexDir clockwiseMostNullTileOrderKey;
            HexDir anticlockwiseMostNullTileOrderKey;
            List<HexDir> remainingOrderKeys = new List<HexDir> 
            { 
                HexDir.W, HexDir.NW, HexDir.NE, HexDir.E, HexDir.SE, HexDir.SW 
            };

            foreach (TileArrayEntry neighbour in borderTile.GetAdjacentTAEs()) 
            {
                
                Vector3 directionVector = neighbour.GetTileWorldLocation() - borderTile.GetTileWorldLocation();
                HexDir orderKey = borderTile.GetOrientationKey(neighbour);
                remainingOrderKeys.Remove(orderKey);

                // draw borders on neighbouring edges with map edges
                if (borderTile.GetAnticlockwiseTile(neighbour) == null)
                {
                    clockwiseMostNullTileOrderKey = HexOrientation.Anticlockwise1(orderKey);
                    remainingOrderKeys.Remove(clockwiseMostNullTileOrderKey);
                    Quaternion orientation = HexOrientation.HexDirToRotation(clockwiseMostNullTileOrderKey);

                    GameObject mapBorderAnticlockwiseMarker = Instantiate(
                        edgeMarkerPrefab,
                        MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                        orientation);
                    borderMarkers.Add(mapBorderAnticlockwiseMarker);

                    if (!area.TrueForAll(x => x.taeID != neighbour.taeID))
                    {
                        GameObject mapBorderUpperCornerMarker = Instantiate(
                            upperCornerMarkerPrefab,
                            MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                            orientation);
                        borderMarkers.Add(mapBorderUpperCornerMarker);
                    }
                }

                if (borderTile.GetClockwiseTile(neighbour) == null)
                {
                    anticlockwiseMostNullTileOrderKey = HexOrientation.Clockwise1(orderKey);
                    remainingOrderKeys.Remove(anticlockwiseMostNullTileOrderKey);
                    Quaternion orientation = HexOrientation.HexDirToRotation(anticlockwiseMostNullTileOrderKey);

                    GameObject mapBorderClockwiseMarker = Instantiate(
                        edgeMarkerPrefab,
                        MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                        orientation);
                    borderMarkers.Add(mapBorderClockwiseMarker);

                    if (!area.TrueForAll(x => x.taeID != neighbour.taeID))
                    {
                        GameObject mapBorderLowerCornerMarker = Instantiate(
                            lowerCornerMarkerPrefab,
                            MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                            orientation);
                        borderMarkers.Add(mapBorderLowerCornerMarker);
                    }
                }

                // check neighbour is outside area
                if (!area.TrueForAll(x => x.taeID != neighbour.taeID)) continue;

                // draw edges
                GameObject marker = Instantiate(
                    edgeMarkerPrefab,
                    MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                    Quaternion.FromToRotation(new Vector3(-1,0,0), directionVector));
                borderMarkers.Add(marker);
                // Debug.Log("Drawn marker at " + entry.tileLoc);

                // draw corners
                if (borderTile.GetAnticlockwiseTile(neighbour) != null)
                {
                    if (!area.TrueForAll(x => x.taeID != borderTile.GetAnticlockwiseTile(neighbour).taeID))
                    {
                        Quaternion orientation = HexOrientation.HexDirToRotation(orderKey);

                        GameObject upperCornerMarker = Instantiate(
                            lowerCornerMarkerPrefab,
                            MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                            orientation);
                        borderMarkers.Add(upperCornerMarker);
                    }
                }
                
                if (borderTile.GetClockwiseTile(neighbour) != null)
                {
                    if (!area.TrueForAll(x => x.taeID != borderTile.GetClockwiseTile(neighbour).taeID))
                    {
                        Quaternion orientation = HexOrientation.HexDirToRotation(orderKey);

                        GameObject lowerCornerMarker = Instantiate(
                            upperCornerMarkerPrefab,
                            MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                            orientation);
                        borderMarkers.Add(lowerCornerMarker);
                    }
                }
            }

            // draw borders on map edges with no neighbours
            foreach (HexDir i in remainingOrderKeys) 
            {
                GameObject marker = Instantiate(
                    edgeMarkerPrefab,
                    MapArrayScript.Instance.tilemap.CellToWorld(borderTile.TileLoc),
                    HexOrientation.HexDirToRotation(i));
                borderMarkers.Add(marker);
            }
        }
        return borderMarkers;
    }

    private static List<TileArrayEntry> GetBorderTiles(List<TileArrayEntry> areaTiles)
    {
        // Debug.Log("BorderIllustrator: GetBorderTiles called");
        List<TileArrayEntry> borderTiles = new List<TileArrayEntry>();

        foreach (var tile in areaTiles)
        {
            if (tile.GetAdjacentTAEs().Count < 6)
            {
                borderTiles.Add(tile);
                // Debug.Log("Added tile " + tile.tileLoc + " to borderTiles");
                continue;
            }
            foreach (TileArrayEntry neighbour in tile.GetAdjacentTAEs())
            {
                if (areaTiles.TrueForAll(x => x.taeID != neighbour.taeID))
                {
                    borderTiles.Add(tile);
                    // Debug.Log("Added tile " + tile.tileLoc + " to borderTiles");
                    break;
                }
            }
        }

        return borderTiles;
    }
}
