// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace realvirtual
{

    public class DemoTextWithDelegate : MonoBehaviour
    {
        public OPCUA_Interface Interface;
        public string NodeId;
        public string TextFromOPCUANode;

        private OPCUANodeSubscription subscription;
        private Text text;


        // Start is called before the first frame update
        void Start()
        {
            text = GetComponent<Text>();
            if (Interface != null)
            {
                Interface.EventOnConnected.AddListener(OnConnected);
            }
              
        }

        public void OnConnected()
        {
            subscription = Interface.Subscribe(NodeId, NodeChanged);
        }

        public void NodeChanged(OPCUANodeSubscription sub, object obj)
        {
         
            TextFromOPCUANode = obj.ToString();
        }

        // Update is called once per frame
        void Update()
        {
            text.text = TextFromOPCUANode;
        }
    }
}
