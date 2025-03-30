using UnityEngine;
using System.Collections;

public class LightStrobe : MonoBehaviour
{
    [Header("Light Settings")]
    public Light strobeLight; // The light to strobe
    public float strobeDelay = 0.5f; // Delay between strobes in seconds
    public float strobeIntensity = 5f; // Intensity of the light during the strobe

    private float originalIntensity; // To store the original intensity of the light
    private bool isStrobing = false;

    void Start()
    {
        if (strobeLight == null)
        {
            Debug.LogError("No light assigned to the strobe script.");
            return;
        }

        // Store the original intensity of the light
        originalIntensity = strobeLight.intensity;
    }

    public void StartStrobe()
    {
        if (!isStrobing)
        {
            isStrobing = true;
            StartCoroutine(StrobeCoroutine());
        }
    }

    public void StopStrobe()
    {
        if (isStrobing)
        {
            isStrobing = false;
            StopCoroutine(StrobeCoroutine());
            strobeLight.intensity = originalIntensity; // Reset to original intensity
        }
    }

    private IEnumerator StrobeCoroutine()
    {
        while (isStrobing)
        {
            strobeLight.intensity = strobeIntensity;
            yield return new WaitForSeconds(strobeDelay);
            strobeLight.intensity = originalIntensity;
            yield return new WaitForSeconds(strobeDelay);
        }
    }

    void OnValidate()
    {
        if (strobeDelay < 0)
        {
            strobeDelay = 0.1f; // Ensure a minimum delay to prevent issues
        }
    }
}
