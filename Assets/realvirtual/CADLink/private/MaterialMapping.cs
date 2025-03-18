
using UnityEngine;
using System.Collections.Generic;
using System;


[Serializable]
public class MatMapping
{
    public string ThreeMfMaterialname;
    public string ThreeMfColorname;
    public Color StepColor;
    public Material Material;
}

[CreateAssetMenu(fileName = "MaterialMapping", menuName = "realvirtual/Add material mapping", order = 1)]
public class MaterialMapping : ScriptableObject
{
    
    [SerializeField]
    public List<MatMapping> Mappings;
}