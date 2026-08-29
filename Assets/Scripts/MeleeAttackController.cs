using System;
using System.Collections;
using UnityEngine;

public class MeleeAttackController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode meleeKey = KeyCode.F;

    [Header("References")]
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private Animator animator;

    [Header("Attack")]
    [SerializeField] private float damage = 120f;
    [SerializeField] private float attackRange = 4f;
    [SerializeField, Range(1f, 180f)] private float hitAngle = 70f;
    [SerializeField] private float startupTime = 0.15f;
    [SerializeField] private float recoveryTime = 0.35f;
    [SerializeField] private string animationTrigger = "Melee";

    private bool isAttacking;

    public bool IsAttacking => isAttacking;

    public event Action OnAttackStarted;
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
        if (Input.GetKeyDown(meleeKey) && CanAttack())
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
        movementController?.ApplyActionLock(startupTime + recoveryTime, true);
        FaceTarget();

        if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        OnAttackStarted?.Invoke();
        yield return new WaitForSeconds(startupTime);

        TryHitTarget();
        yield return new WaitForSeconds(recoveryTime);

        isAttacking = false;
        OnAttackEnded?.Invoke();
    }

    private void TryHitTarget()
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return;
        }

        Vector3 directionToTarget = target.position - transform.position;

        if (directionToTarget.magnitude > attackRange)
        {
            return;
        }

        float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);

        if (angle > hitAngle * 0.5f)
        {
            return;
        }

        MechHealth health = target.GetComponentInParent<MechHealth>();

        if (health != null && health.TakeDamage(damage))
        {
            OnAttackHit?.Invoke(health);
        }
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

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted -= CancelAttack;
        }

        StopAllCoroutines();
        isAttacking = false;
    }

    private void CancelAttack()
    {
        if (!isAttacking)
        {
            return;
        }

        StopAllCoroutines();
        isAttacking = false;
        OnAttackEnded?.Invoke();
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        attackRange = Mathf.Max(0.1f, attackRange);
        startupTime = Mathf.Max(0f, startupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
    }
}
