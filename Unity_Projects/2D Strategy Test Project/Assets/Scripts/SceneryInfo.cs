using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneryInfo : MonoBehaviour
{
    public int locatableID { get; private set; }
    public string sceneryType
    {
        get => CurrentGameState.Instance.gameStateInfo.sceneryDataDict[locatableID].sceneryType;
        set { CurrentGameState.Instance.gameStateInfo.sceneryDataDict[locatableID].sceneryType = value; }
    }
    public HexDir sceneryDirection
    {
        get => CurrentGameState.Instance.gameStateInfo.sceneryDataDict[locatableID].sceneryDirection; 
        set { CurrentGameState.Instance.gameStateInfo.sceneryDataDict[locatableID].sceneryDirection = value; }
    }

    private void Awake()
    {
        locatableID = GetComponent<LocatableObject>().locatableID;
        if (!CurrentGameState.Instance.gameStateInfo.sceneryDataDict.ContainsKey(locatableID))
            CurrentGameState.Instance.gameStateInfo.sceneryDataDict[locatableID] = new SceneryData();
    }
    public void PreDestructionProtocols()
    {
        CurrentGameState.Instance.gameStateInfo.sceneryDataDict.Remove(locatableID);
    }
}

public class SceneryData
{
    public string sceneryType;
    public HexDir sceneryDirection;
}