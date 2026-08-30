using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class SpecialMeleeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private Animator animator;

    [Header("Weapon Data")]
    [SerializeField] private MeleeWeaponDefinition weaponDefinition;

    [Header("Rush")]
    [SerializeField] private float rushSpeed = 24f;
    [SerializeField] private float rushDuration = 0.45f;
    [SerializeField] private float recoveryTime = 0.2f;
    [SerializeField] private float boostCost = 20f;

    [Header("Hit")]
    [SerializeField] private float damage = 100f;
    [SerializeField] private float hitRange = 2.5f;
    [SerializeField] private float hitStunDuration = 0.5f;
    [SerializeField] private float downValue = 50f;
    [SerializeField] private float knockbackSpeed = 6f;
    [SerializeField] private string animationTrigger = "SpecialMelee";

    private Rigidbody rb;
    private Vector3 rushDirection;
    private bool isRushing;
    private bool hasHit;
    private Coroutine rushCoroutine;
    private Transform rushTarget;
    private PlayerMechController targetMovementController;
    private int targetGuidanceCutVersion;

    public bool IsRushing => isRushing;

    public event Action OnRushStarted;
    public event Action<MechHealth> OnRushHit;
    public event Action OnRushEnded;

    public void SetWeaponDefinition(MeleeWeaponDefinition definition)
    {
        weaponDefinition = definition;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted += CancelRush;
        }
    }

    private void Update()
    {
        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.SpecialMelee))
        {
            TryUse();
        }
    }

    private void FixedUpdate()
    {
        if (!isRushing)
        {
            return;
        }

        UpdateRushDirection();
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(
            rushDirection.x * GetRushSpeed(),
            velocity.y,
            rushDirection.z * GetRushSpeed()
        );
        TryHitTarget();
    }

    public bool TryUse()
    {
        if (isRushing
            || movementController == null
            || rushCoroutine != null
            || movementController.IsActionLocked())
        {
            return false;
        }

        HomingProjectile activeProjectile = GetProjectilePrefab();

        if (activeProjectile != null)
        {
            rushCoroutine = StartCoroutine(ThrowRoutine(activeProjectile));
            return true;
        }

        if (!movementController.TryConsumeBoost(GetBoostCost()))
        {
            return false;
        }

        rushCoroutine = StartCoroutine(RushRoutine());
        return true;
    }

    private IEnumerator ThrowRoutine(HomingProjectile projectilePrefab)
    {
        CaptureTargetingState();
        UpdateRushDirection();
        FaceRushDirection();
        movementController.ClearStepInputBuffer();
        movementController.ApplyActionLock(
            GetStartupTime() + GetRecoveryTime(),
            true
        );

        string activeAnimationTrigger = GetAnimationTrigger();

        if (animator != null && !string.IsNullOrWhiteSpace(activeAnimationTrigger))
        {
            animator.SetTrigger(activeAnimationTrigger);
        }

        OnRushStarted?.Invoke();
        yield return new WaitForSeconds(GetStartupTime());

        Vector3 direction = rushTarget != null
            ? (rushTarget.position - transform.position).normalized
            : transform.forward;
        Vector3 spawnPosition = transform.position + Vector3.up * 1.2f + direction * 1.1f;
        HomingProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction, Vector3.up)
        );
        projectile.Launch(rushTarget, direction, transform, rushTarget != null);

        yield return new WaitForSeconds(GetRecoveryTime());
        rushCoroutine = null;
        OnRushEnded?.Invoke();
    }

    private IEnumerator RushRoutine()
    {
        isRushing = true;
        hasHit = false;
        CaptureTargetingState();
        UpdateRushDirection();
        FaceRushDirection();
        movementController.ClearStepInputBuffer();
        movementController.ApplyActionLock(GetRushDuration() + GetRecoveryTime(), true);

        string activeAnimationTrigger = GetAnimationTrigger();

        if (animator != null && !string.IsNullOrWhiteSpace(activeAnimationTrigger))
        {
            animator.SetTrigger(activeAnimationTrigger);
        }

        OnRushStarted?.Invoke();
        yield return new WaitForSeconds(GetRushDuration());

        isRushing = false;
        yield return new WaitForSeconds(GetRecoveryTime());

        rushCoroutine = null;
        OnRushEnded?.Invoke();
    }

    private void UpdateRushDirection()
    {
        bool guidanceWasCut = targetMovementController != null
            && targetMovementController.GuidanceCutVersion != targetGuidanceCutVersion;

        if (guidanceWasCut && rushDirection.sqrMagnitude > 0.01f)
        {
            return;
        }

        Vector3 direction = rushTarget != null
            ? rushTarget.position - transform.position
            : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            rushDirection = direction.normalized;
        }
        else if (rushDirection.sqrMagnitude <= 0.01f)
        {
            rushDirection = transform.forward;
        }
    }

    private void CaptureTargetingState()
    {
        rushTarget = lockOnController != null ? lockOnController.CurrentTarget : null;
        targetMovementController = rushTarget != null
            ? rushTarget.GetComponentInParent<PlayerMechController>()
            : null;
        targetGuidanceCutVersion = targetMovementController != null
            ? targetMovementController.GuidanceCutVersion
            : 0;
    }

    private void FaceRushDirection()
    {
        if (rushDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(rushDirection, Vector3.up);
        }
    }

    private void TryHitTarget()
    {
        if (hasHit)
        {
            return;
        }

        Transform target = rushTarget;

        if (target == null
            || Vector3.Distance(transform.position, target.position) > GetAttackRange())
        {
            return;
        }

        MechHealth health = target.GetComponentInParent<MechHealth>();

        if (health != null && health.TakeDamage(GetDamage()))
        {
            HitReactionController reaction = target.GetComponentInParent<HitReactionController>();
            reaction?.ReceiveHit(
                transform.position,
                GetHitStunDuration(),
                GetDownValue(),
                GetKnockbackSpeed()
            );
            hasHit = true;
            OnRushHit?.Invoke(health);
        }
    }

    private float GetDamage() => weaponDefinition != null ? weaponDefinition.Damage : damage;
    private float GetAttackRange() => weaponDefinition != null
        ? weaponDefinition.AttackRange
        : hitRange;
    private float GetHitStunDuration() => weaponDefinition != null
        ? weaponDefinition.HitStunDuration
        : hitStunDuration;
    private float GetDownValue() => weaponDefinition != null
        ? weaponDefinition.DownValue
        : downValue;
    private float GetKnockbackSpeed() => weaponDefinition != null
        ? weaponDefinition.KnockbackSpeed
        : knockbackSpeed;
    private float GetRushSpeed() => weaponDefinition != null
        ? weaponDefinition.RushSpeed
        : rushSpeed;
    private float GetRushDuration() => weaponDefinition != null
        ? weaponDefinition.RushDuration
        : rushDuration;
    private float GetRecoveryTime() => weaponDefinition != null
        ? weaponDefinition.RecoveryTime
        : recoveryTime;
    private float GetBoostCost() => weaponDefinition != null
        ? weaponDefinition.BoostCost
        : boostCost;
    private float GetStartupTime() => weaponDefinition != null
        ? weaponDefinition.StartupTime
        : 0.15f;
    private HomingProjectile GetProjectilePrefab()
    {
        if (weaponDefinition != null && weaponDefinition.ProjectilePrefab != null)
        {
            return weaponDefinition.ProjectilePrefab;
        }

        return playerShooter != null ? playerShooter.ActiveProjectilePrefab : null;
    }
    private string GetAnimationTrigger() => weaponDefinition != null
        ? weaponDefinition.SpecialAnimationTrigger
        : animationTrigger;

    private void CancelRush()
    {
        if (rushCoroutine == null)
        {
            return;
        }

        StopCoroutine(rushCoroutine);
        rushCoroutine = null;
        isRushing = false;
        OnRushEnded?.Invoke();
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted -= CancelRush;
        }

        CancelRush();
    }

    private void OnValidate()
    {
        rushSpeed = Mathf.Max(0f, rushSpeed);
        rushDuration = Mathf.Max(0.01f, rushDuration);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        boostCost = Mathf.Max(0f, boostCost);
        damage = Mathf.Max(0f, damage);
        hitRange = Mathf.Max(0.1f, hitRange);
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
        downValue = Mathf.Max(0f, downValue);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
    }
}
