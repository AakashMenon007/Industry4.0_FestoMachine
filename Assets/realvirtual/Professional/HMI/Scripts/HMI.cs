// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace realvirtual
{
    [System.Serializable]
    public class RealVirtualHMIEvent: UnityEvent<HMI>
    {}
    //! Base class for all HMI elements
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/hmi-components")]
    public class HMI : BehaviorInterface
    {
        [OnValueChanged("Init")]public Color Color;//!< Color of the HMI element
        public Color ColorMouseOver;//!< Color of the HMI element when the mouse is over it
        [Tooltip("0:transparent; 255: opaque")]public int AlphaVisibility = 255;//!< Alpha value of the HMI element
        public RealVirtualHMIEvent EventOnValueChanged;//!< Event that is triggered when the value of the HMI element changes
        [HideInInspector]public Image bgImg;
        
        private Canvas canvasInParent;
        private Canvas canvaslocal = null;

        public Text GetText(string objName)
        {
            GameObject obj = GetChildByName(objName);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<Text>();
        }
        
        public Image GetImage(string objName)
        {
            GameObject obj = GetChildByName(objName);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<Image>();
        }

        public virtual void CloseExtendedArea(Vector3 pos)
        {
           
        }

        public virtual void Init()
        {
            
        }
#if UNITY_EDITOR
        public void Reset()
        {
            Init();
        }
#endif

        public void OnValueChanged()
        {
            EventOnValueChanged.Invoke(this);
        }

        
        public Canvas GetCanvas(GameObject HMIelement, ref RectTransform rectTransform)
        {
            canvasInParent = HMIelement.transform.parent.gameObject.GetComponent<Canvas>();
            if (canvasInParent == null)
            {
                if (gameObject.GetComponent<Canvas>() == null)
                    canvaslocal = HMIelement.AddComponent<Canvas>();
                else
                    canvaslocal = HMIelement.GetComponent<Canvas>();
            }
            else
            {
                rectTransform = canvasInParent.GetComponent<RectTransform>();
                if (HMIelement.GetComponent<Canvas>() != null)
                {
                    DestroyImmediate(HMIelement.GetComponent<Canvas>() );
                }
            }
            
            if (canvaslocal != null)
            {

                canvaslocal.renderMode = RenderMode.WorldSpace;
                canvaslocal.worldCamera = Camera.main;
                canvaslocal.sortingLayerName = "Default";
                canvaslocal.sortingOrder = 0;
                rectTransform = canvaslocal.GetComponent<RectTransform>();
                return canvaslocal;
            }

            return canvasInParent;
        }

        public void OnTransformParentChanged()
        {
            if(canvaslocal != null && gameObject.GetComponentInParent<Canvas>()!=null)
                DestroyImmediate(canvaslocal);
        }
    }
}
