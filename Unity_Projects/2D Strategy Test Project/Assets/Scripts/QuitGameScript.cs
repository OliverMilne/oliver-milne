using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGameScript : MonoBehaviour
{
    public void OnApplicationQuit()
    {
        LocatableObject.WipeAllLocatableObjectsAndReset();
    }
}
