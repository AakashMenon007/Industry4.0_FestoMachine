using System.Reflection;
using realvirtual;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SignalHierarchyContextMenu
{
  

 
    private  static void ChangeDirection(GameObject gameobjec)
    {
        var signal = gameobjec.GetComponent<Signal>();
        if (signal == null)
            return;
        Signal newsignal = signal;

        var type = signal.GetType();
        if (signal.IsInput())
        {
            if (type == typeof(realvirtual.PLCInputBool))
            {
                 newsignal = gameobjec.AddComponent<PLCOutputBool>();
            }
            if (type == typeof(realvirtual.PLCInputInt))
            {
                newsignal = gameobjec.AddComponent<PLCOutputInt>();
            }
            if (type == typeof(realvirtual.PLCInputFloat))
            {
                newsignal = gameobjec.AddComponent<PLCOutputFloat>();
            }
        }
        else
        {
            if (type == typeof(realvirtual.PLCOutputBool))
            {
                newsignal = gameobjec.AddComponent<PLCInputBool>();
            }
            if (type == typeof(realvirtual.PLCOutputInt))
            {
                newsignal = gameobjec.AddComponent<PLCInputInt>();
            }
            if (type == typeof(realvirtual.PLCOutputFloat))
            {
                newsignal = gameobjec.AddComponent<PLCInputFloat>();
            }

        }
        
        newsignal.Name = signal.Name;
        newsignal.Comment = signal.Comment;
        newsignal.OriginDataType = signal.OriginDataType;
        Object.DestroyImmediate(signal);
    }
    
  
    
    [MenuItem("GameObject/realvirtual/Change Signal Direction",false,0)]
    public static void HierarchyChangeSignalDirection()
    {
        foreach (var obj in Selection.objects)
        {
            var gameobject = (GameObject) obj;
            ChangeDirection(gameobject);
        }
     

    }
   
    [MenuItem("CONTEXT/Component/realvirtual/Change Signal Direction")]
    public static void ComtextChangeSignalDirection(MenuCommand command)
    {
        var gameobject =  command.context;
        var obj = (Component)gameobject;
        ChangeDirection(obj.gameObject);

    }
 
}