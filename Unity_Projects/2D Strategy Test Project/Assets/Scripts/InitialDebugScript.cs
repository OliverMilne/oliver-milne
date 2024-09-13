using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class InitialDebugScript : MonoBehaviour
{
    private static InitialDebugScript instance;
    public static InitialDebugScript Instance { get { return instance; } }

    public Tilemap testedTilemap;
    public MapArrayScript mapArrayScript;
    public SpawnerScript spawnerScript;
    public PlayerSetupScript playerSetupScript;

    // public GameObject ourGuy, ourGuy2;
    // private List<GameObject> _guys = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }
    public void InitialDebugScript_Initialise()
    {
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
                if (tae.isPassable) 
                { 
                    spawnTile = tae;
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
