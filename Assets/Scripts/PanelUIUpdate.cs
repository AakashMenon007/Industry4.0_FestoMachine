using realvirtual;
using UnityEngine;

public class PanelDisplayManager : MonoBehaviour
{
    [System.Serializable]
    public class PanelState
    {
        public NodeReader nodeReader;
        public GameObject engagedPrefab;
        public GameObject disengagedPrefab;
    }

    [System.Serializable]
    public class MachinePanel
    {
        [Header("Emergency Stop")]
        public PanelState emStopState;

        [Header("Reset")]
        public PanelState resetState;

        [Header("Safety Door")]
        public PanelState safetyDoorState;
    }

    [Header("Machine Panels")]
    public MachinePanel[] machines = new MachinePanel[9];

    private void Update()
    {
        for (int i = 0; i < machines.Length; i++)
        {
            UpdatePanelState(machines[i].emStopState);
            UpdatePanelState(machines[i].resetState);
            UpdatePanelState(machines[i].safetyDoorState);
        }
    }

    private void UpdatePanelState(PanelState state)
    {
        if (bool.TryParse(state.nodeReader.dataFromOPCUANode, out bool isEngaged))
        {
            state.engagedPrefab.SetActive(isEngaged);
            state.disengagedPrefab.SetActive(!isEngaged);
        }
        else
        {
            Debug.LogWarning("Invalid boolean value received from OPC UA node");
            state.engagedPrefab.SetActive(false);
            state.disengagedPrefab.SetActive(false);
        }
    }

    // Optional: Method to force update specific panel state
    public void UpdateSpecificPanel(int machineIndex, string panelType)
    {
        MachinePanel machine = machines[machineIndex];

        switch (panelType)
        {
            case "EmStop":
                UpdatePanelState(machine.emStopState);
                break;
            case "Reset":
                UpdatePanelState(machine.resetState);
                break;
            case "SafetyDoor":
                UpdatePanelState(machine.safetyDoorState);
                break;
        }
    }
}
