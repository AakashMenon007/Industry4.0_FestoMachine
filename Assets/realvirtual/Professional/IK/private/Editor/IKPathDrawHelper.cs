// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEditor;
using UnityEngine;

namespace realvirtual
{
    public static class IKPathDrawerHelper
    {
        public static void DrawPath(IKPath path)
        {
            if (path.DrawPath)
                for (int i = 0; i < path.Path.Count; i++)
                {
                    var target = path.Path[i];

                    if (target.InterpolationToTarget == IKTarget.Interploation.Linear)
                        Handles.color = Color.green;
                    else
                        Handles.color = Color.yellow;
                    if (i > 0)
                    {
                        var from = path.Path[i - 1];
                        Handles.DrawLine(from.transform.position, target.transform.position, 3);
                    }
                }
        }
    }

}
