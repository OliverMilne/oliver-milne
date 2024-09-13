using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIControlScript : MonoBehaviour
{
    // pseudo-singleton apparatus
    private static UIControlScript instance;
    public static UIControlScript Instance { get { return instance; } }
    private void Awake()
    {
        if (instance == null) instance = this;
    }

    // stuff it needs to control
    private Text _playerDetailsText;
    private SelectionInfoDisplayScript _selectionInfoDisplayScript;

    public void UIControlScript_Initialise()
    {
        _playerDetailsText = GameObject.Find("Player details placeholder UI Text").GetComponent<Text>();
        SelectedObjectDisplay();
        ShowPlayerDetails();
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
