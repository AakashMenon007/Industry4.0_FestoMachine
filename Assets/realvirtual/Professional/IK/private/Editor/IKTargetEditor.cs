// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEditor;
using NaughtyAttributes.Editor;

namespace realvirtual
{
    [CustomEditor(typeof(IKTarget))]
    public class IKTargetEditor :  NaughtyInspector
    {

        public void OnSceneGUI()
        {
          
            IKTarget iktarget = (IKTarget) target;
            var pathes = iktarget.GetComponentsInParent<IKPath>();
            foreach (var path in pathes)
            {
                IKPathDrawerHelper.DrawPath(path);
            }

        }
    }
    
}


   