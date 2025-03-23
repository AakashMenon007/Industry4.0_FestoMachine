using UnityEngine;

public class PrefabToggle : MonoBehaviour
{
    // Drag your prefab (or any GameObject you want to toggle) here in the Inspector
    public GameObject prefabToToggle;

    // This method turns it on/off
    public void TogglePrefab()
    {
        if (prefabToToggle != null)
        {
            // If it's active, deactivate it; if not, activate it
            prefabToToggle.SetActive(!prefabToToggle.activeSelf);
        }
    }
}
