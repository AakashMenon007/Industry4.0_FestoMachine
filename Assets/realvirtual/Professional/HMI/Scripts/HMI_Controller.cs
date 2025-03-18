// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
#if CINEMACHINE
using Cinemachine;
#endif

namespace realvirtual
{
    //! CameraController is a component to control the cinemachine cameras used by HMI-elements.
    //! This component is necessary as a central control unit at the main parent object of all HMI-elements
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/hmi-components")]
    public class HMI_Controller : MonoBehaviour
    {
         public bool BlockUserMouseNavigation = true; //!< If true, the user can not navigate the scene with the mouse
         public HMI_TabButton MouseCtrlButton;
#if CINEMACHINE
        public CinemachineVirtualCamera MainCinemachineCamera;//!< The main camera used by the HMI

        private CinemachineBrain CinemachineBrain;
       
        private CinemachineVirtualCamera[] currentCameras;
        [HideInInspector] public CinemachineVirtualCamera currCam;
        private CameraPos lastCameraPos;
#else
        [InfoBox("Please install Cinemachine via the package manager for full HMI functionality")]
#endif
        [HideInInspector] public SceneMouseNavigation SceneMouseNavigation;

        private bool MouseCtrlButtonAvailable = false;
        public void OnClickMouseStatusButton()
        {
            if (BlockUserMouseNavigation)
            {
                SetMouseNavigsationStatus(false);
                SceneMouseNavigation.ActivateCinemachine(false);
            }
        }

        public void SetMouseNavigsationStatus(bool blocked)
        {
            if (blocked)
            {
                BlockUserMouseNavigation = true;
                if(MouseCtrlButtonAvailable) 
                    MouseCtrlButton.SetImage(false);
            }
            else
            {
                BlockUserMouseNavigation = false;
                if(MouseCtrlButtonAvailable)
                    MouseCtrlButton.SetImage(true);
            }
        }
        public void Awake()
        {
            if(MouseCtrlButton != null)
                MouseCtrlButtonAvailable = true;
            SceneMouseNavigation=FindObjectOfType<SceneMouseNavigation>();
#if CINEMACHINE
            CinemachineBrain = FindObjectOfType<CinemachineBrain>();
            if (CinemachineBrain != null)
            {
                if (MainCinemachineCamera == null)
                {
                    Debug.LogError("No MainCinemachineCamera defined in HMI_Controller");
                    currCam= CinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
                }
                else
                {
                    currCam = MainCinemachineCamera;
                }
                currentCameras = FindObjectsOfType<CinemachineVirtualCamera>();
               
                if (BlockUserMouseNavigation)
                {
                    SceneMouseNavigation.ActivateCinemachine(true);
                    CinemachineBrain.enabled = true;
                    if(MouseCtrlButtonAvailable)
                    MouseCtrlButton.SetImage(false);

                }
                else
                {
                    if(MouseCtrlButtonAvailable)
                        MouseCtrlButton.SetImage(true);
                }
                setactiveCamera();
            }
#endif
            GameObject Toolbar = GameObject.Find("game4automation/UI/MainView/Toolbar");
            if (Toolbar != null)
            {
                Toolbar.SetActive(false);
                Time.timeScale = 1;
            }
        }

#if CINEMACHINE
        public void SetCamera(CinemachineVirtualCamera camera)
        {
            if (!BlockUserMouseNavigation)
            {
                SceneMouseNavigation.LastCameraPosition.SaveCameraPosition(SceneMouseNavigation);
                SceneMouseNavigation.ActivateCinemachine(true);
                CinemachineBrain.enabled = true;
                currCam = MainCinemachineCamera;
                setactiveCamera();
                currCam = camera;
                setactiveCamera();
            }
            else
            {
                if (!SceneMouseNavigation.CinemachineIsActive)
                {
                    SceneMouseNavigation.ActivateCinemachine(true);
                }
                currCam = camera;
                setactiveCamera();
            }

        }

        public void ResetCamera()
        {
            if (!BlockUserMouseNavigation)
            {
                if (SceneMouseNavigation.CinemachineIsActive)
                {
                    SceneMouseNavigation.ActivateCinemachine(false);
                    if (SceneMouseNavigation.LastCameraPosition != null)
                        SceneMouseNavigation.LastCameraPosition.SetCameraPositionPlaymode(SceneMouseNavigation);
                }
            }
            else
            {
                currCam = MainCinemachineCamera;
                setactiveCamera();
            }
        }
#endif
        public void SetPosinCanvas(Canvas canvas,GameObject hmiElement,Vector3 TargetPos,Vector3 Offset )
        {
            if(canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                hmiElement.transform.position = Camera.main.WorldToScreenPoint(TargetPos + Offset);
            }
            else
            {
                Vector3 targetPosition = TargetPos+Offset;
                canvas.transform.position = targetPosition;
            }
        }
        
        public void SetRotationinCanvas(Canvas canvas,GameObject hmiElement,Vector3 TargetPos,bool followCamera)
        {
            if (SceneMouseNavigation == null)
            {
                SceneMouseNavigation=FindObjectOfType<SceneMouseNavigation>();
#if CINEMACHINE
                CinemachineBrain = FindObjectOfType<CinemachineBrain>();
#endif
            }
            if (followCamera)
            {
                if (SceneMouseNavigation.CinemachineIsActive)
                {
#if CINEMACHINE
                    canvas.transform.rotation = Quaternion.LookRotation(currCam.transform.forward);
#endif
                }
                else
                {
                    canvas.transform.rotation = Quaternion.LookRotation(hmiElement.transform.position - Camera.main.transform.position);
                }
            }
            else
            {
                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    canvas.transform.rotation = Quaternion.LookRotation(hmiElement.transform.position - TargetPos);
                }
                
            }
        }
#if CINEMACHINE
        private void setactiveCamera()
        {
            if (currCam == null)
                return;

            foreach (var camera in currentCameras)
            {
                if (camera != null)
                    camera.Priority = 10;
            }

            currCam.Priority = 100;
            var dolly = currCam.gameObject.GetComponentInParent<CinemachineDollyCart>();
            if (dolly != null)
                dolly.m_Position = 0;
        }
#endif
        
    }
}
