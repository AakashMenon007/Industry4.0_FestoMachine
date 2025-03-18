using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class AnchorUISpawner : MonoBehaviour
{
    [System.Serializable]
    public class UIConfiguration
    {
        public MRUKAnchor.SceneLabels anchorLabel;
        public GameObject uiPanelPrefab;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotationOffset = Vector3.zero;
        public bool faceCamera = true;
    }

    [SerializeField] private List<UIConfiguration> uiConfigurations = new();
    [SerializeField] private Transform uiParent;
    [SerializeField] private float uiScale = 0.1f;

    private Dictionary<MRUKAnchor.SceneLabels, UIConfiguration> configLookup;
    private Transform mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main.transform;
        InitializeLookup();
        RegisterMRUKCallbacks();
    }

    private void InitializeLookup()
    {
        configLookup = new Dictionary<MRUKAnchor.SceneLabels, UIConfiguration>();
        foreach (var config in uiConfigurations)
        {
            if (!configLookup.ContainsKey(config.anchorLabel))
            {
                configLookup.Add(config.anchorLabel, config);
            }
        }
    }

    private void RegisterMRUKCallbacks()
    {
        if (MRUK.Instance)
        {
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }
    }

    private void OnSceneLoaded()
    {
        var currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom)
        {
            currentRoom.AnchorCreatedEvent.AddListener(SpawnUIForAnchor);
            ProcessExistingAnchors(currentRoom);
        }
    }

    private void ProcessExistingAnchors(MRUKRoom room)
    {
        foreach (var anchor in room.Anchors)
        {
            SpawnUIForAnchor(anchor);
        }
    }

    private void SpawnUIForAnchor(MRUKAnchor anchor)
    {
        if (!configLookup.TryGetValue(anchor.Label, out var config)) return;

        var uiInstance = Instantiate(config.uiPanelPrefab,
            anchor.transform.position + config.positionOffset,
            Quaternion.Euler(config.rotationOffset),
            uiParent ? uiParent : transform);

        uiInstance.transform.localScale = Vector3.one * uiScale;

        if (config.faceCamera)
        {
            FaceCamera(uiInstance.transform);
        }

        AttachToAnchor(uiInstance.transform, anchor.transform);
    }

    private void FaceCamera(Transform uiTransform)
    {
        var lookPos = mainCamera.position - uiTransform.position;
        lookPos.y = 0;
        uiTransform.rotation = Quaternion.LookRotation(lookPos);
    }

    private void AttachToAnchor(Transform uiTransform, Transform anchorTransform)
    {
        uiTransform.SetParent(anchorTransform);
        uiTransform.localPosition += uiTransform.parent.InverseTransformVector(uiTransform.localPosition);
    }

    private void OnDestroy()
    {
        if (MRUK.Instance && MRUK.Instance.GetCurrentRoom())
        {
            MRUK.Instance.GetCurrentRoom().AnchorCreatedEvent.RemoveListener(SpawnUIForAnchor);
        }
    }
}