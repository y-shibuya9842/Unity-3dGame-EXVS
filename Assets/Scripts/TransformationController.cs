using System;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class TransformationController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject normalModel;
    [SerializeField] private GameObject transformedModel;

    [Header("Transformation")]
    [SerializeField] private float movementSpeedMultiplier = 1.35f;
    [SerializeField] private float transformationStartBoostCost = 10f;
    [SerializeField] private float transformationBoostDrainPerSecond = 18f;

    [Header("Homing Dash")]
    [SerializeField] private float homingDashSpeed = 30f;
    [SerializeField] private float homingDashDuration = 0.3f;
    [SerializeField] private float homingDashBoostCost = 12f;

    [Header("Vertical Move")]
    [SerializeField] private float ascendSpeed = 6f;
    [SerializeField] private float descendSpeed = 6f;
    [SerializeField] private float jumpDoubleTapWindow = 0.3f;

    [Header("References")]
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private Animator animator;
    [SerializeField] private string transformedAnimationBool = "Transformed";

    private bool isTransformed;
    private bool isHomingDashing;
    private float homingDashTimer;
    private float lastJumpTapTime = -999f;
    private Vector3 homingDashDirection;
    private Rigidbody rb;

    public bool IsTransformed => isTransformed;

    public event Action<bool> OnTransformationChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        ApplyTransformationState(false, false);
    }

    private void Update()
    {
        if (!isTransformed)
        {
            return;
        }

        if (!HasMoveInput())
        {
            EndTransformation();
            return;
        }

        if (!movementController.TryConsumeBoost(
                transformationBoostDrainPerSecond * Time.deltaTime
            ))
        {
            EndTransformation();
            return;
        }

        HandleVerticalMoveInput();
    }

    private void FixedUpdate()
    {
        if (!isHomingDashing)
        {
            return;
        }

        homingDashTimer -= Time.fixedDeltaTime;

        if (homingDashTimer <= 0f || !isTransformed)
        {
            isHomingDashing = false;
            return;
        }

        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(
            homingDashDirection.x * homingDashSpeed,
            velocity.y,
            homingDashDirection.z * homingDashSpeed
        );
    }

    public bool TryHandleStepInput(
        Vector2 inputDirection,
        Vector3 worldDirection,
        bool jumpButtonHeld)
    {
        if (isTransformed)
        {
            if (inputDirection == Vector2.up)
            {
                TryStartHomingDash();
            }

            // 変形中は通常ステップを行わず、変形動作にも誘導切りを付けない。
            return true;
        }

        if (!jumpButtonHeld
            || movementController == null
            || movementController.IsActionLocked()
            || !movementController.TryConsumeBoost(transformationStartBoostCost))
        {
            return false;
        }

        movementController.PrepareForTransformation();

        if (worldDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        }

        ApplyTransformationState(true, true);
        return true;
    }

    private void TryStartHomingDash()
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null
            || movementController == null
            || !movementController.TryConsumeBoost(homingDashBoostCost))
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        homingDashDirection = direction.normalized;
        transform.rotation = Quaternion.LookRotation(homingDashDirection, Vector3.up);
        homingDashTimer = homingDashDuration;
        isHomingDashing = true;
    }

    private void HandleVerticalMoveInput()
    {
        if (!VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Jump))
        {
            return;
        }

        bool isDoubleTap = Time.time - lastJumpTapTime <= jumpDoubleTapWindow;
        Vector3 velocity = rb.linearVelocity;
        velocity.y = isDoubleTap ? -descendSpeed : ascendSpeed;
        rb.linearVelocity = velocity;
        lastJumpTapTime = isDoubleTap ? -999f : Time.time;
    }

    private static bool HasMoveInput()
    {
        return VersusInputManager.Instance.ReadMove().sqrMagnitude > 0.04f;
    }

    public void EndTransformation()
    {
        if (!isTransformed)
        {
            return;
        }

        isHomingDashing = false;
        ApplyTransformationState(false, true);
    }

    private void ApplyTransformationState(bool transformed, bool notify)
    {
        isTransformed = transformed;

        if (normalModel != null)
        {
            normalModel.SetActive(!transformed);
        }

        if (transformedModel != null)
        {
            transformedModel.SetActive(transformed);
        }

        movementController?.SetTransformationSpeedMultiplier(
            transformed ? movementSpeedMultiplier : 1f
        );

        if (animator != null && !string.IsNullOrWhiteSpace(transformedAnimationBool))
        {
            animator.SetBool(transformedAnimationBool, transformed);
        }

        if (notify)
        {
            OnTransformationChanged?.Invoke(transformed);
        }
    }

    private void OnDisable()
    {
        if (isTransformed)
        {
            isHomingDashing = false;
            ApplyTransformationState(false, false);
        }
    }

    private void OnValidate()
    {
        movementSpeedMultiplier = Mathf.Max(0.01f, movementSpeedMultiplier);
        transformationStartBoostCost = Mathf.Max(0f, transformationStartBoostCost);
        transformationBoostDrainPerSecond = Mathf.Max(0f, transformationBoostDrainPerSecond);
        homingDashSpeed = Mathf.Max(0f, homingDashSpeed);
        homingDashDuration = Mathf.Max(0.01f, homingDashDuration);
        homingDashBoostCost = Mathf.Max(0f, homingDashBoostCost);
        ascendSpeed = Mathf.Max(0f, ascendSpeed);
        descendSpeed = Mathf.Max(0f, descendSpeed);
        jumpDoubleTapWindow = Mathf.Max(0.01f, jumpDoubleTapWindow);
    }
}
