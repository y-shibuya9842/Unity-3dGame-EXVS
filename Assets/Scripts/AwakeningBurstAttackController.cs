using System;
using System.Collections;
using UnityEngine;

public class AwakeningBurstAttackController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode burstAttackKey = KeyCode.T;

    [Header("References")]
    [SerializeField] private AwakeningController awakeningController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private Transform firePoint;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private Animator animator;

    [Header("Burst Attack")]
    [SerializeField] private float startupTime = 0.75f;
    [SerializeField] private float totalActionLock = 1.5f;
    [SerializeField] private string animationTrigger = "BurstAttack";

    private bool usedThisAwakening;
    private Coroutine attackCoroutine;

    public bool CanUse => awakeningController != null
        && awakeningController.IsAwakened
        && !usedThisAwakening
        && attackCoroutine == null;

    public event Action OnBurstAttackStarted;
    public event Action OnBurstAttackFired;
    public event Action OnBurstAttackEnded;

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
        if (Input.GetKeyDown(burstAttackKey))
        {
            TryUse();
        }
    }

    public bool TryUse()
    {
        if (!CanUse
            || projectilePrefab == null
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
        movementController?.ApplyActionLock(totalActionLock, false);
        FaceTarget();

        if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        OnBurstAttackStarted?.Invoke();

        if (startupTime > 0f)
        {
            yield return new WaitForSeconds(startupTime);
        }

        FireProjectile();
        OnBurstAttackFired?.Invoke();

        float remainingLock = Mathf.Max(0f, totalActionLock - startupTime);

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
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;
        Vector3 direction = target != null
            ? target.position - spawnPoint.position
            : spawnPoint.forward;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = spawnPoint.forward;
        }

        direction.Normalize();
        HomingProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(direction, Vector3.up)
        );
        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(target, direction, transform, enableHoming);
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
