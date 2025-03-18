using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public class AutoConnectBase : realvirtualBehavior
    {
        
        public virtual bool AutoConnect(BehaviorInterface behavior)
        {
            Debug.LogError("Not impelented");
            return true;
        }
        
        public virtual GameObject GetSignal(string name)
        {
            Transform[] children = transform.GetComponentsInChildren<Transform>();
            // First check names of signals
            foreach (var child in children)
            {
                var signal = child.GetComponent<Signal>();
                if (signal != null && child != gameObject.transform)
                {
                    if (signal.Name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            // Second check names of components
            foreach (var child in children)
            {
                if (child != gameObject.transform)
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        public T GetOrCreateSignal<T> (string signalname) where T : Signal
        {
            var obj = GetSignal(signalname);
            if (obj == null)
                obj = Global.AddGameObjectIfNotExisting(signalname, this.gameObject);
            var signal = obj.GetComponent<T>();
            if (signal == null)
            {
                signal = obj.AddComponent<T>();
                foreach (var sig in obj.GetComponents<Signal>())
                {
                    if (sig != signal)
                        DestroyImmediate(sig);
                }
            }

            signal.Autoconnected = true;
            return signal;
        }
    }
}

