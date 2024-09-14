using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UIElements;

public class UIFixedButtonScript : MonoBehaviour
{
    private UIControlScript _uiControlScript
    {
        get 
        { 
            try { return UIControlScript.Instance; }
            catch 
            {
                Debug.Log("Couldn't fine UIControlScript.Instance");
                return FindObjectOfType<UIControlScript>(); 
            } 
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        _uiControlScript.openPopupNames.CollectionChanged
            += CheckForUIButtonsDisabled;
        CheckForUIButtonsDisabled(null, 
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void CheckForUIButtonsDisabled(object sender, NotifyCollectionChangedEventArgs e)
    {
        // Debug.Log(name + " checking for UI Buttons Disabled");
        if (_uiControlScript.popupIsOpen) 
            GetComponent<UnityEngine.UI.Button>().enabled = false;
        else GetComponent<UnityEngine.UI.Button>().enabled = true;
    }
}
