using System;
using UnityEngine;

public class ShieldGuard : MonoBehaviour
{
    [Header("入力")]
    [SerializeField, InspectorName("レバー入力受付時間")]
    private float leverInputWindow = 0.3f;
    [SerializeField, InspectorName("プレイヤー入力を使用")]
    private bool playerInputEnabled = true;

    [Header("シールド")]
    [SerializeField, Range(1f, 180f), InspectorName("防御角度")]
    private float guardAngle = 110f;
    [SerializeField, InspectorName("基本持続時間")]
    private float baseGuardDuration = 0.45f;
    [SerializeField, InspectorName("延長中のブースト消費量（毎秒）")]
    private float boostDrainPerSecond = 20f;
    [SerializeField, InspectorName("移動停止の更新間隔")]
    private float movementLockRefreshTime = 0.05f;

    [Header("参照")]
    [SerializeField, InspectorName("移動制御")]
    private PlayerMechController movementController;
    [SerializeField, InspectorName("シールド表示")]
    private GameObject guardVisual;

    private bool isGuarding;
    private bool downInputArmed;
    private float downInputTime;
    private Vector2 previousMoveInput;
    private float automaticGuardTimer;
    private float manualGuardTimer;

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

        if (!isGuarding)
        {
            return;
        }

        manualGuardTimer -= Time.deltaTime;
        bool baseGuardActive = manualGuardTimer > 0f;
        bool extendingGuard = VersusInputManager.Instance.ReadMove().y > 0.5f;

        if ((!baseGuardActive && !extendingGuard) || movementController == null)
        {
            StopGuard();
            return;
        }

        movementController.ApplyActionLock(movementLockRefreshTime, false);

        if (!baseGuardActive
            && !movementController.TryConsumeBoost(boostDrainPerSecond * Time.deltaTime))
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
        manualGuardTimer = Mathf.Max(0.01f, baseGuardDuration);
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
        manualGuardTimer = 0f;
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
        manualGuardTimer = 0f;
        isGuarding = false;
        SetGuardVisual(false);
    }

    private void OnValidate()
    {
        leverInputWindow = Mathf.Max(0.01f, leverInputWindow);
        baseGuardDuration = Mathf.Max(0.01f, baseGuardDuration);
        boostDrainPerSecond = Mathf.Max(0f, boostDrainPerSecond);
        movementLockRefreshTime = Mathf.Max(0.01f, movementLockRefreshTime);
    }
}
