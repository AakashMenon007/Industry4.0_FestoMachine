using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways] // Runs in edit mode too!
public class CanvasBackfaceCuller : MonoBehaviour
{
    public Material frontOnlyMaterial;

    void Reset()
    {
        ApplyFrontOnlyMaterial();
    }

    void OnValidate()
    {
        ApplyFrontOnlyMaterial();
    }

    void ApplyFrontOnlyMaterial()
    {
        if (frontOnlyMaterial == null)
        {
            Debug.LogWarning("FrontOnly Material is not assigned!", this);
            return;
        }

        // Apply to all Unity UI components (Images, RawImages, Text, etc.)
        Graphic[] uiGraphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in uiGraphics)
        {
            graphic.material = frontOnlyMaterial;
        }

        // Optional: Apply to TextMeshProUGUI components if you're using TMP
        TextMeshProUGUI[] tmpTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmpTexts)
        {
            tmp.fontMaterial = frontOnlyMaterial;
        }

        Debug.Log("Applied FrontOnly Material to all UI elements!", this);
    }
}
