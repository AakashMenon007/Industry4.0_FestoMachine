using UnityEngine;
using realvirtual;



public class MRNodeReader : MonoBehaviour
{

    [Header("Factory Machine")]
    public int factoryMachineID;
    public OPCUA_Interface oPCUAinterface;

    [Header("OPCUA Reader")]
    public string nodeBeingMonitored;
    public string nodeID;
    public string dataFromOPCUANode;



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

        Debug.Log("Connected to Factory Machine " + factoryMachineID);
        Debug.Log(dataFromOPCUANode);
    }

    // Method called when the OPC UA interface is disconnected
    private void OnInterfaceDisconnected()
    {

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
    }
}
