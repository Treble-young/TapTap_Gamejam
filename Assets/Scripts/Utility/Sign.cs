using UnityEngine;

public class Sign : MonoBehaviour
{
    public Collider2D col;

    public string message = "这是一个提示牌。";
    public bool showOnce = true;
    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerUIManager.Instance.popUpManager.ShowPopUpWindow(message, 999f);
            if (showOnce)
                col.enabled = false;
        }
    }
}
