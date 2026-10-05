using System.Collections;
using UnityEngine;

public class Cabinet : InteractableManager
{
    public GameObject[] worldPickupItemSpawnedPrefab;
    public Transform spawnPoint;

    [Header("Item Spawn Settings")]
    [Tooltip("多个物品生成时的间距偏移量")]
    public float itemSpawnOffset = 0.5f;

    [Header("Shake Settings")]
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.05f;
    private bool isShaking = false;

    public override void Interact(PlayerManager player)
    {
        
        base.Interact(player);

        // 1. 如果柜子里面有东西
        if (worldPickupItemSpawnedPrefab != null && worldPickupItemSpawnedPrefab.Length > 0)
        {
            for (int i = 0; i < worldPickupItemSpawnedPrefab.Length; i++)
            {
                Vector3 offset = new Vector3(i * itemSpawnOffset, 0, 0);

                GameObject item = Instantiate(worldPickupItemSpawnedPrefab[i], spawnPoint.position + offset, Quaternion.identity);
            }

            worldPickupItemSpawnedPrefab = new GameObject[0];
        }
        // 2. 如果柜子是空的
        else if (!isShaking)
        {
            
            StartCoroutine(ShakeCoroutine());
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;

        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.localPosition = originalPos + new Vector3(xOffset, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
}