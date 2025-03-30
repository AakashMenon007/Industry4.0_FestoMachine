using UnityEngine;

public class ToggleGameObject2 : MonoBehaviour
{
    // Drag your GameObject here in the Inspector
    public GameObject targetObject;

    /// <summary>
    /// Toggles the active state of the target GameObject.
    /// </summary>
    public void ToggleActiveState()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
        else
        {
            Debug.LogWarning("No target GameObject assigned!");
        }
    }
}
