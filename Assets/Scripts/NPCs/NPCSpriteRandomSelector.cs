using UnityEngine;
using UnityEngine.Rendering;

public class NPCSpriteRandomSelector : MonoBehaviour
{
    public bool EnableThisSelector = true;

    public Sprite[] possibleSprites;

    public SpriteRenderer _sprite;


    protected virtual void Start()
    {
        if (!EnableThisSelector)
            return;

        if (_sprite == null)
            _sprite = GetComponentInChildren<SpriteRenderer>();

        if (possibleSprites == null || possibleSprites.Length == 0)
        {
            if (PlayerSelector.Instance != null && PlayerSelector.Instance.npcSprites != null)
                possibleSprites = PlayerSelector.Instance.npcSprites.possibleSprites;
        }

        if (_sprite != null && possibleSprites != null && possibleSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, possibleSprites.Length);
            _sprite.sprite = possibleSprites[randomIndex];
        }
    }
}

[CreateAssetMenu(fileName = "NewNPCSprites", menuName = "Scriptable Objects/NPC Sprites")]
public class NPCSprites : ScriptableObject
{
    public Sprite[] possibleSprites;
}
