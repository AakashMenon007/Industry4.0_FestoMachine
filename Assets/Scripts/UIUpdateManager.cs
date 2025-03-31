using realvirtual;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIUpdateManager : MonoBehaviour
{
    [Header("Project Scripts")]
    public OPCUA_Interface[] interfacess;
    public NodeReader[] RFIDInNodeReaders;
    public NodeReader[] RobotArmNodeReaders;
    public NodeReader[] SafetyDoorNodeReaders;
    public NodeReader[] PCBoxNodeReaders;
    public NodeReader[] EmStopNodeReaders;
    public NodeReader[] ResetNodeReaders;
    public NodeReader[] RobotinoNodeReaders;
    public NodeReader[] CameraNodeReaders;
    public SendOrder sendOrder;

    [Header("Project UI Elements")]
    public Image[] connectionImages;
    public TMP_Text[] RFIDInTMP;
    public TMP_Text[] RobotArmTMP;
    public TMP_Text[] PCBoxTMP;
    public TMP_Text[] EmStopTMP;
    public TMP_Text[] EmResetTMP;
    public TMP_Text[] SafetyDoorTMP;
    public TMP_Text[] RobotinoTMP;
    public TMP_Text[] CameraTMP;

    public TMP_Dropdown orderQuantityDropdown;
    public TMP_Dropdown partNumberDropdown;

    private string ConvertToCustomMessage(string value, string nodeType)
    {
        switch (nodeType)
        {
            case "EmStop":
            case "SafetyDoor":
                return value == "True" ? "Disengaged" : "Engaged";
            case "Reset":
                return value == "True" ? "Engaged" : "Disengaged";
            case "RobotArm":
            case "Robotino":
            case "Camera":
                return value == "True" ? "Busy" : "Not Busy";
            case "PCBox":
                return value == "True" ? "Change Req" : "Box Full";
            default:
                return value;
        }
    }

    // Method to update text information from OPC UA node to TMP_Text component
    public void UpdateDataFromNodeTMP(int interfaceToRead, string node)
    {
        switch (node)
        {
            case "RFIDIn":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + RFIDInNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "RobotArm":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + RobotArmNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "SafetyDoor":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + SafetyDoorNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "PCBox":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + PCBoxNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "EmStop":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + EmStopNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "Reset":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + ResetNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "Robotino":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " + RobotinoNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "Camera":
                Debug.LogWarning((interfaceToRead + 1) + " is reading: " +CameraNodeReaders[interfaceToRead].dataFromOPCUANode);
                break;
            case "Icon":
                // Handle icon image update here if needed
                break;
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
        for (int i = 0; i < RobotArmNodeReaders.Length; i++)
        {
            RobotArmTMP[i].text = ConvertToCustomMessage(RobotArmNodeReaders[i].dataFromOPCUANode, "RobotArm");
        }
        for (int i = 0; i < SafetyDoorNodeReaders.Length; i++)
        {
            SafetyDoorTMP[i].text = ConvertToCustomMessage(SafetyDoorNodeReaders[i].dataFromOPCUANode, "SafetyDoor");
        }
        for (int i = 0; i < PCBoxNodeReaders.Length; i++)
        {
            PCBoxTMP[i].text = ConvertToCustomMessage(PCBoxNodeReaders[i].dataFromOPCUANode, "PCBox");
        }
        for (int i = 0; i < EmStopNodeReaders.Length; i++)
        {
            EmStopTMP[i].text = ConvertToCustomMessage(EmStopNodeReaders[i].dataFromOPCUANode, "EmStop");
        }
        for (int i = 0; i < ResetNodeReaders.Length; i++)
        {
            EmResetTMP[i].text = ConvertToCustomMessage(ResetNodeReaders[i].dataFromOPCUANode, "Reset");
        }
        for (int i = 0; i < RobotinoNodeReaders.Length; i++)
        {
            RobotinoTMP[i].text = ConvertToCustomMessage(RobotinoNodeReaders[i].dataFromOPCUANode, "Robotino");
        }
        for (int i = 0; i < CameraNodeReaders.Length; i++)
        {
            CameraTMP[i].text = ConvertToCustomMessage(CameraNodeReaders[i].dataFromOPCUANode, "Robotino");
        }
    }
}
