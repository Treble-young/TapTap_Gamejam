using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [Header("Interactable Settings")]
    public Collider2D interactionArea;
    public PlayerType[] canInteractWithTypes;

    public virtual void Awake()
    {

    }
}
