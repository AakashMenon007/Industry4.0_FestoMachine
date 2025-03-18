// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/signal-manager")]
public class SignalManager : realvirtualBehavior
{
    [InfoBox("Perform Check Signals for Updating the information")]public int SignalsTotal;
    public int SignalsConnected;
    public int SignalsUnconnected;
    public int SignalsDeactivated;
    
    [ReorderableList]public List<GameObject> AutoConnectLevels;
    public List<Signal> UnconnectedSignals;
    
    [Button("Check Signals")]
    void CheckUnconnected()
    {
        SignalsTotal = 0;
        SignalsConnected = 0;
        SignalsDeactivated = 0;
        SignalsUnconnected = 0;
        
        realvirtualController = UnityEngine.Object.FindObjectOfType<realvirtualController>();
        if (realvirtualController!=null)
            realvirtualController.UpdateSignals();
        UnconnectedSignals = new List<Signal>();
        var signals = Global.GetComponentsAlsoInactive<Signal>(this.gameObject);
        foreach (var signal in signals)
        {
            SignalsTotal++;
            if (signal.gameObject.activeSelf)
            {
                if (signal.IsConnectedToBehavior() == false)
                {
                    UnconnectedSignals.Add(signal);
                    SignalsUnconnected++;
                }
                else
                {
                    SignalsConnected++;
                }
                    
            }
            else
            {
                SignalsDeactivated++;
            }
           
        }
        
    }
    
    [Button("Deactivate unconnected Signals")]
    void DeactivatedUnconnected()
    {
#if UNITY_EDITOR
        CheckUnconnected();
        foreach (var signal in UnconnectedSignals)
        {
            signal.gameObject.SetActive(false);
            SignalsDeactivated++;
            SignalsUnconnected--;
        }
#endif
        UnconnectedSignals.Clear();
    }
    
      
    [Button("Activate unconnected Signals")]
    void ActivatedUnconnected()
    {
#if UNITY_EDITOR
        CheckUnconnected();
        var signals = Global.GetComponentsAlsoInactive<Signal>(this.gameObject);
        foreach (var signal in signals)
        {
            if (!signal.gameObject.activeSelf)
            {
                signal.gameObject.SetActive(true);
                SignalsDeactivated--;
                SignalsUnconnected++;
            }
        }
#endif
        UnconnectedSignals.Clear();
    }
    
    [Button("Delete unconnected Signals")]
    void DeleteUnconnected()
    {
        #if UNITY_EDITOR
        CheckUnconnected();
       foreach (var signal in UnconnectedSignals)
       {
           Undo.DestroyObjectImmediate(signal.gameObject);
       }
       #endif
        UnconnectedSignals.Clear();
    }

    [Button("Delete automatically created signals")]
    void DeleteAutoSignals()
    {
#if UNITY_EDITOR
        var signals = GetComponentsInChildren<Signal>();
        foreach (var signal in signals)
        {
            if (signal.Autoconnected== true)
                Undo.DestroyObjectImmediate(signal.gameObject);
        }
        #endif
    }
    
    [Button("Start Signal creation & connection")]
    void AutoConnect()
    {
        
        foreach (var go in AutoConnectLevels)
        {
            var behaviors = go.GetComponentsInChildren<BehaviorInterface>();
            foreach (var behavior in behaviors)
            {
                var connectlogics = GetComponents<AutoConnectBase>();
                foreach (var logic in connectlogics)
                {
                    logic.AutoConnect(behavior);
                }
            }
        }   
        CheckUnconnected();
    }
    
}
}
