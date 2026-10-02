using System.Collections.Generic;
using UnityEngine;

public class PlayerSelector : MonoBehaviour
{
    public int currentPlayerID = 0;
    private int _lastCurrentPlayerID = -1;

    public List<PlayerManager> playerManagers = new List<PlayerManager>();

    void Awake()
    {
        PlayerManager[] players = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
        foreach (PlayerManager player in players)
        {
            if (!playerManagers.Contains(player))
            {
                playerManagers.Add(player);
            }
        }

        foreach (PlayerManager player in playerManagers)
        {
            player.playerID = playerManagers.IndexOf(player);
        }
    }

    void Start()
    {

    }

    void Update()
    {
        UpdatePlayerStates();
    }

    private void UpdatePlayerStates()
    {
        if (currentPlayerID == _lastCurrentPlayerID)
            return;

        _lastCurrentPlayerID = currentPlayerID;

        PlayerManager currentPlayer = null;

        foreach (PlayerManager player in playerManagers)
        {
            bool isCurrent = player.playerID == currentPlayerID;
            player.playerState = isCurrent ? PlayerState.InputControlling : PlayerState.AutoMoving;
            if (isCurrent)
                currentPlayer = player;
        }
    }
}
