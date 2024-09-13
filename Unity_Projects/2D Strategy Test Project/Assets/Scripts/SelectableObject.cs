using System.Collections;
using System.Collections.Generic;
// using UnityEditor.PackageManager;
using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    public LocatableObject locatableObject;

    private void Awake()
    {
        locatableObject = GetComponent<LocatableObject>();
    }

    /// <summary>
    /// Delegate for getting the SelectableObject to respond to an ActionClick at target while this is selected
    /// </summary>
    /// <param name="target"></param>
    public delegate void ActionClickBehaviour(TileArrayEntry target);
    public event ActionClickBehaviour ActionClickBehaviourFires;
    public void PreDestructionProtocols()
    {
        if (SelectorScript.Instance.selectedObject == this) SelectorScript.Instance.selectedObject = null;
    }
    public void ReceiveActionClick(TileArrayEntry targetTAE)
    {
        if (locatableObject.isUnit)
        {
            if (locatableObject.unitInfo == null) Debug.LogError("No unitInfo found!");
            if (locatableObject.unitInfo.ownerID == TurnManagerScript.Instance.CurrentPlayer.playerID)
            {
                ActionClickBehaviourFires(targetTAE);
                // Debug.Log("Action accepted!");
            }
            else if (TurnManagerScript.Instance.CurrentPlayer.isHumanPlayer) 
                Debug.Log("Unit does not belong to you!");
        }
    }
    public string ReturnSelectionInfo()
    {
        if (locatableObject.isUnit) return "Unit " + locatableObject.unitInfo.unitInfoID 
                + ", Player " + locatableObject.unitInfo.ownerID + " at " 
                + locatableObject.GetLocatableLocationTAE().TileLoc + ". HP: " 
                + locatableObject.unitInfo.hitpoints + ", Rd: " + 
                locatableObject.unitInfo.currentReadiness;
        else return "";
    }
}
