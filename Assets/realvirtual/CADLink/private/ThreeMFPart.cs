// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using ThreeMf;
using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{
    public class ThreeMFPart : MonoBehaviour
    {
        [ReadOnly] public string File;
        [ReadOnly] public string Id;
        [ReadOnly] public string Name;
        public string Color = "";
        public string Material = "";
        [ReadOnly] public string LastUpdate;
        [ReadOnly] public bool IsClone = false;
        [ReadOnly] public ThreeMFPart CloneFromOrigin;
        [ReadOnly] public ThreeMFPart AssembledInto;
        [ReadOnly] public List<ThreeMFPart> Clones;
        [ReadOnly] public Vector3 ImportPos;
        [ReadOnly] public Vector3 ImportRot;
        [ReadOnly] public Vector3 ImportRotOriginal;
        [ReadOnly] public Vector3 ImportScale;
        [ReadOnly] private ThreeMfObject threemfobject;


        public void Set3MFfObject(ThreeMfObject obj)
        {
            threemfobject = obj;
        }

        public ThreeMfObject Get3MFObject(ThreeMfObject obj)
        {
            return threemfobject;
        }

        public void MakeAsSubCompponent(ThreeMFPart parent)
        {

            this.transform.parent = parent.transform;
            this.AssembledInto = parent;
        }

        public ThreeMFPart CreateClone(bool nameclonesidentical)
        {
            var newobj = Instantiate(gameObject);
            ThreeMFPart part = newobj.GetComponent<ThreeMFPart>();
            part.CloneFromOrigin = this;
            part.IsClone = true;
            if (Clones == null)
                Clones = new List<ThreeMFPart>();
            Clones.Add(part);
            if (nameclonesidentical)
                part.name = this.gameObject.name;
            else
                part.name = this.gameObject.name + " (" + Clones.Count + ")";

            return part;
        }
    }
}
