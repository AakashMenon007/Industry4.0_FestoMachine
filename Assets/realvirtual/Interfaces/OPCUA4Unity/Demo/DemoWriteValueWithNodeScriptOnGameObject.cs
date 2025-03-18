// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;

namespace realvirtual
{
    public class DemoWriteValueWithNodeScriptOnGameObject : MonoBehaviour
    {
        private OPCUA_Node node;

        // Start is called before the first frame update
        void Start()
        {
            node = GetComponent<OPCUA_Node>();
        }

        // Update is called once per frame
        void Update()
        {
#if REALVIRTUAL
        var signal = GetComponent<Signal>();
        if (signal != null)
            DestroyImmediate(signal);
#endif
            node.WriteNodeValue(Random.Range(0, 1000));
        }
    }
}