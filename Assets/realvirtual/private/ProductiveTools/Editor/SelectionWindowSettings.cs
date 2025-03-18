using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{

    public class SelectionWindowSettings : ScriptableObject
    {
        public MaterialUpdateSettings materialupdatesettings;
        public Material selectedassingmaterial;
        public Material selectedmaterialupdate;
        public GameObject selectedtonewparent;
        public string togroup;
        public GameObject selectedpivottoorigin;

    }

}