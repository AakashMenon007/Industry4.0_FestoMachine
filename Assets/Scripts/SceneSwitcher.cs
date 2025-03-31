using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    // Public method to load a scene by name
    public void SwitchScene(string sceneName)
    {
        // Check if the scene exists before trying to load it
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene " + sceneName + " cannot be loaded. Make sure it is added to the Build Settings.");
        }
    }

    // Public method to load a scene by index
    public void SwitchSceneByIndex(int sceneIndex)
    {
        // Check if the scene index is valid
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError("Scene index " + sceneIndex + " is out of range. Make sure it is valid in the Build Settings.");
        }
    }
}