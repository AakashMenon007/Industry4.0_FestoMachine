using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    [CreateAssetMenu(fileName = "Materialpalet", menuName = "realvirtual/Add material collection",
        order = 1)]
    public class MaterialPalet : ScriptableObject
    {
        public List<Material> materiallist;
    }
}
