using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    public List<TKey> keys = new List<TKey>();
    public List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();

        int count = Mathf.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            // Use the indexer instead of Add: duplicate keys can occur transiently
            // while editing in the Inspector (e.g. a freshly added enum key defaults
            // to its first value). Overwriting keeps the last entry instead of throwing.
            this[keys[i]] = values[i];
        }
    }
}

// ------------------------------------------------------------------
// Concrete, non-generic subclasses.
// Unity cannot serialize the generic type above directly, so for each
// <TKey, TValue> combination you use in a MonoBehaviour you must add a
// concrete subclass here. Each one also gets a one-line PropertyDrawer
// in Assets/Scripts/Editor/SerializableDictionaryDrawer.cs.
// ------------------------------------------------------------------

[System.Serializable]
public class PlayerTypeStringDictionary : SerializableDictionary<PlayerType, string> { }
