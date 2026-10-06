using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractionManager : MonoBehaviour
{
    PlayerManager player;

    [SerializeField] private List<InteractableManager> currentInteractableActions;

    void Awake()
    {
        player = GetComponent<PlayerManager>();
    }

    void Start()
    {
        currentInteractableActions = new List<InteractableManager>();
    }

    void FixedUpdate()
    {
        CheckForInteractable();
    }

    public void CheckForInteractable()
    {
        if (currentInteractableActions.Count == 0)
            return;

        if (currentInteractableActions[0] == null)
        {
            currentInteractableActions.RemoveAt(0);
            return;
        }
    }

    private void RefreshInteractableList()
    {
        for (int i = currentInteractableActions.Count - 1; i >= 0; i--)
        {
            if (currentInteractableActions[i] == null)
                currentInteractableActions.RemoveAt(i);
        }
    }

    /// <summary>与当前最近的交互物交互。</summary>
    /// <returns>是否存在可交互的物体（存在即消费这次按键，无论交互是否成功）。</returns>
    public bool Interact()
    {
        RefreshInteractableList();

        if (currentInteractableActions.Count == 0)
            return false;

        if (currentInteractableActions[0] != null)
            currentInteractableActions[0].Interact(player);

        return true;
    }

    public void AddInteractable(InteractableManager interactable)
    {
        RefreshInteractableList();

        if (!currentInteractableActions.Contains(interactable))
            currentInteractableActions.Add(interactable);
    }

    public void RemoveInteractable(InteractableManager interactable)
    {
        if (currentInteractableActions.Contains(interactable))
            currentInteractableActions.Remove(interactable);

        RefreshInteractableList();
    }
}
