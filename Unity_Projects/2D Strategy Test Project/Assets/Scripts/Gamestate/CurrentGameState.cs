using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Threading;

/// <summary>
/// The point of this class is to provide a serialisable singleton that holds all game state information.
/// </summary>
public sealed class CurrentGameState
{
    // Singleton apparatus
    private static CurrentGameState instance = new CurrentGameState();
    static CurrentGameState() { }
    private CurrentGameState() { }
    public static CurrentGameState Instance {  get { return instance; } }

    // here is the GameStateInfo that actually holds all the data
    public GameStateData gameStateData = new GameStateData();

    public void NewGameStateData()
    {
        gameStateData = new GameStateData();
    }
    // we serialise and deserialise it to load and save
    // later will add proper save path functionality
    public void LoadGameStateInfo()
    {
        NewGameStateData();
        gameStateData = JsonConvert.DeserializeObject<GameStateData>(
            File.ReadAllText( @"D:\Users\User\The Fat Of The Land\Save Files\MonoSaveForTesting.json"));
    }
    public void SaveGame()
    {
        MouseBehaviourScript.Instance.preventMouseInteraction = true;
        while (AsyncThreadsManager.AreAsyncThreadsStillRunning()) Thread.Sleep(10);
        File.WriteAllText(@"D:\Users\User\The Fat Of The Land\Save Files\MonoSaveForTesting.json",
            JsonConvert.SerializeObject(gameStateData));
        MouseBehaviourScript.Instance.preventMouseInteraction = false;
    }
}
