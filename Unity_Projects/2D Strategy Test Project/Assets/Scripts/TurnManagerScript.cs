using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnManagerScript : MonoBehaviour
{
    // pseudo-singleton functionality
    private static TurnManagerScript instance;
    public static TurnManagerScript Instance { get { return instance; } }
    private void Awake() { if (instance == null) instance = this; }

    // stuff from the CurrentGameState
    public int TurnCounter 
    { 
        get => CurrentGameState.Instance.gameStateInfo.turnInfo["TurnCounter"];  
        private set 
        { CurrentGameState.Instance.gameStateInfo.turnInfo["TurnCounter"] = value; }
    }
    private int _currentPlayerIndex  // index of the current player in _players
    {
        get => CurrentGameState.Instance.gameStateInfo.turnInfo["currentPlayerIndex"];
        set
        {
            try
            {
                CurrentGameState.Instance.gameStateInfo.turnInfo["previousPlayerIndex"] = _currentPlayerIndex;
            }
            catch { }
            CurrentGameState.Instance.gameStateInfo.turnInfo["currentPlayerIndex"] = value;
        }
    }
    private int _previousPlayerIndex
    {
        get => CurrentGameState.Instance.gameStateInfo.turnInfo["previousPlayerIndex"];
        set
        {
            CurrentGameState.Instance.gameStateInfo.turnInfo["previousPlayerIndex"] = value;
        }
    }

    // derived stuff
    public PlayerProperties PreviousPlayer 
    { 
        get => PlayerSetupScript.Instance.playerList[_previousPlayerIndex]; 
    }
    public PlayerProperties CurrentPlayer 
    { 
        get => PlayerSetupScript.Instance.playerList[_currentPlayerIndex];
    }

    // internal functionality stuff
    private bool _hasInitialised = false;

    // Unity interface hookups
    public GameObject EndTurnButtonText;

    // callbacks
    public delegate void OnEndTurnCallback();
    public event OnEndTurnCallback OnEndTurn;
    public delegate void OnStartTurnCallback();
    public event OnStartTurnCallback OnStartTurn;

    public void TurnManagerScript_Initialise()
    {
        TurnCounter = 1;
        _currentPlayerIndex = 0;
        if (!_hasInitialised)
        {
            OnStartTurn += MinimalStartTurn;
            OnEndTurn += MinimalEndTurn;
            _hasInitialised = true;
        }
    }
    public void TurnManagerScript_InitialiseFromGameStateInfo()
    {
        if (!_hasInitialised)
        {
            OnStartTurn += MinimalStartTurn;
            OnEndTurn += MinimalEndTurn;
            _hasInitialised = true;
        }
    }
    public void EndTurn() 
    {
        OnEndTurn();

        // move on to next player in _players; if that's player 0, increment turn counter
        _currentPlayerIndex = (_currentPlayerIndex + 1) % PlayerSetupScript.Instance.playerList.Count;
        if (_currentPlayerIndex == 0) TurnCounter++;

        OnStartTurn();
        PlayerAIMasterScript.Instance.PlayerTurnAiCaller();
    }

    // only stuff that should happen every OnEndTurn
    private void MinimalEndTurn() 
    {
        OverlayGraphicsScript.Instance.DrawSelectionGraphics(SelectorScript.Instance.selectedObject);
    }

    // only stuff that should happen every OnStartTurn
    private void MinimalStartTurn()
    {
        OverlayGraphicsScript.Instance.DrawSelectionGraphics(SelectorScript.Instance.selectedObject);
        // Debug.Log("OnStartTurn fired. Current player: " + CurrentPlayer.playerName);
    }

    public void StartFirstTurn() { OnStartTurn(); }
}
