using UnityEngine;
using System.Collections.Generic;

public class VersusLockOnCamera : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform attachTarget;
    [SerializeField] private Transform lookTarget;
    [SerializeField] private List<Transform> targetCandidates = new List<Transform>();
    [SerializeField] private bool handleTargetSwitchInput;
    [SerializeField] private KeyCode switchTargetKey = KeyCode.Tab;

    [Header("Camera Offset")]
    [SerializeField] private Vector3 lockOnOffset = new Vector3(0f, 3f, -8f);
    [SerializeField] private Vector3 noTargetOffset = new Vector3(0f, 3f, -8f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private Vector3 targetLookOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothSpeed = 18f;
    [SerializeField] private float targetChangeDuration = 0.12f;

    [Header("Fallback")]
    [SerializeField] private float noTargetLookDistance = 12f;

    private Vector3 currentLookPosition;
    private Vector3 previousLookPosition;
    private Vector3 positionVelocity;
    private float targetChangeTimer = 999f;
    private int targetIndex;

    public Transform LookTarget => lookTarget;

    private void Start()
    {
        currentLookPosition = GetTargetLookPosition();
        previousLookPosition = currentLookPosition;
    }

    private void LateUpdate()
    {
        if (attachTarget == null)
        {
            return;
        }

        if (handleTargetSwitchInput && Input.GetKeyDown(switchTargetKey))
        {
            SwitchToNextTarget();
        }

        UpdateLookPosition();
        UpdateCameraTransform();
    }

    public void SetAttachTarget(Transform target)
    {
        attachTarget = target;
        ResetLookInterpolation();
    }

    public void ChangeLookTarget(Transform target)
    {
        previousLookPosition = currentLookPosition;
        lookTarget = target;
        targetChangeTimer = 0f;
    }

    public void SwitchToNextTarget()
    {
        RemoveEmptyCandidates();

        if (targetCandidates.Count == 0)
        {
            ChangeLookTarget(null);
            return;
        }

        targetIndex = (targetIndex + 1) % targetCandidates.Count;
        ChangeLookTarget(targetCandidates[targetIndex]);
    }

    private void UpdateLookPosition()
    {
        Vector3 targetPosition = GetTargetLookPosition();

        if (targetChangeTimer < targetChangeDuration)
        {
            targetChangeTimer += Time.deltaTime;
            float rate = Mathf.Clamp01(targetChangeTimer / targetChangeDuration);
            currentLookPosition = Vector3.Lerp(previousLookPosition, targetPosition, SmoothStep(rate));
            return;
        }

        currentLookPosition = targetPosition;
    }

    private void UpdateCameraTransform()
    {
        Vector3 anchorPosition = attachTarget.position + lookOffset;
        Vector3 targetVector = currentLookPosition - anchorPosition;

        if (targetVector.sqrMagnitude < 0.0001f)
        {
            targetVector = attachTarget.forward;
        }

        Quaternion targetBasisRotation = Quaternion.LookRotation(targetVector.normalized, Vector3.up);
        Vector3 offset = lookTarget != null ? lockOnOffset : noTargetOffset;
        Vector3 desiredPosition = anchorPosition + targetBasisRotation * offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        Vector3 lookVector = currentLookPosition - transform.position;

        if (lookVector.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookVector.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    private Vector3 GetTargetLookPosition()
    {
        if (lookTarget != null)
        {
            return lookTarget.position + targetLookOffset;
        }

        if (attachTarget == null)
        {
            return transform.position + transform.forward * noTargetLookDistance;
        }

        return attachTarget.position + lookOffset + attachTarget.forward * noTargetLookDistance;
    }

    private void ResetLookInterpolation()
    {
        currentLookPosition = GetTargetLookPosition();
        previousLookPosition = currentLookPosition;
        targetChangeTimer = targetChangeDuration;
        positionVelocity = Vector3.zero;
    }

    private void RemoveEmptyCandidates()
    {
        for (int i = targetCandidates.Count - 1; i >= 0; i--)
        {
            if (targetCandidates[i] == null)
            {
                targetCandidates.RemoveAt(i);
            }
        }

        if (targetIndex >= targetCandidates.Count)
        {
            targetIndex = 0;
        }
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
