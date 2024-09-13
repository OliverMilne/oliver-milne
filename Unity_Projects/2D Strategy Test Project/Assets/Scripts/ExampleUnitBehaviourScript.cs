using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleUnitBehaviourScript : MonoBehaviour
{
    private int _moveDistance = 5;
    private int _actions = 1; // later will do more advanced things with move limits but for now keep it simple
    private int _maxActions = 1;
    public int owningPlayerID;

    private void Awake()
    {
        // Debug.Log(name + "'s ExampleUnitBehaviourScript is awake!");
        // GetComponent<LocatableObject>().actionClickBehaviour += Move;
        // FindObjectOfType<TurnManagerScript>().OnStartTurn += StartTurnBehaviour;
    }

    private Dictionary<string, int> UnitStats() // later will make a custom format for this
    {
        return new Dictionary<string, int> 
        { 
            ["Movement"] = _moveDistance, 
            ["Actions"] = _actions, 
            ["Max Actions"] = _maxActions,
            ["Owner"] = owningPlayerID,
        };
    }

    private void StartTurnBehaviour()
    {
        _actions = _maxActions;
    }

    private void Move(TileArrayEntry t)
    {
        /*bool couldMove = false;
        foreach (TileArrayEntry tae in GetComponent<LocatableObject>().AssignedTileArrayEntries) 
        {
            if (tae.TileIsAccessibleFromHere(t) && t.isPassable)
            {
                if (t.tileContents.Count > 0)
                {
                    foreach (var guy in t.tileContents)
                    {
                        GameObject.Destroy(guy.gameObject);
                    }
                }
                GetComponent<LocatableObject>().DebugMoveToTile(t);
                couldMove = true;
                break;
            } 
        }
        if (couldMove == false)
        {
            Debug.Log(gameObject + " cannot move to tile " + t.tileLoc + "!");
        }*/

        if (_actions > 0)
        {
            UnitMovement movementScript = FindObjectOfType<UnitMovement>();
            // Debug.Log("UnitMovement found");
            movementScript.MoveUnitDefault(GetComponent<LocatableObject>(), t, _moveDistance);
            _actions--;
        }
        else Debug.Log("No actions remaining!");
    }
}
