using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MRMachineAssign : MonoBehaviour
{
    [Header("Machine Identifier")]
    public int machineNumber;

    [Header("Nodes Being Read & Holders")]
    public GameObject rfidReadersHolder;
    public GameObject emgStopReadersHolder;
    public NodeReader[] rfidReaders;
    public NodeReader[] emgStopReaders;



    [Header ("UI & Output Display")]
    public TMP_Text machineNumberDisplay;
    public TMP_Text rFIDDisplay;
    public TMP_Text emgStopDisplay;

    private bool machineAssigned = false;

    private void Start()
    {
        rfidReadersHolder = GameObject.Find("RFIDIn");
        emgStopReadersHolder = GameObject.Find("EMGStop");

        rfidReaders = rfidReadersHolder.GetComponentsInChildren<NodeReader>();
        emgStopReaders = emgStopReadersHolder.GetComponentsInChildren<NodeReader>();

        AssignMachineNumber();
    }

    private void Update()
    {
        if(machineAssigned)
        {
            machineNumberDisplay.text = "Machine " + machineNumber.ToString();
            rFIDDisplay.text = "Cart " + rfidReaders[machineNumber - 1].dataFromOPCUANode + " has entered the machine.";
            emgStopDisplay.text = emgStopReaders[machineNumber - 1].dataFromOPCUANode;
        }
    }

    public void AssignMachineNumber()
    {
        machineAssigned = true;
    }
}
