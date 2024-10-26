using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StartingUnitSpawner : MonoBehaviour
{
    private static StartingUnitSpawner instance;
    public static StartingUnitSpawner Instance { get { return instance; } }

    public Tilemap testedTilemap;
    public MapArrayScript mapArrayScript;
    public UnitSpawnerScript spawnerScript;
    public PlayerSetupScript playerSetupScript;

    // public GameObject ourGuy, ourGuy2;
    // private List<GameObject> _guys = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }
    public void StartingUnitSpawner_Initialise()
    {
        Dictionary<int, int> contiguousAreas 
            = MapArrayScript.Instance.GetContiguousPassableAreas(out Dictionary<int,int> areaSizes);
        List<int> spawningTileIDs = new();

        // Find the biggest contiguous area to spawn our guys in
        int maxArea = 0;
        int biggestAreaID = -1;
        foreach (int i in areaSizes.Keys)
        {
            if (areaSizes[i] > maxArea)
            {
                maxArea = areaSizes[i];
                biggestAreaID = i;
            }
        }

        // Spawning a default unit for each player
        foreach (PlayerProperties player in playerSetupScript.playerList) 
        {
            TileArrayEntry spawnTile;

            // find a free TileArrayEntry
            int loopBreaker = 0;
            while (true)
            {
                loopBreaker++;
                if (loopBreaker > 1000) 
                    { Debug.LogError("Could not find a place for " + player.playerName + "'s unit!"); break; }

                int randX = Random.Range(0, mapArrayScript.MapTileArray.GetLength(0));
                int randY = Random.Range(0, mapArrayScript.MapTileArray.GetLength(1));

                var tae = mapArrayScript.MapTileArray[randX, randY];
                if (tae.isPassable && !spawningTileIDs.Contains(tae.taeID) 
                    && contiguousAreas[tae.taeID] == biggestAreaID) 
                {
                    spawnTile = tae;
                    spawningTileIDs.Add(tae.taeID);
                    spawnerScript.DefaultUnitSpawn(player.playerID, spawnTile);
                    spawnerScript.DefaultUnitSpawn(player.playerID, spawnTile);
                    spawnerScript.DefaultUnitSpawn(player.playerID, spawnTile);
                    if (player.isHumanPlayer)
                        FindObjectOfType<CameraMovementScript>().SetCameraPosition(
                            tae.GetTileWorldLocation().x, tae.GetTileWorldLocation().y);
                    break; 
                }
            }
        }

        /* _guys.Add(ourGuy);
        _guys.Add(ourGuy2);

        // Get the example guys in position
        foreach (GameObject go in _guys)
        {
            Vector3Int ourGuysTileLocation = testedTilemap.WorldToCell(go.transform.position);
            Debug.Log("ourGuysTileLocation = " + ourGuysTileLocation);
            // Vector3 correctedPosition = testedTilemap.CellToWorld(ourGuysTileLocation);
            // Debug.Log("correctedPosition = " + correctedPosition);
            // ourGuy.transform.position = correctedPosition;

            // Dirtily assign him to his tile
            TileArrayEntry ourTileArrayEntry = mapArrayScript.GetTileArrayEntryAtLocationQuick(ourGuysTileLocation);
            ourTileArrayEntry.AssignTileContents(go);
            ourTileArrayEntry.InstantMoveContentsToTile();
        } */
    }
}
