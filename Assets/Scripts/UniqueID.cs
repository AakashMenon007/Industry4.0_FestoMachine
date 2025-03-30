using UnityEngine;

public class UniqueID : MonoBehaviour
{
    [SerializeField] private string _id; // Unique identifier
    public string ID => _id;

    // Generate a unique ID if empty
    void Reset() => _id = System.Guid.NewGuid().ToString();
}