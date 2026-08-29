using System;
using UnityEngine;

public class TransformationController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode transformationKey = KeyCode.X;

    [Header("Visuals")]
    [SerializeField] private GameObject normalModel;
    [SerializeField] private GameObject transformedModel;

    [Header("Transformation")]
    [SerializeField] private float movementSpeedMultiplier = 1.35f;
    [SerializeField] private float boostCost = 10f;
    [SerializeField] private float actionLockDuration = 0.25f;
    [SerializeField] private float toggleCooldown = 0.5f;

    [Header("References")]
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private Animator animator;
    [SerializeField] private string transformedAnimationBool = "Transformed";

    private bool isTransformed;
    private float cooldownTimer;

    public bool IsTransformed => isTransformed;

    public event Action<bool> OnTransformationChanged;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        ApplyTransformationState(false, false);
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(transformationKey))
        {
            TryToggleTransformation();
        }
    }

    public bool TryToggleTransformation()
    {
        if (cooldownTimer > 0f
            || movementController == null
            || movementController.IsActionLocked())
        {
            return false;
        }

        if (!isTransformed && !movementController.TryConsumeBoost(boostCost))
        {
            return false;
        }

        ApplyTransformationState(!isTransformed, true);
        movementController.ClearStepInputBuffer();
        movementController.ApplyActionLock(actionLockDuration, true);
        cooldownTimer = toggleCooldown;
        return true;
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

        movementController?.SetMovementSpeedMultiplier(
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
            ApplyTransformationState(false, false);
        }
    }

    private void OnValidate()
    {
        movementSpeedMultiplier = Mathf.Max(0.01f, movementSpeedMultiplier);
        boostCost = Mathf.Max(0f, boostCost);
        actionLockDuration = Mathf.Max(0f, actionLockDuration);
        toggleCooldown = Mathf.Max(0f, toggleCooldown);
    }
}
