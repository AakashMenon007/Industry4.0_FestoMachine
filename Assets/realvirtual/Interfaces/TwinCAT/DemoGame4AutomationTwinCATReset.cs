using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    
    public class DemoGame4AutomationTwinCATReset : MonoBehaviour
    {
        void UnReset()
        {
            var res = GetComponent<Signal>();
            res.SetValue(false);
        }
        // Start is called before the first frame update
        void Start()
        {
            var res = GetComponent<Signal>();
            res.SetValue(true);
            Invoke("UnReset",1);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
 
}
