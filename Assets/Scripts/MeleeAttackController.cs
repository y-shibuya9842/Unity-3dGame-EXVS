using System;
using System.Collections;
using UnityEngine;

public class MeleeAttackController : MonoBehaviour
{
    private enum MeleeDirection
    {
        Neutral,
        Forward,
        Backward,
        Side
    }

    [Header("Input")]
    [SerializeField] private KeyCode meleeKey = KeyCode.F;

    [Header("References")]
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private Animator animator;

    [Header("Weapon Data")]
    [SerializeField] private MeleeWeaponDefinition weaponDefinition;

    [Header("Attack")]
    [SerializeField] private float damage = 120f;
    [SerializeField] private float attackRange = 4f;
    [SerializeField, Range(1f, 180f)] private float hitAngle = 70f;
    [SerializeField] private float startupTime = 0.15f;
    [SerializeField] private float recoveryTime = 0.35f;
    [SerializeField] private float hitStunDuration = 0.4f;
    [SerializeField] private float downValue = 35f;
    [SerializeField] private float knockbackSpeed = 4f;
    [SerializeField] private string animationTrigger = "Melee";

    [Header("Directional Attack")]
    [SerializeField] private float forwardDamageMultiplier = 1.1f;
    [SerializeField] private float backwardDamageMultiplier = 1.2f;
    [SerializeField] private float sideDamageMultiplier = 0.9f;
    [SerializeField] private float forwardRangeMultiplier = 1.25f;
    [SerializeField] private string forwardAnimationTrigger = "ForwardMelee";
    [SerializeField] private string backwardAnimationTrigger = "BackwardMelee";
    [SerializeField] private string sideAnimationTrigger = "SideMelee";

    [Header("Combo")]
    [SerializeField] private int maxComboCount = 3;
    [SerializeField] private float comboInputWindow = 0.35f;
    [SerializeField] private float comboDamageMultiplier = 0.85f;
    [SerializeField] private float comboDownValueMultiplier = 1.15f;
    [SerializeField] private string comboStageParameter = "MeleeCombo";

    private bool isAttacking;
    private bool canBufferCombo;
    private bool comboBuffered;
    private MeleeDirection activeDirection;
    private float externalDamageMultiplier = 1f;

    public bool IsAttacking => isAttacking;

    public event Action OnAttackStarted;
    public event Action<int> OnComboStageStarted;
    public event Action<MechHealth> OnAttackHit;
    public event Action OnAttackEnded;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted += CancelAttack;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(meleeKey))
        {
            return;
        }

        if (isAttacking)
        {
            if (canBufferCombo)
            {
                comboBuffered = true;
            }

            return;
        }

        if (CanAttack())
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private bool CanAttack()
    {
        return !isAttacking
            && (movementController == null || !movementController.IsActionLocked());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        activeDirection = ReadMeleeDirection();
        FaceTarget();
        OnAttackStarted?.Invoke();

        for (int comboIndex = 0; comboIndex < GetMaxComboCount(); comboIndex++)
        {
            comboBuffered = false;
            canBufferCombo = false;
            movementController?.ApplyActionLock(
                GetStartupTime() + GetComboInputWindow() + GetRecoveryTime(),
                true
            );
            PlayAttackAnimation(comboIndex);
            OnComboStageStarted?.Invoke(comboIndex + 1);

            yield return new WaitForSeconds(GetStartupTime());
            TryHitTarget(comboIndex);

            canBufferCombo = comboIndex < GetMaxComboCount() - 1;
            yield return new WaitForSeconds(GetComboInputWindow());
            canBufferCombo = false;

            if (!comboBuffered)
            {
                yield return new WaitForSeconds(GetRecoveryTime());
                break;
            }
        }

        isAttacking = false;
        comboBuffered = false;
        OnAttackEnded?.Invoke();
    }

    private void TryHitTarget(int comboIndex)
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return;
        }

        Vector3 directionToTarget = target.position - transform.position;

        float rangeMultiplier = activeDirection == MeleeDirection.Forward
            ? forwardRangeMultiplier
            : 1f;

        if (directionToTarget.magnitude > GetAttackRange() * rangeMultiplier)
        {
            return;
        }

        float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);

        if (angle > GetHitAngle() * 0.5f)
        {
            return;
        }

        MechHealth health = target.GetComponentInParent<MechHealth>();

        float comboDamage = GetDamage()
            * GetDirectionalDamageMultiplier()
            * Mathf.Pow(GetComboDamageMultiplier(), comboIndex)
            * externalDamageMultiplier;

        if (health != null && health.TakeDamage(comboDamage))
        {
            HitReactionController reaction = target.GetComponentInParent<HitReactionController>();
            float comboDownValue = GetDownValue()
                * Mathf.Pow(GetComboDownValueMultiplier(), comboIndex);
            reaction?.ReceiveHit(
                transform.position,
                GetHitStunDuration(),
                comboDownValue,
                GetKnockbackSpeed()
            );
            OnAttackHit?.Invoke(health);
        }
    }

    private MeleeDirection ReadMeleeDirection()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            return MeleeDirection.Forward;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            return MeleeDirection.Backward;
        }

        if (Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.LeftArrow)
            || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.RightArrow))
        {
            return MeleeDirection.Side;
        }

        return MeleeDirection.Neutral;
    }

    private float GetDirectionalDamageMultiplier()
    {
        switch (activeDirection)
        {
            case MeleeDirection.Forward:
                return forwardDamageMultiplier;
            case MeleeDirection.Backward:
                return backwardDamageMultiplier;
            case MeleeDirection.Side:
                return sideDamageMultiplier;
            default:
                return 1f;
        }
    }

    private void PlayAttackAnimation(int comboIndex)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(comboStageParameter))
        {
            animator.SetInteger(comboStageParameter, comboIndex + 1);
        }

        string trigger = GetDirectionalAnimationTrigger();

        if (!string.IsNullOrWhiteSpace(trigger))
        {
            animator.SetTrigger(trigger);
        }
    }

    private string GetDirectionalAnimationTrigger()
    {
        switch (activeDirection)
        {
            case MeleeDirection.Forward:
                return weaponDefinition != null
                    ? weaponDefinition.ForwardAnimationTrigger
                    : forwardAnimationTrigger;
            case MeleeDirection.Backward:
                return weaponDefinition != null
                    ? weaponDefinition.BackwardAnimationTrigger
                    : backwardAnimationTrigger;
            case MeleeDirection.Side:
                return weaponDefinition != null
                    ? weaponDefinition.SideAnimationTrigger
                    : sideAnimationTrigger;
            default:
                return weaponDefinition != null
                    ? weaponDefinition.NeutralAnimationTrigger
                    : animationTrigger;
        }
    }

    private float GetDamage() => weaponDefinition != null ? weaponDefinition.Damage : damage;
    private float GetAttackRange() => weaponDefinition != null
        ? weaponDefinition.AttackRange
        : attackRange;
    private float GetHitAngle() => weaponDefinition != null ? weaponDefinition.HitAngle : hitAngle;
    private float GetStartupTime() => weaponDefinition != null
        ? weaponDefinition.StartupTime
        : startupTime;
    private float GetRecoveryTime() => weaponDefinition != null
        ? weaponDefinition.RecoveryTime
        : recoveryTime;
    private float GetHitStunDuration() => weaponDefinition != null
        ? weaponDefinition.HitStunDuration
        : hitStunDuration;
    private float GetDownValue() => weaponDefinition != null
        ? weaponDefinition.DownValue
        : downValue;
    private float GetKnockbackSpeed() => weaponDefinition != null
        ? weaponDefinition.KnockbackSpeed
        : knockbackSpeed;
    private int GetMaxComboCount() => weaponDefinition != null
        ? weaponDefinition.MaxComboCount
        : maxComboCount;
    private float GetComboInputWindow() => weaponDefinition != null
        ? weaponDefinition.ComboInputWindow
        : comboInputWindow;
    private float GetComboDamageMultiplier() => weaponDefinition != null
        ? weaponDefinition.ComboDamageMultiplier
        : comboDamageMultiplier;
    private float GetComboDownValueMultiplier() => weaponDefinition != null
        ? weaponDefinition.ComboDownValueMultiplier
        : comboDownValueMultiplier;

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

    public void SetDamageMultiplier(float multiplier)
    {
        externalDamageMultiplier = Mathf.Max(0.01f, multiplier);
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted -= CancelAttack;
        }

        StopAllCoroutines();
        isAttacking = false;
        canBufferCombo = false;
        comboBuffered = false;
    }

    private void CancelAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        StopAllCoroutines();
        isAttacking = false;
        canBufferCombo = false;
        comboBuffered = false;
        OnAttackEnded?.Invoke();
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        attackRange = Mathf.Max(0.1f, attackRange);
        startupTime = Mathf.Max(0f, startupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
        downValue = Mathf.Max(0f, downValue);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
        forwardDamageMultiplier = Mathf.Max(0f, forwardDamageMultiplier);
        backwardDamageMultiplier = Mathf.Max(0f, backwardDamageMultiplier);
        sideDamageMultiplier = Mathf.Max(0f, sideDamageMultiplier);
        forwardRangeMultiplier = Mathf.Max(0.01f, forwardRangeMultiplier);
        maxComboCount = Mathf.Max(1, maxComboCount);
        comboInputWindow = Mathf.Max(0.01f, comboInputWindow);
        comboDamageMultiplier = Mathf.Max(0f, comboDamageMultiplier);
        comboDownValueMultiplier = Mathf.Max(0f, comboDownValueMultiplier);
    }
}
