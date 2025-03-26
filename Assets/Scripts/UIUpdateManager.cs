using realvirtual;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIUpdateManager : MonoBehaviour
{
    [Header("Project Scripts")]
    public OPCUA_Interface[] interfaces;  // Renamed from interfacess for clarity
    public NodeReader[] RFIDInNodeReaders;
    public NodeReader[] EmStopNodeReaders;
    public NodeReader[] ChangeReqNodeReaders;
    public NodeReader[] RobotArmHomeNodeReaders;
    public NodeReader[] SafetyDoorClosedNodeReaders;
    public SendOrder sendOrder;

    [Header("Project UI Elements")]
    public Image[] connectionImages;
    public TMP_Text[] RFIDInTMP;
    public TMP_Text[] EmStopTMP;
    public TMP_Text[] ChangeReqTMP;
    public TMP_Text[] RobotArmHomeTMP;
    public TMP_Text[] SafetyDoorClosedTMP; // Fixed reference name
    public TMP_Dropdown orderQuantityDropdown;
    public TMP_Dropdown partNumberDropdown;

    public Image iconImage;

    // Method to update connection status images based on OPC UA interface connection
    public void UpdateConnectionImages(int interfaceToRead)
    {
        if (interfaces[interfaceToRead].IsConnected)
        {
            connectionImages[interfaceToRead].color = Color.green;
        }
        else
        {
            connectionImages[interfaceToRead].color = Color.red;
        }
    }

    // Method to update text information from OPC UA node to TMP_Text component
    public void UpdateDataFromNodeTMP(int interfaceToRead, string node)
    {
        if (node == "RFIDIn")
        {
            Debug.LogWarning((interfaceToRead + 1) + " is reading: " + RFIDInNodeReaders[interfaceToRead].dataFromOPCUANode);
        }
        else if (node == "EmStop")
        {
            Debug.LogWarning((interfaceToRead + 1) + " is reading: " + EmStopNodeReaders[interfaceToRead].dataFromOPCUANode);
        }
        else if (node == "ChangeReq")
        {
            Debug.LogWarning((interfaceToRead + 1) + " is reading: " + ChangeReqNodeReaders[interfaceToRead].dataFromOPCUANode);
        }
        else if (node == "SafetyDoorClosed")
        {
            Debug.LogWarning((interfaceToRead + 1) + " is reading: " + SafetyDoorClosedNodeReaders[interfaceToRead].dataFromOPCUANode);
        }
        else if (node == "RobotArmHome")
        {
            Debug.LogWarning((interfaceToRead + 1) + " is reading: " + RobotArmHomeNodeReaders[interfaceToRead].dataFromOPCUANode);
        }
        else if (node == "Icon")
        {
            // Assuming the node data contains the path to an image resource.
            // You may need to adjust this logic based on how the data is structured.
            Sprite newIcon = Resources.Load<Sprite>(RFIDInNodeReaders[interfaceToRead].dataFromOPCUANode);
            if (newIcon != null)
            {
                iconImage.sprite = newIcon;
            }
            else
            {
                Debug.LogWarning("Icon not found: " + RFIDInNodeReaders[interfaceToRead].dataFromOPCUANode);
            }
        }
    }

    // Method to send an order to the machine using values from UI dropdowns
    public void SendOrderToMachine()
    {
        sendOrder.partNumber = partNumberDropdown.options[partNumberDropdown.value].text;
        sendOrder.qty = orderQuantityDropdown.options[orderQuantityDropdown.value].text;
        sendOrder.SendOrderToFactory();
    }

    private void Update()
    {
        for (int i = 0; i < RFIDInNodeReaders.Length; i++)
        {
            RFIDInTMP[i].text = RFIDInNodeReaders[i].dataFromOPCUANode;
        }

        for (int i = 0; i < EmStopNodeReaders.Length; i++)
        {
            EmStopTMP[i].text = EmStopNodeReaders[i].dataFromOPCUANode;
        }

        for (int i = 0; i < ChangeReqNodeReaders.Length; i++)
        {
            ChangeReqTMP[i].text = ChangeReqNodeReaders[i].dataFromOPCUANode;
        }

        for (int i = 0; i < SafetyDoorClosedNodeReaders.Length; i++)
        {
            SafetyDoorClosedTMP[i].text = SafetyDoorClosedNodeReaders[i].dataFromOPCUANode;
        }

        for (int i = 0; i < RobotArmHomeNodeReaders.Length; i++)
        {
            RobotArmHomeTMP[i].text = SafetyDoorClosedNodeReaders[i].dataFromOPCUANode;
        }
    }
}
