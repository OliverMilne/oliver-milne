using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// There should only be one of this, and it should be attached to the Canvas.
/// </summary>
public class UIControlScript : MonoBehaviour
{
    // pseudo-singleton apparatus
    private static UIControlScript instance;
    public static UIControlScript Instance { get { return instance; } }
    private void Awake()
    {
        // Debug.Log("UIControlScript is awake");
        if (instance == null && gameObject.name == "Canvas") instance = this;
        openPopupNames.CollectionChanged += MinimalPopupChangeHasOccurred;
    }

    // stuff it needs to control
    public GameObject InGameMenu;
    public ObservableCollection<string> openPopupNames = new ObservableCollection<string>();
    private Text _playerDetailsText;
    public bool popupIsOpen { get { return openPopupNames.Count != 0; } }
    private SelectionInfoDisplayScript _selectionInfoDisplayScript;

    public void UIControlScript_Initialise()
    {
        _playerDetailsText 
            = GameObject.Find("Player details placeholder UI Text").GetComponent<Text>();
        SelectedObjectDisplay();
        ShowPlayerDetails();
    }

    private void MinimalPopupChangeHasOccurred(object sender, NotifyCollectionChangedEventArgs e) 
    { 
        // Debug.Log("Popup Change Has Occurred");
    }
    public void SelectedObjectDisplay()
    {
        if (_selectionInfoDisplayScript == null)
            _selectionInfoDisplayScript = GameObject.Find("Selection Info Display")
            .GetComponent<SelectionInfoDisplayScript>();

        _selectionInfoDisplayScript.SelectedObject = SelectorScript.Instance.selectedObject;
    }
    public void ShowPlayerDetails() 
    {
        // get the current player from the TurnManager
        PlayerProperties currentPlayer = TurnManagerScript.Instance.CurrentPlayer;

        // if there is one, put their remaining actions in _playerDetailsText
        _playerDetailsText.text = currentPlayer.playerName + " playing. Remaining actions: "
            + currentPlayer.actions;
    }
}
