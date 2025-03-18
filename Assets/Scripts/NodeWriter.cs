using UnityEngine;
using realvirtual;

public class NodeWriter : MonoBehaviour
{
    [Header("Factory Machine")]
    public int factoryMachineID;
    public OPCUA_Interface oPCUAinterface;

    //[Header("OPCUA Reader")]
    //public string nodeBeingMonitored;
    //public string nodeID;
    //public string dataFromOPCUANode;

    // Start is called before the first frame update
    void Start()
    {
        oPCUAinterface.EventOnConnected.AddListener(OnInterfaceConnected);
        oPCUAinterface.EventOnDisconnected.AddListener(OnInterfaceDisconnected);
    }

    private void OnInterfaceConnected()
    {
        Debug.Log("Node Writer connected to Factory Machine " + factoryMachineID);
    }

    private void OnInterfaceDisconnected()
    {
        Debug.LogWarning("Node Writer for Factory Machine " + factoryMachineID + " has disconnected");
    }
    
    public void WriteNodeToFalse(string nodeID)
    {
        oPCUAinterface.WriteNodeValue(nodeID, false);
    }

    public void WriteNodeToTrue(string nodeID)
    {
        oPCUAinterface.WriteNodeValue(nodeID, true);
    }

}
