using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractionManager : MonoBehaviour
{
    PlayerManager player;

    public List<InteractableManager> currentInteractableActions;

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
        UpdateInteractableHighlights();
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
        // 清理已销毁（为 null）的交互物
        currentInteractableActions.RemoveAll(interactable => interactable == null);

        // 将拾取物品类交互物移到列表最前面，保证其优先被交互
        List<InteractableManager> pickups = new List<InteractableManager>();
        List<InteractableManager> others = new List<InteractableManager>();

        foreach (InteractableManager interactable in currentInteractableActions)
        {
            if (interactable is WorldPickUpItemInteractableManager)
                pickups.Add(interactable);
            else
                others.Add(interactable);
        }

        pickups.AddRange(others);
        currentInteractableActions = pickups;
    }

    private InteractableManager GetPrimaryInteractable()
    {
        foreach (InteractableManager interactable in currentInteractableActions)
        {
            if (interactable != null && interactable.CanInteractWith(player))
                return interactable;
        }

        return null;
    }

    private void UpdateInteractableHighlights()
    {
        InteractableManager primary = GetPrimaryInteractable();

        foreach (InteractableManager interactable in currentInteractableActions)
        {
            if (interactable == null)
                continue;

            interactable.SetOutlineVisible(interactable == primary);
        }
    }

    /// <summary>与当前最近的交互物交互。</summary>
    /// <returns>是否存在可交互的物体（存在即消费这次按键，无论交互是否成功）。</returns>
    public bool Interact()
    {
        RefreshInteractableList();

        if (currentInteractableActions.Count == 0)
            return false;

        InteractableManager primary = GetPrimaryInteractable();
        if (primary == null)
            return false;

        primary.Interact(player);
        return true;
    }

    public void AddInteractable(InteractableManager interactable)
    {
        RefreshInteractableList();

        if (!currentInteractableActions.Contains(interactable))
            currentInteractableActions.Add(interactable);

        UpdateInteractableHighlights();
    }

    public void RemoveInteractable(InteractableManager interactable)
    {
        if (currentInteractableActions.Contains(interactable))
            currentInteractableActions.Remove(interactable);

        if (interactable != null)
            interactable.SetOutlineVisible(false);

        RefreshInteractableList();
        UpdateInteractableHighlights();
    }
}
