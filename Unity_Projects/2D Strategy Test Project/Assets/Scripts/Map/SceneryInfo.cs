using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneryInfo : MonoBehaviour
{
    public int locatableID { get; private set; }
    public SceneryType sceneryType
    {
        get => CurrentGameState.Instance.gameStateData.sceneryDataDict[locatableID].sceneryType;
        set { CurrentGameState.Instance.gameStateData.sceneryDataDict[locatableID].sceneryType = value; }
    }
    public HexDir sceneryDirection
    {
        get => CurrentGameState.Instance.gameStateData.sceneryDataDict[locatableID].sceneryDirection; 
        set { CurrentGameState.Instance.gameStateData.sceneryDataDict[locatableID].sceneryDirection = value; }
    }

    private void Awake()
    {
        locatableID = GetComponent<LocatableObject>().locatableID;
        if (!CurrentGameState.Instance.gameStateData.sceneryDataDict.ContainsKey(locatableID))
            CurrentGameState.Instance.gameStateData.sceneryDataDict[locatableID] = new SceneryData();
    }
    public void PreDestructionProtocols()
    {
        CurrentGameState.Instance.gameStateData.sceneryDataDict.Remove(locatableID);
    }
}

public class SceneryData
{
    public SceneryType sceneryType = SceneryType.NULL;
    public HexDir sceneryDirection = HexDir.W;
}
