using System;
using UnityEngine;

public class ShieldGuard : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private float leverInputWindow = 0.3f;
    [SerializeField] private bool playerInputEnabled = true;

    [Header("Guard")]
    [SerializeField, Range(1f, 180f)] private float guardAngle = 110f;
    [SerializeField] private float boostDrainPerSecond = 20f;
    [SerializeField] private float movementLockRefreshTime = 0.05f;

    [Header("References")]
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private GameObject guardVisual;

    private bool isGuarding;
    private bool downInputArmed;
    private float downInputTime;
    private Vector2 previousMoveInput;
    private float automaticGuardTimer;

    public bool IsGuarding => isGuarding;

    public event Action OnGuardStarted;
    public event Action OnGuardEnded;
    public event Action OnAttackBlocked;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        SetGuardVisual(false);
    }

    private void Update()
    {
        if (automaticGuardTimer > 0f)
        {
            automaticGuardTimer -= Time.deltaTime;

            if (automaticGuardTimer <= 0f)
            {
                StopGuard();
            }

            return;
        }

        if (!playerInputEnabled)
        {
            return;
        }

        UpdateLeverInput();

        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Guard))
        {
            StartGuard();
        }

        if (!isGuarding)
        {
            return;
        }

        bool keepGuarding = VersusInputManager.Instance.IsPressed(VersusInputAction.Guard)
            || VersusInputManager.Instance.ReadMove().y > 0.5f;

        if (!keepGuarding || movementController == null)
        {
            StopGuard();
            return;
        }

        movementController.ApplyActionLock(movementLockRefreshTime, false);

        if (!movementController.TryConsumeBoost(boostDrainPerSecond * Time.deltaTime))
        {
            StopGuard();
        }
    }

    public bool TryBlock(Vector3 attackOrigin)
    {
        if (!isGuarding)
        {
            return false;
        }

        Vector3 directionToAttack = attackOrigin - transform.position;

        if (directionToAttack.sqrMagnitude <= 0.01f)
        {
            return true;
        }

        float angle = Vector3.Angle(transform.forward, directionToAttack.normalized);

        if (angle > guardAngle * 0.5f)
        {
            return false;
        }

        OnAttackBlocked?.Invoke();
        return true;
    }

    public void SetPlayerInputEnabled(bool enabled)
    {
        playerInputEnabled = enabled;
    }

    public void StartAutomaticGuard(float duration)
    {
        automaticGuardTimer = Mathf.Max(0.1f, duration);

        if (!isGuarding)
        {
            isGuarding = true;
            SetGuardVisual(true);
            OnGuardStarted?.Invoke();
        }
    }

    private void UpdateLeverInput()
    {
        Vector2 moveInput = VersusInputManager.Instance.ReadMove();
        bool pressedDown = moveInput.y < -0.5f && previousMoveInput.y >= -0.5f;
        bool pressedForward = moveInput.y > 0.5f && previousMoveInput.y <= 0.5f;
        previousMoveInput = moveInput;

        if (pressedDown)
        {
            downInputArmed = true;
            downInputTime = Time.time;
        }

        if (downInputArmed && Time.time - downInputTime > leverInputWindow)
        {
            downInputArmed = false;
        }

        if (downInputArmed && pressedForward)
        {
            downInputArmed = false;
            StartGuard();
        }
    }

    private void StartGuard()
    {
        if (isGuarding || movementController == null || movementController.CurrentBoost <= 0f)
        {
            return;
        }

        isGuarding = true;
        movementController.ClearStepInputBuffer();
        SetGuardVisual(true);
        OnGuardStarted?.Invoke();
    }

    private void StopGuard()
    {
        if (!isGuarding)
        {
            return;
        }

        isGuarding = false;
        SetGuardVisual(false);
        OnGuardEnded?.Invoke();
    }

    private void SetGuardVisual(bool visible)
    {
        if (guardVisual != null)
        {
            guardVisual.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        automaticGuardTimer = 0f;
        isGuarding = false;
        SetGuardVisual(false);
    }

    private void OnValidate()
    {
        leverInputWindow = Mathf.Max(0.01f, leverInputWindow);
        boostDrainPerSecond = Mathf.Max(0f, boostDrainPerSecond);
        movementLockRefreshTime = Mathf.Max(0.01f, movementLockRefreshTime);
    }
}
