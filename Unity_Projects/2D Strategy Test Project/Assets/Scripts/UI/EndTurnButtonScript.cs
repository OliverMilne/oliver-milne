using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTurnButtonScript : MonoBehaviour
{
    // doing it this ugly way because the timings don't work out to use the Instances
    private InitialiserScript _initialiserScript;
    private TurnManagerScript _turnManagerScript;
    private void Awake()
    {
        _initialiserScript = FindObjectOfType<InitialiserScript>();
        _turnManagerScript = FindObjectOfType<TurnManagerScript>();

        _initialiserScript.OnLoadGame += UpdateButtonText;
        _turnManagerScript.OnStartTurn += UpdateButtonText;
    }

    public void OnButtonPressed()
    {
        // Debug.Log("End Turn button pressed");
        if (_turnManagerScript.CurrentPlayer.isHumanPlayer)
            _turnManagerScript.EndTurn();
    }
    private void UpdateButtonText() 
    {
        Transform child = transform.GetChild(0);
        Text buttonText = child.GetComponent<Text>();
        buttonText.text = "End " + _turnManagerScript.CurrentPlayer.playerName 
            + "'s Turn " + _turnManagerScript.TurnCounter;
        buttonText.color = _turnManagerScript.CurrentPlayer.playerColor;
    }
}
