using UnityEngine;
using realvirtual;


public class NodeReader : MonoBehaviour
{

    [Header("Factory Machine")]
    public int factoryMachineID;
    public OPCUA_Interface oPCUAinterface;

    [Header("OPCUA Reader")]
    public string nodeBeingMonitored;
    public string nodeID;
    public string dataFromOPCUANode;

    [Header("UI")]
    public UIUpdateManager uiUpdateManager;

    // Find the UIUpdateManager component on scene start
    private void Awake()
    {
        uiUpdateManager = GameObject.FindGameObjectWithTag("UIUpdateManager").GetComponent<UIUpdateManager>();
    }

    // Subscribe to OPC UA events on start
    void Start()
    {
        oPCUAinterface.EventOnConnected.AddListener(OnInterfaceConnected);
        oPCUAinterface.EventOnDisconnected.AddListener(OnInterfaceDisconnected);
    }

    // Method called when the OPC UA interface is connected
    private void OnInterfaceConnected()
    {
        // Subscribe to the specified node and provide the method to call on node change
        var subscription = oPCUAinterface.Subscribe(nodeID, NodeChanged);
        dataFromOPCUANode = subscription.ToString();
        uiUpdateManager.UpdateConnectionImages(factoryMachineID - 1);

        Debug.Log("Connected to Factory Machine " + factoryMachineID);
        Debug.Log(dataFromOPCUANode);
    }

    // Method called when the OPC UA interface is disconnected
    private void OnInterfaceDisconnected()
    {
        // Update UI elements based on the disconnection
        uiUpdateManager.UpdateConnectionImages(factoryMachineID - 1);

        Debug.LogWarning("Factory Machine " + factoryMachineID + " has disconnected");
    }

    // Method called when the monitored node changes its value
    public void NodeChanged(OPCUANodeSubscription sub, object value)
    {
        //if(nodeBeingMonitored == "Icon")
        //{
        //    dataFromOPCUANode
        //}
        dataFromOPCUANode = value.ToString();
        uiUpdateManager.UpdateDataFromNodeTMP(factoryMachineID - 1, nodeBeingMonitored);
    }
}
