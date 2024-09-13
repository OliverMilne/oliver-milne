using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class VictoryConditions : MonoBehaviour
{
    private static VictoryConditions instance;
    public static VictoryConditions Instance { get { return instance; } }

    public GameObject GameOverPopup;
    private bool _victoryConditionsEventsSubscribed = false;

    void Awake()
    {
        if (instance == null) { instance = this; }
    }
    public void VictoryConditions_Initialise()
    {
        GameOverPopup.SetActive(false);
        if (!_victoryConditionsEventsSubscribed)
        { 
            LocatableObject.LocatableObjectDestroyed += ExterminationVictoryCheck;
            _victoryConditionsEventsSubscribed = true;
        }
    }

    void ExterminationVictoryCheck()
    {
        List<PlayerProperties> survivingPlayers = new List<PlayerProperties>();
        foreach (var pair in PlayerProperties.playersById)
        {
            if (pair.Value.ownedObjectIds.Count > 0)
                survivingPlayers.Add(pair.Value);
        }
        if (survivingPlayers.Count == 0) throw new System.Exception("No surviving players!");
        if (survivingPlayers.Count == 1) DeliverVictory(survivingPlayers.First());
    }

    void DeliverVictory(PlayerProperties winner)
    {
        GameOverPopup.transform.Find("Victory or Defeat Text").GetComponent<Text>().text 
            = winner.playerName + " wins!";
        GameOverPopup.SetActive(true);
    }
}
