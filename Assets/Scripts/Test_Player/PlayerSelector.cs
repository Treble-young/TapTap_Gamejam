using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerSelector : MonoBehaviour
{
    public static PlayerSelector Instance;

    public int currentPlayerID = 0;
    public PlayerManager currentPlayer => playerManagers.Find(player => player.playerID == currentPlayerID);

    private int _lastCurrentPlayerID = -1;

    [SerializeField] private LightFollow _followLight;

    public List<PlayerManager> playerManagers = new List<PlayerManager>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

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

        if (currentPlayer != null && PlayerInputManager.Instance != null && PlayerInputManager.Instance.followCamera != null)
        {
            PlayerInputManager.Instance.followCamera.Follow = currentPlayer.transform;
        }

        if (_followLight != null && currentPlayer != null)
        {
            _followLight.target = currentPlayer.transform;
        }
    }
}
