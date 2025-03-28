using realvirtual;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIUpdateManager : MonoBehaviour
{
    [System.Serializable]
    public class MachineUI
    {
        [Header("Node Readers")]
        public NodeReader RFIDInNodeReader;
        public NodeReader EmStopNodeReader;
        public NodeReader ResetNodeReader;
        public NodeReader SafetyDoorNodeReader;
        public NodeReader PCBoxNodeReader;


        [Header("TMP Text")]
        public TMP_Text RFIDInTMP;
        public TMP_Text EmStopTMP;
        public TMP_Text ResetTMP;
        public TMP_Text SafetyDoorText;
        public TMP_Text PCBoxText;
    }

    [Header("Project Scripts")]
    public OPCUA_Interface[] interfaces;

    [Header("Machine UI")]
    public MachineUI[] machines = new MachineUI[9];

    [Header("Connection Status")]
    public Image[] connectionImages;

    // Method to update connection status images based on OPC UA interface connection
    public void UpdateConnectionImages(int interfaceIndex)
    {
        connectionImages[interfaceIndex].color = interfaces[interfaceIndex].IsConnected ? Color.green : Color.red;
    }

    // Method to update text information from OPC UA node to TMP_Text component
    public void UpdateDataFromNodeTMP(int machineIndex, string nodeType)
    {
        MachineUI machine = machines[machineIndex];
        string data = "";

        switch (nodeType)
        {
            case "RFIDIn":
                data = machine.RFIDInNodeReader.dataFromOPCUANode;
                Debug.LogWarning($"Machine {machineIndex + 1} RFID is reading: {data}");
                break;
            case "EmStop":
                data = machine.EmStopNodeReader.dataFromOPCUANode;
                Debug.LogWarning($"Machine {machineIndex + 1} EmStop is reading: {data}");
                break;
            case "Reset":
                data = machine.ResetNodeReader.dataFromOPCUANode;
                Debug.LogWarning($"Machine {machineIndex + 1} Reset is reading: {data}");
                break;
            case "SafetyDoor":
                data = machine.SafetyDoorNodeReader.dataFromOPCUANode;
                Debug.LogWarning($"Machine {machineIndex + 1}  is reading: {data}");
                break;
            case "PCBox":
                data = machine.PCBoxNodeReader.dataFromOPCUANode;
                Debug.LogWarning($"Machine {machineIndex + 1}  is reading: {data}");
                break;
        }
    }

    private void Update()
    {
        for (int i = 0; i < machines.Length; i++)
        {
            MachineUI machine = machines[i];

            // RFID
            machine.RFIDInTMP.text = machine.RFIDInNodeReader.dataFromOPCUANode;

            // Emergency Stop
            machine.EmStopTMP.text = GetCustomMessage(machine.EmStopNodeReader.dataFromOPCUANode, "Disengaged", "Engaged");

            // Reset
            machine.ResetTMP.text = GetCustomMessage(machine.ResetNodeReader.dataFromOPCUANode, "Engaged", "Disengaged");

            // Safety Door
            machine.SafetyDoorText.text = GetCustomMessage(machine.SafetyDoorNodeReader.dataFromOPCUANode, "Open", "Closed");

            // Safety Door
            machine.PCBoxText.text = GetCustomMessage(machine.PCBoxNodeReader.dataFromOPCUANode, "No Change", "Change Req ");
        }
    }

    private string GetCustomMessage(string opcUAValue, string trueMessage, string falseMessage)
    {
        if (bool.TryParse(opcUAValue, out bool boolValue))
        {
            return boolValue ? trueMessage : falseMessage;
        }
        return "Unknown State";
    }
}