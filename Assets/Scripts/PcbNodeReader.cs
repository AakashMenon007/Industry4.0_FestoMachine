using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PCBBoxManager : MonoBehaviour
{
    [Header("Project Scripts")]
    public NodeReader[] pcbNodeReaders;

    [Header("Project UI Elements")]
    public TMP_Text[] pcbStatusTMP;
    public Image[] pcbStatusImages;

    private void Update()
    {
        for (int i = 0; i < pcbNodeReaders.Length; i++)
        {
            bool status = bool.Parse(pcbNodeReaders[i].dataFromOPCUANode);
            pcbStatusTMP[i].text = status ? "PCB Present" : "No PCB";
            pcbStatusImages[i].color = status ? Color.green : Color.red;
        }
    }
}