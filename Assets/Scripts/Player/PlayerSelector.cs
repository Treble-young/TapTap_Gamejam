using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerSelector : MonoBehaviour
{
    public static PlayerSelector Instance;

    public int currentPlayerID = 0;
    public PlayerManager currentPlayer => playerManagers.Find(player => player.playerID == currentPlayerID);

    public PlayerInventoryManager currentInventory => currentPlayer != null ? currentPlayer.playerInventory : null;

    public PlayerManager GetMainPlayer()
    {
        return playerManagers.Find(player => player.playerType == PlayerType.Main);
    }

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
                if (player.canBeInlcudedInPlayerList)
                {
                    playerManagers.Add(player);
                }
            }
        }

        currentPlayerID = 0;
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
            if (player.playerState == PlayerState.Stranger)
                continue;

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

    public void UseCurrentSelectedItem()
    {
        if (currentInventory == null)
        {
            return;
        }

        InventoryItemData item = currentInventory.currentSelectedItem;
        if (item == null)
        {
            return;
        }

        item.Use(currentPlayer);

        currentInventory.NotifyChanged();
    }

    public void SelectNext()
    {
        if (playerManagers.Count == 0)
            return;

        for (int i = 1; i <= playerManagers.Count; i++)
        {
            int index = (currentPlayerID + i) % playerManagers.Count;
            if (playerManagers[index] != null && playerManagers[index].playerState != PlayerState.Stranger)
            {
                currentPlayerID = index;
                return;
            }
        }
    }

    public void SelectPrevious()
    {
        if (playerManagers.Count == 0)
            return;

        for (int i = 1; i <= playerManagers.Count; i++)
        {
            int index = ((currentPlayerID - i) % playerManagers.Count + playerManagers.Count) % playerManagers.Count;
            if (playerManagers[index] != null && playerManagers[index].playerState != PlayerState.Stranger)
            {
                currentPlayerID = index;
                return;
            }
        }
    }
}
