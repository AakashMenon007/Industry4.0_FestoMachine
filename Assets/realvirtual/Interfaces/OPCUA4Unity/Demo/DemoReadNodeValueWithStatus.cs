
using realvirtual;
using UnityEngine;

public class DemoReadNodeValueWithStatus : MonoBehaviour
{
    public OPCUA_Node node;
    public string Status;
   

    public string Value;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var val = node.ReadNodeValue();
        if (val != null)
          Value = val.ToString();
        Status = node.Status;

    }
}
