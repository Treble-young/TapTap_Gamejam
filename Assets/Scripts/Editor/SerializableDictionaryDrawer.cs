using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generic drawer body. Unity can't attach [CustomPropertyDrawer] to a generic
/// type, so for every concrete SerializableDictionary subclass you add one
/// one-line drawer class at the bottom of this file.
/// </summary>
public abstract class SerializableDictionaryDrawer<TKey, TValue> : PropertyDrawer
{
    private const float Padding = 2f;
    private const float RemoveButtonWidth = 18f;
    private const float Gap = 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty keysProp = property.FindPropertyRelative("keys");
        SerializedProperty valuesProp = property.FindPropertyRelative("values");

        EditorGUI.BeginProperty(position, label, property);

        Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            float y = headerRect.yMax + Padding;
            int count = Mathf.Min(keysProp.arraySize, valuesProp.arraySize);
            float fieldWidth = (position.width - RemoveButtonWidth - Gap) / 2f;

            int removeIndex = -1;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty keyProp = keysProp.GetArrayElementAtIndex(i);
                SerializedProperty valueProp = valuesProp.GetArrayElementAtIndex(i);

                Rect keyRect = new Rect(position.x, y, fieldWidth, EditorGUIUtility.singleLineHeight);
                Rect valueRect = new Rect(keyRect.xMax + Gap, y, fieldWidth, EditorGUIUtility.singleLineHeight);
                Rect removeRect = new Rect(valueRect.xMax + Gap, y, RemoveButtonWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none, true);
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);

                if (GUI.Button(removeRect, "-"))
                    removeIndex = i;

                y += EditorGUIUtility.singleLineHeight + Padding;
            }

            if (removeIndex >= 0)
            {
                keysProp.DeleteArrayElementAtIndex(removeIndex);
                valuesProp.DeleteArrayElementAtIndex(removeIndex);
            }

            Rect addRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(addRect, "+ Add Entry"))
            {
                keysProp.arraySize++;
                valuesProp.arraySize++;

                // A new enum key defaults to its first value, which usually already
                // exists. Assign the first unused value instead to avoid duplicates.
                if (typeof(TKey).IsEnum)
                {
                    SerializedProperty newKey = keysProp.GetArrayElementAtIndex(keysProp.arraySize - 1);
                    int valueCount = System.Enum.GetValues(typeof(TKey)).Length;
                    for (int candidate = 0; candidate < valueCount; candidate++)
                    {
                        bool used = false;
                        for (int j = 0; j < keysProp.arraySize - 1; j++)
                        {
                            if (keysProp.GetArrayElementAtIndex(j).enumValueIndex == candidate)
                            {
                                used = true;
                                break;
                            }
                        }

                        if (!used)
                        {
                            newKey.enumValueIndex = candidate;
                            break;
                        }
                    }
                }
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        SerializedProperty keysProp = property.FindPropertyRelative("keys");
        int rows = Mathf.Max(keysProp.arraySize, 0);
        // header + (rows of key/value pairs) + add button + trailing padding
        return EditorGUIUtility.singleLineHeight
             + Padding
             + rows * (EditorGUIUtility.singleLineHeight + Padding)
             + EditorGUIUtility.singleLineHeight
             + Padding;
    }
}

[CustomPropertyDrawer(typeof(PlayerTypeStringDictionary))]
public class PlayerTypeStringDictionaryDrawer : SerializableDictionaryDrawer<PlayerType, string> { }
