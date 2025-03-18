using System.Collections.Generic;
using NaughtyAttributes;
using RuntimeInspectorNamespace;
using UnityEngine.Rendering;

using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace realvirtual
{
#pragma warning disable 0108
    [System.Serializable]
    public class Game4AutomationEventSelected: UnityEvent<GameObject, bool, bool>
    {
    }

    [System.Serializable]
    public class Game4AutomationEventHovered: UnityEvent<GameObject, bool>
    {
    }
    
    [System.Serializable]
    public class GameAutomationEventBlockRotation: UnityEvent<bool,bool>
    {
    }
    
    [System.Serializable]
    public class Game4AutomationEventDoubleClicked: UnityEvent<GameObject>
    {
    }
    
    [System.Serializable]
    public class Game4AutomationEventLongPressed: UnityEvent<GameObject>
    {
    }
    
    [System.Serializable]
    public class Game4AutomationEventMouseDownOnObject: UnityEvent<GameObject>
    {
    }

    
    [System.Serializable]
    public class Game4AutomationEventMultiSelect:  UnityEvent<bool>
    {
    }
    
    [System.Serializable]
    public class Game4AutomationEventMultiSelectEmpty:  UnityEvent<bool>
    {
    }


    //! Selection Raycast for selecting objects during runtime
    public class SelectionRaycast : realvirtualBehavior
    {
        public bool AlwaysOn = false; //!<if true the selection is always on even if button is turned off in game4automation controller
        public bool IsActive = true; //!<selection by raycast is active
        public bool EnableToch = true; //!<allow touch input
        public bool AutomaticallyAddColliders = true;
        public bool ChangeMaterialOnHover = true; //!<change material on hover
        public bool EnableMultiSelect = true; //!<enable multi select
        [ShowIf("ChangeMaterialOnHover")]public Material HighlightMaterial; //!<the highlight materiials
        public bool ChangeMaterialOnSelect = true; //!<change material on select
        [ShowIf("ChangeMaterialOnSelect")]public Material SelectMaterial;//!<the select material
        [ReorderableList] public List<string> SelectionLayer; //!<the layers that can be selected
        public string UILayer = "UI"; //!<the layer for the UI - selection will be disabled if UI is touched
        public string ContextToolbarLayer = "g4a ContextToolbar"; //!<the layer for the toolbar - selection will be kept active if toolbar is touched

        [ReadOnly] public GameObject TouchedObject;
        [ReadOnly] public bool ObjectIsSelected;
        [ReadOnly] public GameObject SelectedObject;//!<the selected object
        [ReadOnly] public List<GameObject> SelectedObjects = new List<GameObject>(); //!<the selected objects on multiselect
        [ReadOnly] public Vector3 SelectedPosition; //!<the selected object hit point position
        [ReadOnly] public bool ObjectIsHovered;
        [ReadOnly] public GameObject HoveredObject;//!<the hovered object
        [ReadOnly] public Vector3 HoveredPosition; //!<the hovered object hit point position
        [ReadOnly] public bool MultiSelectModeIsOn;
        [ReadOnly] public bool DoubleSelect;
        [ReadOnly] public bool LongPressed;
        [ReadOnly] public bool OnUI; 
        [ReadOnly] public bool OnContextToolbar; //!<true if the object was double clicked
        [ReadOnly] public bool MouseDownOnObject; //!<true if the mouse was going down on a selectable object
        public bool PingHoverObject; //!<true if the hovered object should be pinged in the hierarchy
        public bool SelectHoverObject; //!<true if the hovered object should be selected in the hierarchy
        public bool PingSelectObject; //!<true if the selected object should be pinged in the hierarchy
        public bool SelectSelectObject; //!<true if the selected object should be selected in the hierarchy
        public bool AutoCenterSelectedObject; //!<true if the selected object (its selection point) is automatically centered in the scene view
        public bool ZoomDoubleClickedObject=true; //!<true if the selected object (its selection point) is automatically centered in the scene view when double clicking on it
        public bool FocusDoubleClickedObject=true; //!<true if the selected object (its selection point) is automatically centered in the scene view when double clicking on it
        public bool OpenRuntimeINspector;
        public bool ShowSelectedIcon;
        public float TimeDoubleClick = 1.0f; //!<time in seconds for double click    
        public float TimeLongPress = 1.0f; //!<time in seconds for long press
        [Foldout("Events")] public Game4AutomationEventSelected
            EventSelected; //!<  Unity event which is called for MU enter and exit. On enter it passes MU and true. On exit it passes MU and false.
        [Foldout("Events")] public Game4AutomationEventHovered
            EventHovered; //!<  Unity event which is called for MU enter and exit. On enter it passes MU and true. On exit it passes MU and false.
        [Foldout("Double Click")] public Game4AutomationEventDoubleClicked
            EventDoubleClicked; //!<  Unity event which is called for MU enter and exit. On enter it passes MU and true. On exit it passes MU and false.
        [Foldout("Events")] public GameAutomationEventBlockRotation
            EventBlockRotation; //!<  Unity event which is called when rotation should be blocked
        [Foldout("Events")] public Game4AutomationEventLongPressed
            EventLongPressed; //!<  Unity event which is called when rotation should be blocked
        [Foldout("Events")] public Game4AutomationEventMultiSelect
            EventMultiSelect; //!<  Unity event which is called when rotation should be blocked]
        [Foldout("Events")] public Game4AutomationEventMultiSelectEmpty
            EventMultiSelectEmpty; //!<  Unity event which is called when rotation should be blocked
        [Foldout("Events")] public Game4AutomationEventMouseDownOnObject
            EventMouseDownOnObject; //!<  Unity event which is called when mouse is down on an object
        public RuntimeInspector RuntimeInspector;
        public GameObject SelectedIcon;
        public bool DebugMode;

        private Vector3 Hitpoint;
        private Vector3 distancehitpoint;
        private RaycastHit GObject;
        private SceneMouseNavigation navigate;
        private int layermask;
        private int uilayer;
        private int toolbarlayer;
        private Camera camera;
        private List<ObjectSelection> selections = new List<ObjectSelection>();
        private List<ObjectSelection> hovers = new List<ObjectSelection>();
        private bool isactivebefore = false;
        private GameObject selectedicon;
        private Material lineMaterial;
        private bool inittouch = false;
        private float timeselected;
        private Vector2 mousepositiononselect;
        private GameObject hovered;
        private GameObject clicked;

        public void SelectObject(GameObject obj)
        {
            SelectedObject = obj;
            if (DebugMode) Debug.Log("Selected: " + obj.name);
            if (!SelectedObjects.Contains(obj)) SelectedObjects.Add(obj);
            SelectedPosition = Hitpoint;
            ObjectIsSelected = true;
            var selectablenew = obj.GetComponent<ISelectable>();
            if (selectablenew != null) selectablenew.OnSelected(MultiSelectModeIsOn);
            if (EventSelected != null) EventSelected.Invoke(obj, true, MultiSelectModeIsOn);
            HighlightSelectObject(true,obj);
            SelectedPosition = Hitpoint;
            distancehitpoint = SelectedObject.transform.position - Hitpoint;
            if (OpenRuntimeINspector)
            {
                // enable gameobject runtimeinspector
                if (RuntimeInspector != null && realvirtualController.RuntimeInspectorEnabled)
                {
                    RuntimeInspector.gameObject.SetActive(true);
                    RuntimeInspector.Inspect(SelectedObject);
                }
            }
        
            // On Touch block rotation and register it as touched
            if (Input.touchCount ==1  && TouchedObject != obj)
            {
                inittouch = true;
            }
        }

        public void DeSelectObject(GameObject obj)
        {
            if (obj == null) return;
            var selectableold  = obj.GetComponent<ISelectable>();
            if (selectableold != null) selectableold.OnDeselected();
            if (EventSelected != null ) EventSelected.Invoke(obj, false, MultiSelectModeIsOn);
            HighlightSelectObject(false,obj);
            SelectedObjects.Remove(obj);
            SelectedObject = null;
            if (MultiSelectModeIsOn && SelectedObjects.Count == 0)
            {
                MultiSelectOn(false);
                if (EventMultiSelectEmpty != null) EventMultiSelectEmpty.Invoke(true);
            }
        }

        public void MultiSelectOn(bool turnon)
        {
            MultiSelectModeIsOn = turnon;
            if (turnon == false)
            {
                foreach (var selected in SelectedObjects.ToArray())
                {
                    DeSelectObject(selected);
                }
            }
        }

        public void DeSelectObject()
        {
            if (SelectedObject != null)
              DeSelectObject(SelectedObject);
            
            if (MultiSelectModeIsOn)
            {
                MultiSelectOn(false);
                if (EventMultiSelectEmpty != null) EventMultiSelectEmpty.Invoke(true);
            }
        }
        
        public void DeSelectIfNotThis(GameObject obj)
        {
            if (SelectedObject != null && SelectedObject != obj)
                DeSelectObject(SelectedObject);
        }
        
        public Vector3 GetHitpoint()
        {
            return SelectedObject.transform.position - distancehitpoint;
        }

        public void ShowCenterIcon(bool show)
        {
            if (show)
            {
                if (SelectedIcon != null && ShowSelectedIcon)
                {
                    if (selectedicon == null)
                        selectedicon = Instantiate(SelectedIcon, Hitpoint, Quaternion.identity);
                    selectedicon.transform.position = GetHitpoint();
                }
            }
            else
            {
                if (selectedicon != null)
                    DestroyImmediate(selectedicon);
            }
        }

        //Returns 'true' if we touched or hovering on Unity UI element.
        public bool IsPointerOverUIElement()
        {
            return IsPointerOverUIElement(GetEventSystemRaycastResults());
        }
 
 
        //Returns 'true' if we touched or hovering on Unity UI element.
        private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
        {
            for (int index = 0; index < eventSystemRaysastResults.Count; index++)
            {
                RaycastResult curRaysastResult = eventSystemRaysastResults[index];
                if (curRaysastResult.gameObject.layer == uilayer)
                    return true;
            }
            return false;
        }
        
        public bool IsPointerOverToolbar()
        {
            return IsPointerOverToolbar(GetEventSystemRaycastResults());
        }
        
        private bool IsPointerOverToolbar(List<RaycastResult> eventSystemRaysastResults)
        {
            for (int index = 0; index < eventSystemRaysastResults.Count; index++)
            {
                RaycastResult curRaysastResult = eventSystemRaysastResults[index];
                if (curRaysastResult.gameObject.layer == toolbarlayer)
                    return true;
            }
            return false;
        }
 
 
        //Gets all event system raycast results of current mouse or touch position.
        static List<RaycastResult> GetEventSystemRaycastResults()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raysastResults);
            return raysastResults;
        }

        
        private void HighlightHoverObject(bool highlight, GameObject currObj)
        {
            var meshrenderer = currObj.GetComponentInChildren<MeshRenderer>();
            if (highlight)
            {
                HoveredPosition = Hitpoint;
                if (ChangeMaterialOnHover)
                {
                    var mu = currObj.GetComponent<MU>();
                    if (mu != null)
                    {
                        var meshes = mu.GetComponentsInChildren<MeshRenderer>();
                        foreach (var mesh in meshes)
                        {
                            var sel = mesh.gameObject.AddComponent<ObjectSelection>();
                            sel.SetNewMaterial(HighlightMaterial);
                            hovers.Add(sel);
                        }
                    }
                    else
                    {
                        var sel = currObj.AddComponent<ObjectSelection>();
                        sel.SetNewMaterial(HighlightMaterial);
                        hovers.Add(sel);
                    }
                }

#if UNITY_EDITOR
                if (PingHoverObject)
                    EditorGUIUtility.PingObject((currObj));
                if (HoveredObject)
                    if (SelectHoverObject)
                        Selection.objects = new[] {currObj};
#endif
            }
            else
            {
                HoveredPosition = Vector3.zero;
                if (ChangeMaterialOnHover)
                {
                    foreach (var hover in hovers)
                    {
                        hover.ResetMaterial();
                    }
                    hovers.Clear();
                }
            
            }
        }

        private void HighlightSelectObject(bool highlight, GameObject currObj)
        {
            MU mu = null;
            if (currObj != null)
                mu = currObj.GetComponent<MU>();
           
            if (highlight)
            {

#if UNITY_EDITOR
                if (PingSelectObject)
                    EditorGUIUtility.PingObject((currObj));
                if (SelectSelectObject)
                    Selection.objects = new[] {currObj};
#endif
                if (!ChangeMaterialOnSelect)
                    return;

                if (mu != null)
                {
                    var meshes = mu.GetComponentsInChildren<MeshRenderer>();
                    foreach (var mesh in meshes)
                    {
                        var sel = mesh.gameObject.AddComponent<ObjectSelection>();
                        sel.SetNewMaterial(SelectMaterial);
                        selections.Add(sel);
                    }
                }
                else
                {
                    var sel = currObj.AddComponent<ObjectSelection>();
                    sel.SetNewMaterial(SelectMaterial);
                    selections.Add(sel);
                }

            

            }
            else
            {
                if (!ChangeMaterialOnSelect)
                    return;

                foreach (var sel in selections)
                {
                    sel.ResetMaterial();
                }
                selections.Clear();
            }
        }
        
        
        public void CheckPointerBackground()
        {
            OnUI = IsPointerOverUIElement();
            OnContextToolbar = IsPointerOverToolbar();
        }
        

        new void Awake()
        {
            base.Awake();

            if (!realvirtualController.ObjectSelectionEnabled && !AlwaysOn)
            {
                return;
            }
        
            
            camera = GetComponent<Camera>();
            // get all meshrenderers
            var meshrenderers = FindObjectsOfType<MeshRenderer>();

            if (AutomaticallyAddColliders)
            {
                foreach (var comp in meshrenderers)
                {
                    if (comp.gameObject.GetComponent<Collider>() == null)
                    {
                        // get mesh from gameobject
                        try
                        {
                            var collider = comp.gameObject.AddComponent<MeshCollider>();
                            collider.convex = true;
                        }
                        catch
                        {
                        }
                        comp.gameObject.layer = LayerMask.NameToLayer(SelectionLayer[0]);
                    }
                    else
                    {
                        var collider = comp.GetComponent<Collider>();
                        // check if collider is on default layer
                        if (collider.gameObject.layer == 0)
                        {
                            // set layer to selection layer
                            collider.gameObject.layer = LayerMask.NameToLayer(SelectionLayer[0]);
                        }
                    }
                }
            }
         
            navigate = gameObject.GetComponent<SceneMouseNavigation>();
            base.Awake();
            uilayer = LayerMask.NameToLayer(UILayer);
            toolbarlayer = LayerMask.NameToLayer(ContextToolbarLayer);
            layermask = LayerMask.GetMask(SelectionLayer.ToArray());
            
        }
        
        void Start()
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);
        }
        
        void ChangeHovered(GameObject oldhovered , GameObject newhovered)
        {
            HoveredObject = newhovered;
            if (oldhovered != null)
            {
                if (DebugMode) Debug.Log("Unhovered: " + oldhovered.name);
                var selectableold  = oldhovered.GetComponent<ISelectable>();
                if (selectableold != null) selectableold.OnUnhovered();
                if (EventHovered != null && oldhovered != null) EventHovered.Invoke(oldhovered, false);
                HighlightHoverObject(false,oldhovered);
            }

            if (newhovered != null)
            {
                if (EventBlockRotation != null) EventBlockRotation.Invoke(true,true);
                if (DebugMode) Debug.Log("Hovered: " + newhovered.name);
                ObjectIsHovered = true;
                var selectablenew = newhovered.GetComponent<ISelectable>();
                if (selectablenew != null) selectablenew.OnHovered();
                if (EventHovered != null && newhovered != null) EventHovered.Invoke(newhovered, true);
                HighlightHoverObject(true,newhovered);
            }
            else
            {
                ObjectIsHovered = false;
                if (EventBlockRotation != null) EventBlockRotation.Invoke(false,true);
            }
        }
        
        void ChangeSelected(GameObject oldselected , GameObject newselected)
        {
            if (oldselected == newselected)
            {
                if (EventSelected != null ) EventSelected.Invoke(oldselected, false, MultiSelectModeIsOn);
                if (oldselected != null)
                {
                    var selectableold  = oldselected.GetComponent<ISelectable>();
                    if (selectableold != null) selectableold.OnSelected(MultiSelectModeIsOn);
                }
                return;
            }

            if (oldselected != null)
            {
                if (!MultiSelectModeIsOn )
                {
                   DeSelectObject(oldselected);
                }
            }

            if (newselected != null)
            {
               SelectObject(newselected);
            }
            else
            {
                SelectedObject = null;
                SelectedPosition = Vector3.zero;
                distancehitpoint = Vector3.zero;
                ObjectIsSelected = false;
                if (OpenRuntimeINspector)
                {
                    if (RuntimeInspector != null && realvirtualController.RuntimeInspectorEnabled)
                    {
                        RuntimeInspector.StopInspect();
                        RuntimeInspector.gameObject.SetActive(false);
                    }
                }
                ShowCenterIcon(false);
            }
        }

        
        // Update is called once per frame
        void Update()
        {
 
            DoubleSelect = false;
            // turn component off if isactive = false
            if ((!IsActive || !realvirtualController.ObjectSelectionEnabled) && !AlwaysOn)
            {
                // hide selected and hovered objects which have been selected before
                if (isactivebefore && !IsActive)
                {
                    if (SelectedObject != null)
                    {
                        ChangeSelected(SelectedObject,null);
                  
                    }

                    if (HoveredObject != null)
                    {
                        ChangeHovered(HoveredObject, null);
                    }
                }
                isactivebefore = false;
                return;
            }
            

            // Init variables
            isactivebefore = true;
            hovered = null;
            bool touched = false;
            bool hit = false;
            bool isclicked = false;
            bool dragged = false;
            Touch touch = new Touch();
            TouchedObject = null;
            OnUI = IsPointerOverUIElement();
            OnContextToolbar = IsPointerOverToolbar();
            MouseDownOnObject = false;

            // Get the ray from mouse or touch, touch prefered
            Vector2 rayposition = Vector2.zero;
            
            // First do touch if enabled and touched
            if (EnableToch && Input.touchCount == 1)
            {
                touch = Input.GetTouch(0);
                rayposition  = touch.position;
                touched = true;
            }
            else // otherwise
            {
                rayposition = Input.mousePosition;
                touched = false;
            }
            
            // check if it is a touch select or mouse click
            if (touched)
            {
                if (touch.phase == TouchPhase.Began && !OnUI) isclicked = true;
            }
            else // otherwise the mouse click
            {
                if (Input.GetMouseButtonDown(0) && !OnUI) isclicked = true;
            }
            
            // check if it is a dragging
            if (!isclicked)
            {
                if (touched)
                {
                    if (touch.phase != TouchPhase.Began) dragged = true;
                }
                else
                {
                    if (Input.GetMouseButton(0)) dragged = true;
                }
            }
            
            
            /// Get the object under the ray
            Ray ray = Camera.main.ScreenPointToRay(rayposition);
            if ((Physics.Raycast(ray, out GObject, Mathf.Infinity, layermask) && !OnUI) || inittouch)
            {
                hit = true;
                Hitpoint = GObject.point;
                if (!inittouch)
                    hovered  = GObject.transform.gameObject;
                else
                {
                    hovered = SelectedObject;
                    inittouch = false;
                }
                if (touched) TouchedObject = hovered;
            }
            
            if (isclicked && hovered != null)
            {
                if (hovered.GetComponent<ISelectable>() != null)
                {
                    MouseDownOnObject = true;
                    if (EventMouseDownOnObject != null) EventMouseDownOnObject.Invoke(hovered);
                }
                    
            }

      
            // now call all the events and set the variables
            if (hit)
            {
                if (hovered != HoveredObject && !dragged) // Only when hovered and not dragged with mouse or finger pressed
                {
                    ChangeHovered(HoveredObject, hovered);
                }
                if (isclicked)
                {
                    mousepositiononselect = rayposition;
                    Invoke("CancelLongPress",TimeLongPress+0.1f);
                
                    if (SelectedObject != hovered)
                    {
                        ChangeSelected(SelectedObject, hovered);
                    }
                    else
                    {
                        // Double Clicked, check if time is below double click time
                        if (Time.time - timeselected < TimeDoubleClick)
                        {
                            DoubleSelect = true;
                            if (EventDoubleClicked != null) EventDoubleClicked.Invoke(SelectedObject);
                        }
                    }
                    timeselected = Time.time;
                }
            }
            else // no hit 
            {
                // no hit - no hovered object
                if (HoveredObject != null)
                {
                    ChangeHovered(HoveredObject, null);
                }
                
                // touched but no hit
                if (touched)
                {
                    if (SelectedObject != null)
                    {
                        ChangeSelected(SelectedObject, null);
                    }
                }
                
                // mouse click but no hit
                if (isclicked)
                {
                    if (SelectedObject != null)
                    {
                        ChangeSelected(SelectedObject, null);
                    }
                }
            }
            
            // check if it is long pressed
            if (touched || Input.GetMouseButton(0))
            {
                var distance = Vector2.Distance(mousepositiononselect, rayposition);
                if (distance < 10 && !LongPressed)
                {
                    if (Time.time - timeselected > TimeLongPress)
                    {
                        LongPressed = true;
                        if (EventLongPressed != null) EventLongPressed.Invoke(SelectedObject);
                        if (EnableMultiSelect && !MultiSelectModeIsOn)
                        {
                            MultiSelectModeIsOn = true;
                            if (EventMultiSelect != null) EventMultiSelect.Invoke(true);
                            ChangeSelected(SelectedObject, SelectedObject);
                      
                        }
                        else
                        {
                            if (EnableMultiSelect && MultiSelectModeIsOn)
                            {
                               DeSelectObject(SelectedObject);
                              
                            }
                        }
                    }
                }
            }
            else
            {
                LongPressed = false;
            }
            
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (SelectedObject != null)
                  DeSelectObject(SelectedObject);
            }
            
            inittouch = false;
        }
    }
}