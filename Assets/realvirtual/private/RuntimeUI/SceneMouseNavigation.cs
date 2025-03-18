// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 in2Sight GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz    

using System;
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN ) && !UNITY_WEBGL
using SpaceNavigatorDriver;
#endif
#if CINEMACHINE
   using Cinemachine;
#endif
using NaughtyAttributes;
using UnityEditor;
using UnityEngine.EventSystems;


namespace realvirtual
{
    //! Controls the Mouse and Touch navigation in Game mode
    public class SceneMouseNavigation : realvirtualBehavior
    {
        [Tooltip("Toggle the orbit camera mode")]
        public bool UseOrbitCameraMode = false; //!< Toggle the orbit camera mode 


        [Tooltip("Block rotation on selected objects")]
        public bool BlockRotationOnSelected = false;

        [Tooltip("Toggle the first person controller")]
        public bool FirstPersonControllerActive = true; //!< Toggle the first person controller 

        [Tooltip("Rotate the camera with the left mouse button")]
        public bool RotateWithLeftMouseButton = false; //!< Rotate the camera with the left mouse button 

        [Tooltip("Rotates the camera to focused objects instead of panning the camera ")]
        public bool RotateToFocusObject = false; //!< Rotates the camera to focused objects instead of panning the camera 

        [Tooltip("Reference to the first person controller script")]
        public FirstPersonController FirstPersonController; //!< Reference to the first person controller script 

        [Tooltip("The last camera position before switching modes")]
        public CameraPos LastCameraPosition; //!< The last camera position before switching modes

        [Tooltip("Set the camera position on start play")]
        public bool SetCameraPosOnStartPlay = true; //!< Set the camera position on start play

        [Tooltip("Save the camera position on quitting the application")]
        public bool SaveCameraPosOnQuit = true; //!< Save the camera position on quitting the application 

        [Tooltip("Set the editor camera position")]
        public bool SetEditorCameraPos = true; //!< Set the editor camera position

        [Tooltip("The target of the camera")] public Transform target; //!< The target of the camera

        [Tooltip("Offset of the camera's target")]
        public Vector3 targetOffset; //!< Offset of the camera's target 

        [Tooltip("The distance of the camera from its target")]
        public float distance = 5.0f; //!< The distance of the camera from its target

        [Tooltip("Calculate the maximum distance using the bounding box of the scene")]
        public bool
            CalclulateMaxDistanceWithBoundingBox =
                true; //!< Calculate the maximum distance using the bounding box of the scene 

        [Tooltip("The DPI scale of the screen, is automatically calculated and is used to scale all screen pixel related distances measurements")]
        [ReadOnly] public float DPIScale = 1; //!< The DPI scale of the screen, is automatically calculated and is used to scale all screen pixel related distances measurements
        
        // Zoomtomousepos not working yet - needs to be finished
        [HideInInspector][Tooltip("Zooms always at position of pointer pos and not to at screen center if true")] public bool ZoomToMousePos = false; //!Zooms always at position of pointer pos and not to at screen center if true

        [HideIf("CalclulateMaxDistanceWithBoundingBox")] [Tooltip("The maximum distance of the camera from its target")]
        public float maxDistance = 20; //!< The maximum distance of the camera from its target

        [Tooltip("The minimum distance of the camera from its target")]
        public float minDistance = .6f; //!< The minimum distance of the camera from its target

        [Tooltip("The speed of mouse rotation, 1 is standard value")]
        public float MouseRotationSpeed = 1f; //!< The speed of rotation around the y-axis

        [Tooltip("The minimum angle limit for the camera rotation around the horizontal axis ")]
        public float MinHorizontalRotation = 0; //!< The minimum angle limit for the camera rotation around the x-axis 

        [Tooltip("The maximum angle limit for the camera rotation around the horizontal axis")]
        public float MaxHorizontalRotation = 100; //!< The maximum angle limit for the camera rotation around the x-axis

        [Tooltip("The speed of zooming in and out, 1 is standard")]
        public float ZoomSpeed = 1; //!< The speed of zooming in and out, 1 is standard 

        [Tooltip("The speed at which the zooming slows down, 1 is standard")]
        public float RotDamping = 1f; //!< The speed at which the zooming slows down, 1 is standard

        [Tooltip("The speed at which the panning slows down, 1 is standard")]
        public float PanDamping = 1; //!< The speed at which the panning slows down, 1 is standard

        [Tooltip("The speed at which the zooming slows down, 1 is standard")]
        public float ZoomDamping = 1f; //!< The speed at which the zooming slows down, 1 is standard

        [Tooltip("The speed of panning the camera in orthographic mode")]
        public float orthoPanSpeed = 1; //!< The speed of panning the camera in orthographic mode

        [Tooltip("The time to wait before starting the demo due to inactivity")]
        public float StartDemoOnInactivity = 5.0f; //!< The time to wait before starting the demo due to inactivity 

        [Tooltip("The time without any mouse activity before considering the camera inactive")]
        public float
            DurationNoMouseActivity = 0; //!< The time without any mouse activity before considering the camera inactive

        [Tooltip("A game object used for debugging purposes")]
        public GameObject DebugObj;

        [Header("Touch Controls")] [Tooltip("The touch interaction script")]
        public TouchInteraction Touch; //!< The touch interaction script 

        [Tooltip("The speed of pan speed with touch")]
        public float TouchPanSpeed = 1f; //!< The speed of rotating with touch

        [Tooltip("The speed of rotating with touch")]
        public float TouchRotationSpeed = 1f; //!< The speed of rotating with touch

        [Tooltip("The speed of zooming with touch")]
        public float TouchZoomSpeed = 1f; //!< The speed of zooming with touch

        [Tooltip("Invert vertical touch axis")]
        public bool TouchInvertVertical = false; //! Touch invert vertical

        [Tooltip("Invert horizohntal touch axis")]
        public bool TouchInvertHorizontal = false; //! Touch invert horizontal

        
        [Header("SpaceNavigator")] public bool EnableSpaceNavigator = true; //! Enable space navigator
        public float SpaceNavTransSpeed = 1; //! Space navigator translation speed

        [Header("Status")]  [ReadOnly] public float currentDistance; //! Current distance
        [ReadOnly] public float desiredDistance; //! Desired distance
        [ReadOnly] public Quaternion currentRotation; //! Current rotation
        [ReadOnly] public Quaternion desiredRotation; //! Desired rotation
        [ReadOnly] public bool isrotating = false;
        [ReadOnly] public bool CinemachineIsActive = false;
        
        private Quaternion rotation;
        private Vector3 position;
        private Camera mycamera;
        private float _lastmovement;
        private bool _demostarted;
        private float lastperspectivedistance;
        private Vector3 _pos;
        private bool startcameraposset = false;
        [HideInInspector] public bool orthograhicview = false;
        [HideInInspector] public OrthoViewController orthoviewcontroller;
   
        private bool selectionmanagernotnull = false;
        private SelectionRaycast selectionmanager;
        private GameObject selectedbefore;
        private float xDeg = 0.0f;
        private float yDeg = 0.0f;
        private Vector3 pointerbottomraycastpos;
        private Vector3 pointerbottomraycastposbefore;
        private Vector3 targetposition;
        private bool blockrotation;
        private bool blockrotationbefore;
        public bool blockleftmouserotation = false;
        private bool touchnotnull = false;
     
        private Vector3 zoomposviewport = Vector3.zero;
        private Vector3 zoomposworld = Vector3.zero;
        
        private HMI_Controller hmiController;
        
        
        public void OnButtonOrthoOverlay(GenericButton button)
        {
            orthoviewcontroller.OrthoEnabled = button.IsOn;
            orthoviewcontroller.UpdateViews();
        }


        public void OnButtonOrthographicView(GenericButton button)
        {
            SetOrthographicView(button.IsOn);
        }

        public void SetOrthographicView(bool active)
        {
            if (active == orthograhicview && Application.isPlaying)
                return; /// no changes
            orthograhicview = active;
            if (mycamera == null)
                mycamera = GetComponent<Camera>();
            mycamera.orthographic = active;
            if (!active)
            {
                desiredDistance = lastperspectivedistance;
                mycamera.farClipPlane = 5000f;
                mycamera.nearClipPlane = 0.1f;
            }
            else
            {
                lastperspectivedistance = desiredDistance;
                mycamera.farClipPlane = 5000f;
                mycamera.nearClipPlane = -5000f;
            }

            // change button in UI
            var button = Global.GetComponentByName<GenericButton>(Global.g4acontroller.gameObject, "Perspective");
            if (button != null)
                if (button.IsOn != active)
                    button.SetStatus(active);
        }

        void Start()
        {
            selectionmanagernotnull = GetComponent<SelectionRaycast>() != null;
            if (selectionmanagernotnull)
            {
                selectionmanager = GetComponent<SelectionRaycast>();
                selectionmanager.EventSelected.AddListener(OnSelected);
                selectionmanager.EventBlockRotation.AddListener(BlockRotation);
                selectionmanager.EventMultiSelect.AddListener(OnMultiSelect);
            }

            if (LastCameraPosition != null)
                if (SetCameraPosOnStartPlay)
                    LastCameraPosition.SetCameraPositionPlaymode(this);

#if UNITY_WEBGL
            RotateWithLeftMouseButton = true;
#endif
        }

        private void OnMultiSelect(bool multisel)
        {
         
           
        }
      

        public void BlockRotation(bool block, bool onlyleftmouse)
        {
            if (block)
                isrotating = false;
            
            blockrotation = block;
            
            if (onlyleftmouse)
            {
                blockrotation = false;
                blockleftmouserotation = block;
            }
              
        }

        private void OnSelected(GameObject go, bool selected, bool multiselect)
        {
            
       
        }

        void OnEnable()
        {
            if (Touch != null)
            {
                touchnotnull = true;
            }
            Init();
        }

        private void OnApplicationQuit()
        {
            if (LastCameraPosition != null)
                if (SaveCameraPosOnQuit)
                    LastCameraPosition.SaveCameraPosition(this);
        }


        public void OnViewButton(GenericButton button)
        {
            if (button.IsOn && FirstPersonController != null)
            {
                SetOrthographicView(false);
                if (CinemachineIsActive)
                    ActivateCinemachine(false);
                Global.SetActiveIncludingSubObjects(FirstPersonController.gameObject, true);
                FirstPersonControllerActive = true;
                FirstPersonController.SetActive(true);
            }
            else
            {
                FirstPersonControllerActive = false;
                Global.SetActiveIncludingSubObjects(FirstPersonController.gameObject, false);
            }
        }


        public void SetNewCameraPosition(Vector3 targetpos, float camdistance, Vector3 camrotation)
        {
            // End first person controller if it is on
            if (FirstPersonControllerActive)
            {
                FirstPersonController.SetActive(false);
                FirstPersonControllerActive = false;
            }

            if (target == null)
                return;
            desiredDistance = camdistance;
            currentDistance = camdistance;
            target.position = targetpos;
            targetposition = targetpos;
            desiredRotation = Quaternion.Euler(camrotation);
            currentRotation = Quaternion.Euler(camrotation);
            rotation = Quaternion.Euler(camrotation);
            transform.rotation = Quaternion.Euler(camrotation);
            
            // calculate position based on the new currentDistance 
            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);

            if (position != transform.position)
            {
                transform.position = position;
            }
            
        }

        public void SetViewDirection(Vector3 camrotation)
        {
            desiredRotation = Quaternion.Euler(camrotation);
            currentRotation = Quaternion.Euler(camrotation);
            rotation = Quaternion.Euler(camrotation);
            transform.rotation = Quaternion.Euler(camrotation);
        }

        public void ActivateCinemachine(bool activate)
        {
#if CINEMACHINE
            CinemachineBrain brain;
            brain = GetComponent<CinemachineBrain>();
            if (brain == null)
                return;
            
            if (!activate)
            {
                if (brain.ActiveVirtualCamera != null)
                {
                    Quaternion camrot = brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.rotation;
                    Vector3 rot = camrot.eulerAngles;
                    distance = Vector3.Distance(transform.position, target.position);
                    Vector3 tarpos = brain.ActiveVirtualCamera.VirtualCameraGameObject.transform.position +
                                     (camrot * Vector3.forward * distance + targetOffset);
                    SetNewCameraPosition(tarpos, distance, rot);
                }
            }
            if (brain != null)
            {
                if (activate)
                {
                    brain.enabled = true;

                }
                else
                {
                    brain.enabled = false;

                }
            }

            CinemachineIsActive = activate;


#endif
        }

#if CINEMACHINE
        public void ActivateCinemachineCam(CinemachineVirtualCamera vcam)
        {
            vcam.enabled = true;
            vcam.Priority = 100;
            if (CinemachineIsActive==false)
                ActivateCinemachine(true);
            
            // Set low priority to all other vcams
            var vcams = GameObject.FindObjectsOfType(typeof(CinemachineVirtualCamera));
            foreach (CinemachineVirtualCamera vc in vcams)
            {
                if (vc != vcam)
                    vc.Priority = 10;
            }
        }
#endif

        public void Init()
        {
#if CINEMACHINE
            ActivateCinemachine(false);
            hmiController = FindObjectOfType<HMI_Controller>();

#endif
            if (CalclulateMaxDistanceWithBoundingBox)
            {
                var rnds = FindObjectsOfType<Renderer>();
                if (rnds.Length != 0)
                {
                    var b = rnds[0].bounds;
                    for (int i = 1; i < rnds.Length; i++)
                    {
                        b.Encapsulate(rnds[i].bounds);
                    }

                    maxDistance = b.size.magnitude * 1.5f;
                }
            }

            //If there is no target, create a temporary target at 'distance' from the cameras current viewpoint
            if (!target)
            {
                GameObject go = new GameObject("Cam Target");
                go.transform.position = transform.position + (transform.forward * distance);
                target = go.transform;
            }

            mycamera = GetComponent<Camera>();

            distance = Vector3.Distance(transform.position, target.position);
            currentDistance = distance;
            desiredDistance = distance;

            //be sure to grab the current rotations as starting points.
            position = transform.position;
            rotation = transform.rotation;
            currentRotation = transform.rotation;
            desiredRotation = transform.rotation;

            xDeg = Vector3.Angle(Vector3.right, transform.right);
            yDeg = Vector3.Angle(Vector3.up, transform.up);


            if (LastCameraPosition != null && !FirstPersonControllerActive && !startcameraposset)
            {
                if (SetCameraPosOnStartPlay)
                {
                    SetNewCameraPosition(LastCameraPosition.TargetPos, LastCameraPosition.CameraDistance,
                        LastCameraPosition.CameraRot);
                }

                startcameraposset = true;
            }

            if (FirstPersonController != null)
            {
                if (FirstPersonControllerActive)
                {
                    FirstPersonController.SetActive(true);
                }
                else
                {
                    FirstPersonController.SetActive(false);
                }
            }
            #if (UNITY_EDTOR_LINUX || UNITY_STANDALONE_LINUX)
            		DPIScale = 1;
            #else
             		DPIScale = 144/Screen.dpi;
            #endif
            orthoviewcontroller = this.transform.parent.GetComponentInChildren<OrthoViewController>();
            
        }

        void CameraTransform(Vector3 direction)
        {
            target.rotation = transform.rotation;
            target.Translate(direction * PanDamping);
            _lastmovement = Time.realtimeSinceStartup;
        }

        void CamereSetDirection(Vector3 direction)
        {
            desiredDistance = 10f;
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        bool MouseOverViewport(Camera main_cam, Camera local_cam)
        {
            if (!Input.mousePresent) return true; //always true if no mouse??

            Vector3 main_mou = main_cam.ScreenToViewportPoint(Input.mousePosition);
            return local_cam.rect.Contains(main_mou);
        }

        Vector3 RayCastToBottom(bool touch = false)
        {
            Ray ray;
            if (!touch)
                ray = mycamera.ScreenPointToRay(Input.mousePosition);
            else
            {
                ray = mycamera.ScreenPointToRay(Touch.TouchPos);
            }
            
            float distance;
            // find distance to the bottom plane
            Plane bottom = new Plane(Vector3.up, Vector3.zero);
            if (bottom.Raycast(ray, out  distance))
            {
                return ray.GetPoint(distance);
            }

            // now raycast to the plane in front (parallel) of the camere going through the bottom
            Plane plane = new Plane(mycamera.transform.forward, mycamera.transform.position + transform.forward * distance);
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }
        
        Vector3 RayCastToBottomZoom(bool touch = false)
        {
            Ray ray;
            if (!touch)
                ray = mycamera.ScreenPointToRay(Input.mousePosition);
            else
            {
                ray = mycamera.ScreenPointToRay(Touch.TwoFingerMiddlePos);
            }

            Plane plane = new Plane(Vector3.up, Vector3.zero);
            // raycast from mouseposition to this plane
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        /*
     * Camera logic on LateUpdate to only update after all character movement logic has been handled. 
     */
        void LateUpdate()
        {
#if CINEMACHINE
            if((hmiController!=null &&hmiController.BlockUserMouseNavigation) )
                return;
              
#endif
          
            // Check if Mouse is over UI element
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            // Check Touch Status
            var istouching = false;
            var touchpanning = false;
            var starttouch = false;
            var touchrotate = false;
            var endtouch = false;
            var iszooming = false;
            var istwofinger = false;

            if (touchnotnull)
            {
                istouching = Touch.IsTouching;
                touchpanning = Touch.IsTwoFinger;
                starttouch = Touch.IsBeginPhase;
                istwofinger = Touch.IsTwoFinger;
                endtouch = Touch.IsEndPhase;
                iszooming = Touch.TwoFingerDeltaDistance != 0;
                if (istouching && !touchpanning)
                    touchrotate = true;
            }

            var buttonrotate = 1;
            if (RotateWithLeftMouseButton)
                buttonrotate = 0;
            
            bool MouseInOrthoCamera = false;
            Camera incamera = mycamera;
            if (Camera.allCameras.Length > 1)
                foreach (var cam in Camera.allCameras)
                {
                    if (cam != Camera.main)
                    {
                        if (MouseOverViewport(mycamera, cam))
                        {
                            MouseInOrthoCamera = true;
                            incamera = cam;
                        }
                    }
                }

            if (FirstPersonControllerActive)
                return;

            if (UseOrbitCameraMode)
                return;

            if (CinemachineIsActive)
            {
                var scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Input.GetMouseButton(2) || Input.GetMouseButton(3) || Input.GetMouseButton(1)
                    || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl) ||
                    Input.GetKey(KeyCode.RightControl)
                    || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) ||
                    Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.UpArrow) || Math.Abs(scroll) > 0.001f ||
                    Input.GetKey(KeyCode.Escape))
                {
                    ActivateCinemachine(false);
                }
            }

            // Set init position when mouse bottom is going down
            if (Input.GetMouseButtonDown(2) || starttouch)
            {
                pointerbottomraycastposbefore = RayCastToBottom(istouching);
                targetposition = target.position;
            }

            if (Input.GetMouseButtonDown(buttonrotate) || starttouch)
            {
                if (!blockrotation)
                {
                    // Debug.Log("Start Rotation");
                    desiredRotation = transform.rotation;
                    xDeg = transform.rotation.eulerAngles.y;
                    yDeg = transform.rotation.eulerAngles.x;
                    isrotating = true;
                }
            }

            if (Input.GetMouseButtonUp(buttonrotate) || endtouch || istwofinger)
            {
                isrotating = false;
                //Debug.Log("End Rotation" + endtouch + " twofinger " + istwofinger);
            }

            // Check for Panning
            if (Input.GetMouseButton(2) || touchpanning)
            {
                pointerbottomraycastpos = RayCastToBottom(istouching);
                
            }

            // If Control and Middle button? ZOOM!
            if (Input.GetMouseButton(2) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
               
                desiredDistance -=  Input.GetAxis("Mouse Y") * Time.deltaTime * ZoomSpeed * DPIScale * 6 * Mathf.Abs(currentDistance);
            }
            
            // If right mouse (or left if rotation is with left) is selected ORBIT
            else if (isrotating && !blockleftmouserotation && !blockrotation)
            {
                _lastmovement = Time.realtimeSinceStartup;
                if (!touchrotate)
                {
                    var scale = 0.05f* DPIScale * MouseRotationSpeed * 100;
                    xDeg += Input.GetAxis("Mouse X") * scale;
                    yDeg -= Input.GetAxis("Mouse Y") * scale;
                }
                else
                {
                    xDeg += Touch.TouchDeltaPos.x * DPIScale* TouchRotationSpeed * 400 * 0.001f;
                    yDeg -= Touch.TouchDeltaPos.y * DPIScale* TouchRotationSpeed * 400 * 0.001f;
                }

                //Clamp the vertical axis for the orbit
                yDeg = ClampAngle(yDeg, MinHorizontalRotation, MaxHorizontalRotation);
                // set camera rotation 
                desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
                currentRotation = transform.rotation;
                
                rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * RotDamping *3f);
                transform.rotation = rotation;
            }
            // otherwise if middle mouse is selected, we pan by way of transforming the target in screenspace
            else if (Input.GetMouseButton(2) || touchpanning)
            {
                if (!MouseInOrthoCamera)
                {
                    var delta = pointerbottomraycastposbefore - pointerbottomraycastpos;
                    _lastmovement = Time.realtimeSinceStartup;
                    targetposition = targetposition + delta;
                 
                }
                else
                {
                    if (orthoviewcontroller != null)
                    {
                        if (incamera.name == "Side")
                        {
                            orthoviewcontroller.transform.Translate(Vector3.right * -Input.GetAxis("Mouse X") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                            orthoviewcontroller.transform.Translate(Vector3.up * -Input.GetAxis("Mouse Y") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                        }

                        if (incamera.name == "Top")
                        {
                            orthoviewcontroller.transform.Translate(new Vector3(0, 0, -1) * -Input.GetAxis("Mouse X") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                            orthoviewcontroller.transform.Translate(new Vector3(1, 0, 0) * -Input.GetAxis("Mouse Y") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                        }

                        if (incamera.name == "Front")
                        {
                            orthoviewcontroller.transform.Translate(new Vector3(0, 0, -1) * -Input.GetAxis("Mouse X") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                            orthoviewcontroller.transform.Translate(new Vector3(0, 1, 0) * -Input.GetAxis("Mouse Y") *
                                orthoPanSpeed * orthoviewcontroller.Distance / 10);
                        }
                    }
                }
            }


            /// Zoom in and out
            // affect the desired Zoom distance if we roll the scrollwheel
            var mousescroll = Input.GetAxis("Mouse ScrollWheel");
            if (mousescroll != 0 || istwofinger)
            {
                _lastmovement = Time.realtimeSinceStartup;

                if (ZoomToMousePos)
                {
                    zoomposworld = RayCastToBottomZoom(istouching);
                    zoomposviewport = Camera.main.WorldToViewportPoint(zoomposworld);
                }
   
               
                if (!MouseInOrthoCamera)
                {
                    if (!iszooming)
                        desiredDistance -= mousescroll * 0.05f * ZoomSpeed * 65 * Mathf.Abs(currentDistance);
                    else
                    {
                        #if UNITY_WEBGL && !UNITY_EDITOR
                        desiredDistance -= Touch.TwoFingerDeltaDistance * TouchZoomSpeed * 0.0042f * DPIScale*
                                           Mathf.Abs(currentDistance);
                        #else
                        desiredDistance -= Touch.TwoFingerDeltaDistance* TouchZoomSpeed * 0.0042f * DPIScale*
                                           Mathf.Abs(currentDistance);
                        #endif
                    }
                }
                else
                {
                    if (orthoviewcontroller != null)
                    {
                        orthoviewcontroller.Distance += mousescroll * orthoviewcontroller.Distance;
                        orthoviewcontroller.UpdateViews();
                    }
                }

                //clamp the zoom min/max
                desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
            }


            // if hotkey focus is pressed and selectionamanger has a selected object, focus it
            if (selectionmanagernotnull)
            {
                if ((Input.GetKey(KeyCode.F) || (selectionmanager.DoubleSelect &&
                                                 (selectionmanager.FocusDoubleClickedObject ||
                                                  selectionmanager.ZoomDoubleClickedObject))) &&
                    selectionmanager.SelectedObject != null)
                {
                    _lastmovement = Time.realtimeSinceStartup;
                    var pos = selectionmanager.GetHitpoint();
                    selectionmanager.ShowCenterIcon(true);
                    // get bounding box of all children of selected object

                    Bounds combinedBounds = new Bounds();

// Get the renderer for each child object and combine their bounds
                    foreach (Renderer renderer in selectionmanager.SelectedObject.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer != null)
                        {
                            if (combinedBounds.size == Vector3.zero)
                            {
                                combinedBounds = renderer.bounds;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(renderer.bounds);
                            }
                        }
                    }

                    float cameraDistance = 2.0f;
                    Vector3 objectSizes = combinedBounds.max - combinedBounds.min;
                    float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
                    float cameraView =
                        2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad *
                                         mycamera.fieldOfView); // Visible height 1 meter in front
                    float distance =
                        cameraDistance * objectSize / cameraView; // Combined wanted distance from the object
                    distance += 0.5f * objectSize; // Estimated offset from the center to the outside of the object
                    if (!RotateToFocusObject)
                    {
                        // If not rotate to target object on focus then just move the targetposition (=panning the camera)
                        targetposition = pos;
                    }
                    else
                    {
                        targetposition = pos;
                        var tonewtarget = pos-this.position;
                        // if rotation is wished - calculate desired rotation and new distance (camera should not move)
                        desiredDistance = Vector3.Magnitude(tonewtarget);
                        //desiredRotation = Quaternion.LookRotation(targetposition-this.position,this.transform.up);
                    
                        desiredRotation = Quaternion.LookRotation(tonewtarget,this.transform.up);
                        
                        var euler = desiredRotation.eulerAngles;
                        desiredRotation = Quaternion.Euler(euler.x, euler.y, 0);
                        
                        
                    }
                        
                    if (selectionmanager.ZoomDoubleClickedObject || Input.GetKey(KeyCode.F))
                        desiredDistance = distance;
                }

                if (selectionmanager.SelectedObject != null &&
                    ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ||
                     selectionmanager.AutoCenterSelectedObject))
                {
                    var pos = selectionmanager.GetHitpoint();
                    selectionmanager.ShowCenterIcon(true);
                    if (!RotateToFocusObject)
                    {
                        // If not rotate to target object on focus then just move the targetposition (=panning the camera)
                        targetposition = pos;
                    }
                    else
                    { 
                        // if rotation is wished - calculate desired rotation and new distance (camera should not move)
                        targetposition = pos;
                        var tonewtarget = pos-this.position;
                        // if rotation is wished - calculate desired rotation and new distance (camera should not move)
                        desiredDistance = Vector3.Magnitude(tonewtarget);
                        //desiredRotation = Quaternion.LookRotation(targetposition-this.position,this.transform.up);
                    
                        desiredRotation = Quaternion.LookRotation(tonewtarget,this.transform.up);
                        
                        var euler = desiredRotation.eulerAngles;
                        desiredRotation = Quaternion.Euler(euler.x, euler.y, 0);
                    }
                }
            }

            if (!MouseInOrthoCamera)
            {
                // Key Navigation
                var shift = false;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    shift = true;
                var control = false;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    control = true;
                // Key 3D Navigation
                if (Input.GetKey(KeyCode.UpArrow) && shift && !control)
                    CameraTransform(Vector3.forward);

                if (Input.GetKey(KeyCode.DownArrow) && shift && !control)
                    CameraTransform(Vector3.back);

                if (Input.GetKey(KeyCode.UpArrow) && !shift && !control)
                    CameraTransform(Vector3.down);

                if (Input.GetKey(KeyCode.DownArrow) && !shift && !control)
                    CameraTransform(Vector3.up);

                if (Input.GetKey(KeyCode.RightArrow) && !control)
                    CameraTransform(Vector3.left);

                if (Input.GetKey(KeyCode.LeftArrow) && !control)
                    CameraTransform(Vector3.right);

                if (Input.GetKey(KeyCode.LeftArrow) && control)
                    CamereSetDirection(Vector3.left);

                if (realvirtualController.EnableHotkeys)
                {
                    if (Input.GetKey(realvirtualController.HotKeyTopView))
                    {
                        SetViewDirection(new Vector3(90, 90, 0));
                    }

                    if (Input.GetKey(realvirtualController.HotKeyFrontView))
                    {
                        if (selectionmanagernotnull && realvirtualController.HotKeyFrontView ==
                            realvirtualController.HoteKeyFocfus)
                            if (selectionmanager.SelectedObject == null)
                                SetViewDirection(new Vector3(0, 90, 0));
                            else
                                SetViewDirection(new Vector3(0, 90, 0));
                    }

                    if (Input.GetKey(realvirtualController.HotKeyBackView))
                    {
                        SetViewDirection(new Vector3(0, 180, 0));
                    }

                    if (Input.GetKey(realvirtualController.HotKeyLeftView))
                    {
                        SetViewDirection(new Vector3(0, 180, 0));
                    }

                    if (Input.GetKey(realvirtualController.HotKeyRightView))
                    {
                        SetViewDirection(new Vector3(0, 0, 0));
                    }
                }
            }
            else
            {
                if (realvirtualController.EnableHotkeys)
                {
                    if (Input.GetKeyDown(realvirtualController.HotKeyOrhtoBigger))
                        orthoviewcontroller.Size += 0.05f;
                    if (Input.GetKeyDown(realvirtualController.HotKeyOrhtoSmaller))
                        orthoviewcontroller.Size -= 0.05f;
                    if (orthoviewcontroller.Size > 0.45f)
                        orthoviewcontroller.Size = 0.45f;
                    if (orthoviewcontroller.Size < 0.1f)
                        orthoviewcontroller.Size = 0.1f;
                    if (Input.GetKeyDown(realvirtualController.HoteKeyOrthoDirection))
                        orthoviewcontroller.Angle += 90;
                    if (orthoviewcontroller.Angle >= 360)
                        orthoviewcontroller.Angle = 0;
                    orthoviewcontroller.UpdateViews();
                }
            }

            if (realvirtualController.EnableHotkeys)
                if (Input.GetKeyDown(realvirtualController.HotKeyOrthoViews))
                {
                    orthoviewcontroller.OrthoEnabled = !orthoviewcontroller.OrthoEnabled;
                    var button =
                        Global.GetComponentByName<GenericButton>(realvirtualController.gameObject, "OrthoViews");
                    if (button != null)
                        button.SetStatus(orthoviewcontroller.OrthoEnabled);
                    orthoviewcontroller.UpdateViews();
                }

            if (mycamera.orthographic)
            {
                mycamera.orthographicSize += mousescroll * mycamera.orthographicSize;
                desiredDistance = 0;
            }
#if ((!UNITY_IOS && !UNITY_ANDROID && !UNITY_EDITOR_OSX && !UNITY_WEBGL) || (UNITY_EDITOR && !UNITY_WEBGL && !UNITY_EDITOR_OSX))
            // Space Navigator
            if (EnableSpaceNavigator)
            {
                if (SpaceNavigator.Translation != Vector3.zero)
                {
                    target.rotation = transform.rotation;
                    var spacetrans = SpaceNavigator.Translation;
                    var newtrans = new Vector3(-spacetrans.x, spacetrans.y, -spacetrans.z) * SpaceNavTransSpeed;
                    target.Translate(newtrans, Space.Self);
                }

                if (SpaceNavigator.Rotation.eulerAngles != Vector3.zero)
                {
                   
                    transform.Rotate(-SpaceNavigator.Rotation.eulerAngles);
                    rotation = transform.rotation;
                }
            }
#endif

            currentRotation = transform.rotation;
            if (desiredRotation != currentRotation)
            {
               rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * RotDamping *3f);
               transform.rotation = rotation;
            }
               

            // Lerp the target movement
            target.position = Vector3.Lerp(target.position, targetposition, Time.deltaTime * PanDamping * 5);
            //target.position = targetposition;
            
            // For smoothing of the zoom, lerp distance
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * ZoomDamping * 10.0f);

            // calculate position based on the new currentDistance 
            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);

            if (position != transform.position)
            {
                transform.position = position;
            }
            
            // Zoom movement to the side to exactly zoom at pointer position
            if (zoomposworld != Vector3.zero && ZoomToMousePos)
            {
                var worldpos = Camera.main.ViewportToWorldPoint(zoomposviewport);
                var delta = zoomposworld-worldpos;
                targetposition -= delta;
                //Debug.Log("ZoomPos World " +  zoomposworld  + "ZoomPosViewport" + zoomposviewport + "Current Worldpos " + worldpos + "Delta " + delta);
                
                // calculate position based on the new currentDistance 
                position = targetposition - (rotation * Vector3.forward * currentDistance + targetOffset);

                if (position != transform.position)
                {
                    transform.position = position;
                }
                
            }  
            
            zoomposworld = Vector3.zero;

            DurationNoMouseActivity = Time.realtimeSinceStartup - _lastmovement;
            
            pointerbottomraycastposbefore = RayCastToBottom(istouching);

            blockrotationbefore = blockrotation;

#if CINEMACHINE
            if ((Time.realtimeSinceStartup - _lastmovement) > StartDemoOnInactivity)
            {
                if (!CinemachineIsActive)
                   ActivateCinemachine(true);
            }
#endif
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
    }
}