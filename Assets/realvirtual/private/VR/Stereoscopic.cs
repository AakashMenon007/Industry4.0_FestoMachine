using UnityEngine;
using NaughtyAttributes;

namespace realvirtual
{



    public class Stereoscopic : MonoBehaviour
    {
        private Transform cameraHeadTransform;
        public Camera monoCamera;
        public GameObject leftEyeCamera;
        public GameObject rightEyeCamera;

        [OnValueChanged("InitView")] [Range(0.00f, 0.10f)]
        public float EyeSeparation = 0.0346f;

        public float StepEyeSeparation = 0.005f;
        [OnValueChanged("InitView")] public float NearClipPlane = 0.01f;
        [OnValueChanged("InitView")] public float FarClipPlane = 100.0f;

        [OnValueChanged("InitView")] [Range(0.0f, 120f)]
        public float FieldOfView = 60.0f;

        [OnValueChanged("InitView")] public bool isStereo = true;



        void InitView()
        {

            // Get the transform of the CameraHead gameobject
            cameraHeadTransform = gameObject.transform;

            leftEyeCamera.transform.localPosition = new Vector3(-EyeSeparation / 2, 0, 0);
            var cameraLE = leftEyeCamera.GetComponent<Camera>();
            cameraLE.rect = new Rect(0, 0, 0.5f, 1);
            cameraLE.fieldOfView = FieldOfView;
            cameraLE.ResetAspect();
            cameraLE.aspect *= 2;
            cameraLE.nearClipPlane = NearClipPlane;
            cameraLE.farClipPlane = FarClipPlane;

            rightEyeCamera.transform.localPosition = new Vector3(+EyeSeparation / 2, 0, 0);
            var cameraRE = rightEyeCamera.GetComponent<Camera>();
            cameraRE.rect = new Rect(0.5f, 0, 0.5f, 1);
            cameraRE.fieldOfView = FieldOfView;
            cameraRE.ResetAspect();
            cameraRE.aspect *= 2;
            cameraRE.nearClipPlane = NearClipPlane;
            cameraRE.farClipPlane = FarClipPlane;
            
            monoCamera.enabled = !isStereo;
            leftEyeCamera.SetActive(isStereo);
            rightEyeCamera.SetActive(isStereo);
        }

        void Start()
        {
            InitView();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                isStereo = false;
                InitView();
            }
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                isStereo = true;
                InitView();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                EyeSeparation = EyeSeparation - StepEyeSeparation;
                if (EyeSeparation <= 0)
                {
                    EyeSeparation = 0.0346f;
                }
            }

            if (Input.GetKeyDown(KeyCode.F12))
            {
                EyeSeparation = EyeSeparation + StepEyeSeparation;
            }

        }
    }
}