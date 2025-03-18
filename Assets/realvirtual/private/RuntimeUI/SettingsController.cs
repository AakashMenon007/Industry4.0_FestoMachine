using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public class SettingsController : MonoBehaviour
    {

        public GameObject window;
        // Start is called before the first frame update

        public void Start()
        {
            int quality = PlayerPrefs.GetInt("Quality", -1); 
            if (quality!=-1)
                QualitySettings.SetQualityLevel(quality, true);
            quality = QualitySettings.GetQualityLevel();
            var tog = GetComponentsInChildren<QualityToggleChange>();
            foreach (var to in tog)
            {
                to.SetQualityStatus(quality);
            }
        }

        public void OnQualityToggleChanged(int qualitylevel)
        {
            QualitySettings.SetQualityLevel(qualitylevel, true);
            PlayerPrefs.SetInt("Quality", qualitylevel);
            PlayerPrefs.Save();
        }

        public void CloseSettingsWindow()
        {
            window.SetActive(false);
        }
        
#if REALVIRTUAL_PLANNER
        public void OpenSettingsWindow(UI_ToolbarItem item)
        {
            if(item.IsOn)
                window.SetActive(true);
            else
            {
                window.SetActive(false);
            }
        }

        public void OpenSettingsWindowFromMenu()
        {
            if (window.activeSelf)
                window.SetActive(false);
            else
            {
                window.SetActive(true);
            }
        }
#endif

    }
}
    
