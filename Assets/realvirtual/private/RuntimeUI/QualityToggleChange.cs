using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace realvirtual
{
    public class QualityToggleChange : MonoBehaviour
    {

        public SettingsController settingscontroller,IUISkinEdit;
        private Toggle toggle;
        private realvirtualController _controller;
        public int qualitylevel;
        // Start is called before the first frame update

        void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnQualityToggleChanged);
            _controller = FindObjectOfType<realvirtualController>();
        }
        private void Start()
        {
#if REALVIRTUAL_PLANNER
            UpdateUISkinParameter();
#endif
        }
        public void SetQualityStatus(int quality)
        {
            if (quality == qualitylevel)
                toggle.isOn = true;
        }

        public void OnQualityToggleChanged(bool ison)
        {
            if (ison)
                settingscontroller.OnQualityToggleChanged(qualitylevel);
        }
#if REALVIRTUAL_PLANNER
        public void UpdateUISkinParameter()
        {
           
            toggle.graphic.color = _controller.EditorSkin.WindowHoverColor;
           
        }
#endif

    }
}
    
