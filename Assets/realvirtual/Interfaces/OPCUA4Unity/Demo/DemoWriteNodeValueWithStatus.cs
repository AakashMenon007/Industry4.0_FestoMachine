
using System;
using realvirtual;
using UnityEditor;
using UnityEngine;

public class DemoWriteNodeValueWithStatus : MonoBehaviour
{
    public int IntValue;
    public OPCUA_Interface Interface;
    public string Status;
    public string Servertimestamp;
    public bool IsGood;
    public bool IsBad;
    public string Value;
    
    
    private OPCUA_Node node;

    void Awake()
    {
        // Node with Interface is created upon startup because nodeid is not included in imported nodes
        node = GetComponent<OPCUA_Node>();
        if (node==null)
           node = gameObject.AddComponent<OPCUA_Node>();
        node.NodeId = "ns=2;s=Demo.Static.Scalar.Int16";
        node.Interface = Interface;

    }

    // Update is called once per frame
    void Update()
    {
        if (IntValue > 1000)
            IntValue = 0;
        IntValue = IntValue + 1;
        
        node.WriteNodeValue((Int16)IntValue);
       
    }
}
