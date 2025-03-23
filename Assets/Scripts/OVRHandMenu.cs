using UnityEngine;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(Canvas))]
public class OVRHandMenu : MonoBehaviour
{
    public enum Handedness { Left, Right, Either }

    [Header("Hand Settings")]
    [SerializeField] Handedness _handedness = Handedness.Either;
    [SerializeField] OVRSkeleton _leftHandSkeleton;
    [SerializeField] OVRSkeleton _rightHandSkeleton;
    [SerializeField] float _palmFacingAngleThreshold = 45f;
    [SerializeField] float _maxPalmToCameraAngle = 60f;

    [Header("Positioning")]
    [SerializeField] Vector3 _positionOffset = new Vector3(0, 0, 0.1f);
    [SerializeField] float _followSmoothness = 10f;
    [SerializeField] float _rotationSmoothness = 5f;

    [Header("Visibility")]
    [SerializeField] float _gazeAngleThreshold = 35f;
    [SerializeField] float _showHideDuration = 0.2f;

    private Transform _menuTransform;
    private Camera _mainCamera;
    private bool _isShowing;
    private Handedness _activeHand;
    private Vector3 _targetScale = Vector3.one;

    private void Awake()
    {
        _menuTransform = transform;
        _mainCamera = Camera.main;
        SetMenuVisibility(false, true);
    }

    private void Update()
    {
        UpdateHandTracking();
        UpdateMenuVisibility();
    }

    private void UpdateHandTracking()
    {
        if (TryGetValidHand(out var handType, out var handPos, out var handRot))
        {
            _activeHand = handType;
            UpdateTargetPosition(handPos, handRot);
        }
    }

    private bool TryGetValidHand(out Handedness handType, out Vector3 position, out Quaternion rotation)
    {
        handType = Handedness.Either;
        position = Vector3.zero;
        rotation = Quaternion.identity;

        bool leftValid = IsHandValid(_leftHandSkeleton, false);
        bool rightValid = IsHandValid(_rightHandSkeleton, true);

        if (_handedness == Handedness.Left && leftValid)
        {
            return GetHandPose(_leftHandSkeleton, false, out position, out rotation);
        }
        if (_handedness == Handedness.Right && rightValid)
        {
            return GetHandPose(_rightHandSkeleton, true, out position, out rotation);
        }
        if (_handedness == Handedness.Either)
        {
            if (rightValid) return GetHandPose(_rightHandSkeleton, true, out position, out rotation);
            if (leftValid) return GetHandPose(_leftHandSkeleton, false, out position, out rotation);
        }
        return false;
    }

    private bool IsHandValid(OVRSkeleton skeleton, bool isRightHand)
    {
        if (skeleton == null || !skeleton.IsDataValid) return false;

        if (TryGetBoneRotation(skeleton, OVRSkeleton.BoneId.Hand_WristRoot, out var wristRot))
        {
            Vector3 palmNormal = isRightHand ? wristRot * Vector3.left : wristRot * Vector3.right;
            Vector3 toCamera = (_mainCamera.transform.position - skeleton.transform.position).normalized;
            float angleToCamera = Vector3.Angle(palmNormal, toCamera);
            return angleToCamera <= _palmFacingAngleThreshold;
        }
        return false;
    }

    private bool GetHandPose(OVRSkeleton skeleton, bool isRightHand, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (TryGetBoneRotation(skeleton, OVRSkeleton.BoneId.Hand_WristRoot, out var wristRot) &&
            TryGetBonePosition(skeleton, OVRSkeleton.BoneId.Hand_WristRoot, out var wristPos))
        {
            Vector3 offset = isRightHand ?
                wristRot * new Vector3(-_positionOffset.x, _positionOffset.y, _positionOffset.z) :
                wristRot * _positionOffset;

            position = wristPos + offset;
            rotation = Quaternion.LookRotation(_mainCamera.transform.position - position, Vector3.up);
            return true;
        }
        return false;
    }

    private void UpdateTargetPosition(Vector3 targetPos, Quaternion targetRot)
    {
        // Position smoothing
        _menuTransform.position = Vector3.Lerp(
            _menuTransform.position,
            targetPos,
            Time.deltaTime * _followSmoothness
        );

        // Rotation smoothing
        _menuTransform.rotation = Quaternion.Slerp(
            _menuTransform.rotation,
            targetRot,
            Time.deltaTime * _rotationSmoothness
        );
    }

    private void UpdateMenuVisibility()
    {
        bool shouldShow = ShouldShowMenu();
        if (_isShowing != shouldShow)
        {
            SetMenuVisibility(shouldShow);
        }
    }

    private bool ShouldShowMenu()
    {
        if (!_mainCamera) return false;

        Vector3 toMenu = (_menuTransform.position - _mainCamera.transform.position).normalized;
        float gazeAngle = Vector3.Angle(_mainCamera.transform.forward, toMenu);
        return gazeAngle <= _gazeAngleThreshold;
    }

    private void SetMenuVisibility(bool show, bool immediate = false)
    {
        _isShowing = show;
        _targetScale = show ? Vector3.one : Vector3.zero;

        if (immediate)
        {
            transform.localScale = _targetScale;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(AnimateScale());
        }
    }

    private System.Collections.IEnumerator AnimateScale()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0;

        while (elapsed < _showHideDuration)
        {
            transform.localScale = Vector3.Lerp(
                startScale,
                _targetScale,
                elapsed / _showHideDuration
            );
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = _targetScale;
    }

    #region Bone Utilities
    private bool TryGetBonePosition(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId, out Vector3 position)
    {
        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == boneId)
            {
                position = bone.Transform.position;
                return true;
            }
        }
        position = Vector3.zero;
        return false;
    }

    private bool TryGetBoneRotation(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId, out Quaternion rotation)
    {
        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == boneId)
            {
                rotation = bone.Transform.rotation;
                return true;
            }
        }
        rotation = Quaternion.identity;
        return false;
    }
    #endregion
}