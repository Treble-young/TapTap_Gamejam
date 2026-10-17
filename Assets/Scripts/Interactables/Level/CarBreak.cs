using System.Security.Cryptography;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CarBreak : MonoBehaviour
{
    public Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Car>() != null)
        {
            col.enabled = false;
            collision.GetComponent<Car>().BreakDown();
        }
    }
}
