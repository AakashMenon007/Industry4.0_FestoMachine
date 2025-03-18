using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public class DemoImportRuntime : MonoBehaviour
    {

        void Import()
        {
            GetComponent<CADLink>().ImportCad();
        }
        // Start is called before the first frame update
        void Start()
        {
           // Start a little bit later - only necessary for testrunner
           Invoke("Import",1);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

