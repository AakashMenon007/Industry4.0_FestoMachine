// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;

namespace realvirtual
{
    public class DemoMoveCubeWithNodeScript : MonoBehaviour
    {
        private OPCUA_Node node;

        public float PositionX;

        // Start is called before the first frame update
        void Start()
        {
            node = GetComponent<OPCUA_Node>(); // Values are transfered from connected OPCUA_Node scritp
        }

        // Update is called once per frame
        void Update()
        {
            node.ReadNode();
            if (node.SignalValue != null) // on Startup the Value can be 0 before first update from OPC Server
                PositionX = (float) node.SignalValue;
            transform.rotation = Quaternion.Euler(new Vector3(PositionX, 0, 0));

        }
    }
}
