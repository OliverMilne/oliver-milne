using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
    public GameStateInfo gameStateInfo = new GameStateInfo();

    public void NewGameStateInfo()
    {
        gameStateInfo = new GameStateInfo();
    }
    // we serialise and deserialise it to load and save
    // later will add proper save path functionality
    public void LoadGameStateInfo()
    {
        NewGameStateInfo();
        gameStateInfo = JsonConvert.DeserializeObject<GameStateInfo>(
            File.ReadAllText( @"D:\Users\User\The Fat Of The Land\Save Files\MonoSaveForTesting.json"));
    }
    public void SaveGame()
    {
        File.WriteAllText(@"D:\Users\User\The Fat Of The Land\Save Files\MonoSaveForTesting.json",
            JsonConvert.SerializeObject(gameStateInfo));
    }
}
