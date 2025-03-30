using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class AutoReferenceLinker : MonoBehaviour
{
    public GameObject[] RootObjects => _rootObjects;
    [SerializeField] private GameObject[] _rootObjects; // Drag your root objects here

    // Stores all GameObjects and their UniqueIDs
    private Dictionary<string, GameObject> _idToObject = new Dictionary<string, GameObject>();

    void Awake()
    {
        BuildIDDictionary();
        LinkAllReferences();
    }

    // Build a dictionary of IDs to GameObjects
    private void BuildIDDictionary()
    {
        foreach (var root in _rootObjects)
        {
            UniqueID[] uniqueIDs = root.GetComponentsInChildren<UniqueID>(true);
            foreach (UniqueID uid in uniqueIDs)
            {
                _idToObject[uid.ID] = uid.gameObject;
            }
        }
    }

    // Automatically link all references
    public void LinkAllReferences()
    {
        foreach (var root in _rootObjects)
        {
            MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour script in scripts)
            {
                if (script == null) continue; // Skip missing scripts
                LinkScriptReferences(script);
            }
        }
    }

    // Link references for a single script
    private void LinkScriptReferences(MonoBehaviour script)
    {
        FieldInfo[] fields = script.GetType().GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        foreach (FieldInfo field in fields)
        {
            // Handle GameObject references
            if (field.FieldType == typeof(GameObject))
            {
                GameObject referencedGO = (GameObject)field.GetValue(script);
                if (referencedGO != null && referencedGO.TryGetComponent<UniqueID>(out var uid))
                {
                    if (_idToObject.TryGetValue(uid.ID, out GameObject linkedGO))
                    {
                        field.SetValue(script, linkedGO);
                    }
                }
            }

            // Handle Component references (e.g., Transform, Rigidbody)
            else if (typeof(Component).IsAssignableFrom(field.FieldType))
            {
                Component referencedComp = (Component)field.GetValue(script);
                if (referencedComp != null && referencedComp.TryGetComponent<UniqueID>(out var uid))
                {
                    if (_idToObject.TryGetValue(uid.ID, out GameObject linkedGO))
                    {
                        Component linkedComp = linkedGO.GetComponent(field.FieldType);
                        field.SetValue(script, linkedComp);
                    }
                }
            }
        }
    }
}