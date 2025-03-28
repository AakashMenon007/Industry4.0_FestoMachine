using UnityEngine;
using TMPro;

public class MRMachineAssign : MonoBehaviour
{
    [Header("Machine Identifier")]
    [SerializeField] private int machineNumber = 1; // Default to 1

    [Header("Node Holders")]
    [SerializeField] private GameObject rfidReadersHolder;
    [SerializeField] private GameObject emgStopReadersHolder;

    [Header("UI Display")]
    [SerializeField] private TMP_Text machineNumberDisplay;
    [SerializeField] private TMP_Text rFIDDisplay;
    [SerializeField] private TMP_Text emgStopDisplay;

    private MRNodeReader[] rfidReaders;
    private MRNodeReader[] emgStopReaders;
    private bool initialized;

    private void Start()
    {
        InitializeReferences();
        ValidateMachineNumber();
        initialized = true;
    }

    private void InitializeReferences()
    {
        if (rfidReadersHolder == null)
            rfidReadersHolder = GameObject.Find("RFIDIn");
        if (emgStopReadersHolder == null)
            emgStopReadersHolder = GameObject.Find("EMGStop");

        if (rfidReadersHolder)
            rfidReaders = rfidReadersHolder.GetComponentsInChildren<MRNodeReader>(true);
        if (emgStopReadersHolder)
            emgStopReaders = emgStopReadersHolder.GetComponentsInChildren<MRNodeReader>(true);
    }

    private void ValidateMachineNumber()
    {
        machineNumber = Mathf.Clamp(machineNumber, 1, int.MaxValue);
        UpdateMachineNumberDisplay();
    }

    private void Update()
    {
        if (!initialized) return;

        UpdateMachineNumberDisplay();
        UpdateRFIDDisplay();
        UpdateEmergencyStopDisplay();
    }

    private void UpdateMachineNumberDisplay()
    {
        if (machineNumberDisplay)
            machineNumberDisplay.text = $"Machine {machineNumber}";
    }

    private void UpdateRFIDDisplay()
    {
        if (IsValidIndex(rfidReaders, machineNumber - 1) && rFIDDisplay != null)
        {
            var rfidData = rfidReaders[machineNumber - 1].dataFromOPCUANode;
            rFIDDisplay.text = $"Cart {rfidData} has entered the machine.";
        }
    }

    private void UpdateEmergencyStopDisplay()
    {
        if (IsValidIndex(emgStopReaders, machineNumber - 1) && emgStopDisplay != null)
        {
            var emgData = emgStopReaders[machineNumber - 1].dataFromOPCUANode;
            emgStopDisplay.text = emgData;
        }
    }

    private bool IsValidIndex(MRNodeReader[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length;
    }

    public void AssignMachineNumber(int newNumber)
    {
        machineNumber = newNumber;
        ValidateMachineNumber();
    }
}
