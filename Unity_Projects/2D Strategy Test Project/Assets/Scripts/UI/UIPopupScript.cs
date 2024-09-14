using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPopupScript : MonoBehaviour
{
    private void OnEnable()
    {
        if (UIControlScript.Instance != null) UIControlScript.Instance.openPopupNames.Add(name);
        else FindObjectOfType<UIControlScript>().openPopupNames.Add(name); 
        /*string openPopupString = "";
        foreach (string s in FindObjectOfType<UIControlScript>().openPopupNames) 
            openPopupString += s + ", ";
        Debug.Log(name + " enabled. Open popups: " + openPopupString);*/
    }
    private void OnDisable()
    {
        UIControlScript.Instance.openPopupNames.Remove(name);
        /*string openPopupString = "";
        foreach (string s in UIControlScript.Instance.openPopupNames) openPopupString += s + ", ";
        Debug.Log(name + " disabled. Open popups: " + openPopupString);*/
    }
}
