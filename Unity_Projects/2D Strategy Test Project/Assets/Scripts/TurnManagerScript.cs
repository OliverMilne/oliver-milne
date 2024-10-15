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
        get => CurrentGameState.Instance.gameStateData.turnData["TurnCounter"];  
        private set 
        { CurrentGameState.Instance.gameStateData.turnData["TurnCounter"] = value; }
    }
    private int _currentPlayerIndex  // index of the current player in _players
    {
        get => CurrentGameState.Instance.gameStateData.turnData["currentPlayerIndex"];
        set
        {
            try
            {
                CurrentGameState.Instance.gameStateData.turnData["previousPlayerIndex"] = _currentPlayerIndex;
            }
            catch { }
            CurrentGameState.Instance.gameStateData.turnData["currentPlayerIndex"] = value;
        }
    }
    private int _previousPlayerIndex
    {
        get => CurrentGameState.Instance.gameStateData.turnData["previousPlayerIndex"];
        set
        {
            CurrentGameState.Instance.gameStateData.turnData["previousPlayerIndex"] = value;
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
    public bool EndTurnOnUpdate
    {
        get 
        {
            if (!CurrentGameState.Instance.gameStateData.turnData.ContainsKey("EndTurnOnUpdate"))
            {
                CurrentGameState.Instance.gameStateData.turnData["EndTurnOnUpdate"] = 0;
                return false;
            }
            else if (CurrentGameState.Instance.gameStateData.turnData["EndTurnOnUpdate"] == 0) return false;
            else return true;
        }
        set 
        { 
            if (value) CurrentGameState.Instance.gameStateData.turnData["EndTurnOnUpdate"] = 1;
            else CurrentGameState.Instance.gameStateData.turnData["EndTurnOnUpdate"] = 0;
        }
    }

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
        StartCoroutine(PlayerAIMasterScript.Instance.PlayerTurnAiCaller());
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
        UIControlScript.Instance.ShowPlayerDetails();
        // Debug.Log("OnStartTurn fired. Current player: " + CurrentPlayer.playerName);
    }

    public void StartFirstTurn() { OnStartTurn(); }
    public void Update()
    {
        if (EndTurnOnUpdate) { EndTurnOnUpdate = false; EndTurn(); }
    }
}
