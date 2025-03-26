using realvirtual;
using UnityEngine;
using TMPro;

public class UIUpdateManager2 : MonoBehaviour
{
    [Header("Project Scripts")]
    public NodeReader[] EmStopNodeReaders;

    [Header("Project UI Elements")]
    public TMP_Text[] EmStopTMP;

    // Cache for previous data to minimize redundant updates
    private string[] previousNodeData;

    private void Awake()
    {
        // Validate that the lengths of the arrays match
        if (EmStopNodeReaders.Length != EmStopTMP.Length)
        {
            Debug.LogError("Mismatch in array lengths: EmStopNodeReaders and EmStopTMP must have the same length.");
        }

        // Initialize previousNodeData to avoid null references
        previousNodeData = new string[EmStopNodeReaders.Length];
    }

    private void Update()
    {
        // Ensure NodeReaders and TMP_Text elements are assigned
        if (EmStopNodeReaders == null || EmStopTMP == null)
        {
            Debug.LogError("NodeReaders or TMP_Text elements are not assigned!");
            return;
        }

        for (int i = 0; i < EmStopNodeReaders.Length; i++)
        {
            // Check for missing references
            if (EmStopNodeReaders[i] == null || EmStopTMP[i] == null)
            {
                Debug.LogError($"Missing reference at index {i} for NodeReader or TMP_Text.");
                continue;
            }

            // Fetch current data from the NodeReader
            string currentData = EmStopNodeReaders[i].dataFromOPCUANode;

            // Update the UI element only if the data has changed
            if (previousNodeData[i] != currentData)
            {
                previousNodeData[i] = currentData;
                EmStopTMP[i].text = currentData;
            }
        }
    }
}
