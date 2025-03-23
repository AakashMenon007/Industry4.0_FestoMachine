using UnityEngine;

public class ToggleObjectSwitcher : MonoBehaviour
{
    private GameObject currentActiveObject;

    // This method turns off the last object and turns on the new one
    public void ActivateNewObject(GameObject newObject)
    {
        // Turn off the currently active object
        if (currentActiveObject != null)
        {
            currentActiveObject.SetActive(false);
        }

        // Turn on the new object
        newObject.SetActive(true);

        // Update the current active object
        currentActiveObject = newObject;
    }
}
