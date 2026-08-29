using System;
using System.Collections;
using UnityEngine;

public class AwakeningBurstAttackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AwakeningController awakeningController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private Transform firePoint;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private Animator animator;

    [Header("Weapon Data")]
    [SerializeField] private RangedWeaponDefinition weaponDefinition;

    [Header("Burst Attack")]
    [SerializeField] private float startupTime = 0.75f;
    [SerializeField] private float totalActionLock = 1.5f;
    [SerializeField] private string animationTrigger = "BurstAttack";

    private bool usedThisAwakening;
    private Coroutine attackCoroutine;
    private Transform preparedTarget;
    private PlayerMechController targetMovementController;
    private int targetGuidanceCutVersion;

    public bool CanUse => awakeningController != null
        && awakeningController.IsAwakened
        && !usedThisAwakening
        && attackCoroutine == null;

    public event Action OnBurstAttackStarted;
    public event Action OnBurstAttackFired;
    public event Action OnBurstAttackEnded;

    public void SetWeaponDefinition(RangedWeaponDefinition definition)
    {
        weaponDefinition = definition;
    }

    private void Awake()
    {
        if (awakeningController == null)
        {
            awakeningController = GetComponent<AwakeningController>();
        }

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void OnEnable()
    {
        if (awakeningController != null)
        {
            awakeningController.OnAwakeningStarted += ResetUsage;
            awakeningController.OnAwakeningEnded += CancelAttack;
        }
    }

    private void Update()
    {
        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.BurstAttack))
        {
            TryUse();
        }
    }

    public bool TryUse()
    {
        if (!CanUse
            || GetProjectilePrefab() == null
            || (movementController != null && movementController.IsActionLocked()))
        {
            return false;
        }

        usedThisAwakening = true;
        attackCoroutine = StartCoroutine(AttackRoutine());
        return true;
    }

    private IEnumerator AttackRoutine()
    {
        movementController?.ClearStepInputBuffer();
        movementController?.ApplyActionLock(GetActionLockDuration(), false);
        FaceTarget();
        CaptureTargetingState();

        if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        OnBurstAttackStarted?.Invoke();

        float activeStartupTime = GetStartupTime();

        if (activeStartupTime > 0f)
        {
            yield return new WaitForSeconds(activeStartupTime);
        }

        FireProjectile();
        OnBurstAttackFired?.Invoke();

        float remainingLock = Mathf.Max(
            0f,
            GetActionLockDuration() - activeStartupTime
        );

        if (remainingLock > 0f)
        {
            yield return new WaitForSeconds(remainingLock);
        }

        attackCoroutine = null;
        OnBurstAttackEnded?.Invoke();
    }

    private void FireProjectile()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;
        bool guidanceWasCut = targetMovementController != null
            && targetMovementController.GuidanceCutVersion != targetGuidanceCutVersion;
        Vector3 direction = !guidanceWasCut && preparedTarget != null
            ? preparedTarget.position - spawnPoint.position
            : spawnPoint.forward;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = spawnPoint.forward;
        }

        direction.Normalize();
        HomingProjectile projectile = Instantiate(
            GetProjectilePrefab(),
            spawnPoint.position,
            Quaternion.LookRotation(direction, Vector3.up)
        );
        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(
            preparedTarget,
            direction,
            transform,
            enableHoming && !guidanceWasCut
        );
    }

    private HomingProjectile GetProjectilePrefab()
    {
        return weaponDefinition != null && weaponDefinition.ProjectilePrefab != null
            ? weaponDefinition.ProjectilePrefab
            : projectilePrefab;
    }

    private float GetStartupTime()
    {
        return weaponDefinition != null ? weaponDefinition.StartupTime : startupTime;
    }

    private float GetActionLockDuration()
    {
        return weaponDefinition != null
            ? weaponDefinition.ActionLockDuration
            : totalActionLock;
    }

    private void CaptureTargetingState()
    {
        preparedTarget = lockOnController != null ? lockOnController.CurrentTarget : null;
        targetMovementController = preparedTarget != null
            ? preparedTarget.GetComponentInParent<PlayerMechController>()
            : null;
        targetGuidanceCutVersion = targetMovementController != null
            ? targetMovementController.GuidanceCutVersion
            : 0;
    }

    private void FaceTarget()
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ResetUsage()
    {
        usedThisAwakening = false;
    }

    private void CancelAttack()
    {
        if (attackCoroutine == null)
        {
            return;
        }

        StopCoroutine(attackCoroutine);
        attackCoroutine = null;
        OnBurstAttackEnded?.Invoke();
    }

    private void OnDisable()
    {
        if (awakeningController != null)
        {
            awakeningController.OnAwakeningStarted -= ResetUsage;
            awakeningController.OnAwakeningEnded -= CancelAttack;
        }

        CancelAttack();
    }

    private void OnValidate()
    {
        startupTime = Mathf.Max(0f, startupTime);
        totalActionLock = Mathf.Max(startupTime, totalActionLock);
    }
}
