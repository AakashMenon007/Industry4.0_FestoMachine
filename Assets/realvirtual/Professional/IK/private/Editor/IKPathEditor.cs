// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using UnityEngine;
using UnityEditor;
using NaughtyAttributes.Editor;

namespace realvirtual
{

    [CustomEditor(typeof(IKPath))]
    public class IKPathEditor : NaughtyInspector

    {
        
    public void OnSceneGUI()
    {
        IKPath path = (IKPath) target;
        var targets = path.GetComponentsInChildren<IKTarget>();
        Vector3[] positions = new Vector3[targets.Length];
        Quaternion[] rotations = new Quaternion[targets.Length];

        var e = Event.current;
        bool rotation = false;
        if (e.control)
            rotation = true;

        var startMatrix = Handles.matrix;

        IKPathDrawerHelper.DrawPath(path);
        if (path.DrawTargets)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (!rotation)
                    positions[i] = Handles.PositionHandle(targets[i].transform.position,
                        targets[i].transform.rotation);
                else
                    rotations[i] = Handles.RotationHandle(targets[i].transform.rotation,
                        targets[i].transform.position);
            }


            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (!rotation)
                        if (targets[i].transform.position != positions[i])
                        {
                            targets[i].transform.position = positions[i];
                            if (!Application.isPlaying)
                                targets[i].SetAsTarget();
                        }

                    if (rotation)
                        if (targets[i].transform.rotation != rotations[i])
                        {
                            targets[i].transform.rotation = rotations[i];
                            if (!Application.isPlaying)
                                targets[i].SetAsTarget();
                        }
                }
            }
        }
    }
    }


}

   