// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;


namespace realvirtual
{

    public class DemoReadNodeNotRecommended : MonoBehaviour
    {

        public OPCUA_Interface Interface;
        public string NodeId;

        // Update is called once per frame
        void Update()
        {

            float myvar = (float) Interface.ReadNodeValue(NodeId);
        }
    }
}