using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RollingDoor : InteractableManager
{
    [Header("Hand Crank Settings")]
    public GameObject handCrankPoint;
    public bool isControlledByHandCrank = false;

    [Header("Door Settings")]
    public bool isBlockedByShoppingCart = false;
    public HandCrank handCrank; // Reference to the Hand Crank item
    public GameObject doorObject; // Reference to the door object
    public bool isDoorOpen = false;
    public Transform moveStartPoint;
    public Transform moveEndPoint;
    public float doorMoveDuration = 1f;
    public float doorFallDuration = 0.5f;
    public int doorOpenSortingOrder = 2;
    public int doorClosedSortingOrder = 1;

    private Coroutine doorCoroutine;
    private Collider2D doorCollider;
    private SpriteRenderer doorSpriteRenderer;
    private ShoppingCart blockedCart;

    [Header("UI Settings")]
    public GameObject menu;
    public Button installHandCrankButton;
    public Button removeHandCrankButton;
    public Button operateDoorButton;
    public Button exitButton;

    public override void Interact(PlayerManager player)
    {
        base.Interact(player);

        if (menu != null)
        {
            menu.SetActive(true);

            UpdateMenu();
        }
    }

    public void UpdateMenu()
    {
        if (!handCrank && PlayerSelector.Instance.currentInventory.currentSelectedItem is HandCrank)
        {
            installHandCrankButton.interactable = true;
        }
        else
        {
            installHandCrankButton.interactable = false;
        }

        if (handCrank != null && isControlledByHandCrank)
        {
            removeHandCrankButton.interactable = true;
        }
        else
        {
            removeHandCrankButton.interactable = false;
        }

        if (isControlledByHandCrank && handCrank != null)
        {
            operateDoorButton.interactable = true;
        }
        else
        {
            operateDoorButton.interactable = false;
        }
    }

    public void Exit()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }

    public void InstallHandCrank()
    {
        if (handCrank == null && !isControlledByHandCrank && PlayerSelector.Instance.currentInventory.currentSelectedItem is HandCrank)
        {
            isControlledByHandCrank = true;
            handCrank = PlayerSelector.Instance.currentInventory.currentSelectedItem as HandCrank;
            PlayerSelector.Instance.currentInventory.RemoveItem(PlayerSelector.Instance.currentInventory.currentSelectedItem);
            UpdateMenu();
        }
    }

    public void RemoveHandCrank()
    {
        if (handCrank != null && isControlledByHandCrank)
        {
            isControlledByHandCrank = false;
            PlayerSelector.Instance.currentInventory.TryAdd(handCrank);
            handCrank = null;
            UpdateMenu();

            if (doorCoroutine != null)
            {
                StopCoroutine(doorCoroutine);
            }
            doorCoroutine = StartCoroutine(DropDoor());
        }
    }

    public void OperateDoor()
    {
        if (isControlledByHandCrank && handCrank != null)
        {
            if (doorCoroutine != null)
            {
                StopCoroutine(doorCoroutine);
            }
            doorCoroutine = StartCoroutine(MoveDoor());
        }
    }

    private IEnumerator MoveDoor()
    {
        isBlockedByShoppingCart = false;
        isDoorOpen = !isDoorOpen;
        bool opening = isDoorOpen;

        if (!opening)
        {
            yield return CloseDoorRoutine(doorMoveDuration);
            yield break;
        }

        // 开门（上升）
        SetDoorSortingOrder(doorOpenSortingOrder);
        Vector3 startPosition = doorObject.transform.position;
        Vector3 targetPosition = moveEndPoint.position;

        float elapsed = 0f;
        while (elapsed < doorMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorMoveDuration);
            doorObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        doorObject.transform.position = targetPosition;
        SetDoorColliderEnabled(false);

        // 门重新打开后恢复购物车的移动
        if (blockedCart != null)
        {
            blockedCart.canMove = true;
            blockedCart = null;
        }
    }

    private IEnumerator DropDoor()
    {
        isBlockedByShoppingCart = false;
        yield return CloseDoorRoutine(doorFallDuration);
    }

    // 门向下关闭/下落：下降过程中检测购物车，挡住就停在购物车上
    private IEnumerator CloseDoorRoutine(float duration)
    {
        SetDoorColliderEnabled(true);

        Vector3 startPosition = doorObject.transform.position;
        Vector3 targetPosition = moveStartPoint.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            doorObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            if (TryBlockDoor())
                yield break;

            yield return null;
        }

        doorObject.transform.position = targetPosition;
        isDoorOpen = false;
        SetDoorSortingOrder(doorClosedSortingOrder);
    }

    private bool TryBlockDoor()
    {
        ShoppingCart cart = GetBlockingShoppingCart();
        if (cart == null)
            return false;

        isBlockedByShoppingCart = true;
        isDoorOpen = false;
        blockedCart = cart;
        blockedCart.canMove = false;

        SetDoorSortingOrder(doorOpenSortingOrder);
        SetDoorColliderEnabled(false);
        return true;
    }

    private void SetDoorColliderEnabled(bool enabled)
    {
        if (doorObject == null)
            return;

        if (doorCollider == null)
            doorCollider = doorObject.GetComponent<Collider2D>();
        if (doorCollider != null)
            doorCollider.enabled = enabled;
    }

    private void SetDoorSortingOrder(int order)
    {
        if (doorObject == null)
            return;

        if (doorSpriteRenderer == null)
            doorSpriteRenderer = doorObject.GetComponentInChildren<SpriteRenderer>();
        if (doorSpriteRenderer != null)
            doorSpriteRenderer.sortingOrder = order;
    }

    private ShoppingCart GetBlockingShoppingCart()
    {
        if (doorObject == null)
            return null;

        if (doorCollider == null)
            doorCollider = doorObject.GetComponent<Collider2D>();
        if (doorCollider == null)
            return null;

        // 门是通过 transform 直接移动的，先同步物理变换，保证精确重叠检测用的是当前位置
        Physics2D.SyncTransforms();

        ContactFilter2D filter = new() { useTriggers = true };
        Collider2D[] results = new Collider2D[8];

        int count = doorCollider.Overlap(filter, results);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = results[i];
            if (hit == null)
                continue;

            ShoppingCart cart = hit.GetComponentInParent<ShoppingCart>();
            if (cart != null)
                return cart;
        }

        return null;
    }
}
