using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody))]
public class SpecialMeleeController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode specialMeleeKey = KeyCode.V;

    [Header("References")]
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private Animator animator;

    [Header("Rush")]
    [SerializeField] private float rushSpeed = 24f;
    [SerializeField] private float rushDuration = 0.45f;
    [SerializeField] private float recoveryTime = 0.2f;
    [SerializeField] private float boostCost = 20f;

    [Header("Hit")]
    [SerializeField] private float damage = 100f;
    [SerializeField] private float hitRange = 2.5f;
    [SerializeField] private string animationTrigger = "SpecialMelee";

    private Rigidbody rb;
    private Vector3 rushDirection;
    private bool isRushing;
    private bool hasHit;
    private Coroutine rushCoroutine;

    public bool IsRushing => isRushing;

    public event Action OnRushStarted;
    public event Action<MechHealth> OnRushHit;
    public event Action OnRushEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
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
        if (Input.GetKeyDown(specialMeleeKey))
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
            rushDirection.x * rushSpeed,
            velocity.y,
            rushDirection.z * rushSpeed
        );
        TryHitTarget();
    }

    public bool TryUse()
    {
        if (isRushing
            || movementController == null
            || movementController.IsActionLocked()
            || !movementController.TryConsumeBoost(boostCost))
        {
            return false;
        }

        rushCoroutine = StartCoroutine(RushRoutine());
        return true;
    }

    private IEnumerator RushRoutine()
    {
        isRushing = true;
        hasHit = false;
        UpdateRushDirection();
        FaceRushDirection();
        movementController.ClearStepInputBuffer();
        movementController.ApplyActionLock(rushDuration + recoveryTime, true);

        if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        OnRushStarted?.Invoke();
        yield return new WaitForSeconds(rushDuration);

        isRushing = false;
        yield return new WaitForSeconds(recoveryTime);

        rushCoroutine = null;
        OnRushEnded?.Invoke();
    }

    private void UpdateRushDirection()
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;
        Vector3 direction = target != null
            ? target.position - transform.position
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

        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null || Vector3.Distance(transform.position, target.position) > hitRange)
        {
            return;
        }

        MechHealth health = target.GetComponentInParent<MechHealth>();

        if (health != null && health.TakeDamage(damage))
        {
            hasHit = true;
            OnRushHit?.Invoke(health);
        }
    }

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
    }
}
