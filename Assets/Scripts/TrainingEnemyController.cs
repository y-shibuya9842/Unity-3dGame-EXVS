using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Rigidbody))]
public class TrainingEnemyController : MonoBehaviour
{
    [Header("移動")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float randomDirectionInterval = 1.4f;

    [Header("ジャンプ")]
    [SerializeField] private float jumpAcceleration = 18f;
    [SerializeField] private float maximumUpwardSpeed = 8f;
    [SerializeField] private float jumpIdleDuration = 2f;

    [Header("オートガード")]
    [SerializeField] private float automaticGuardDuration = 1.5f;

    private Rigidbody rb;
    private BattleParticipant participant;
    private MechHealth health;
    private HitReactionController hitReaction;
    private ShieldGuard shieldGuard;
    private Transform target;
    private Vector3 randomDirection;
    private float directionTimer;
    private float jumpPhaseTimer;
    private float postHitTimer;
    private bool jumpActive;
    private bool postHitPending;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        participant = GetComponent<BattleParticipant>();
        health = GetComponent<MechHealth>();
        hitReaction = GetComponent<HitReactionController>();
        shieldGuard = GetComponent<ShieldGuard>();

        if (shieldGuard == null)
        {
            shieldGuard = gameObject.AddComponent<ShieldGuard>();
        }

        shieldGuard.SetPlayerInputEnabled(false);
        ResetMovementCycle();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
        }

        if (hitReaction != null)
        {
            hitReaction.OnReactionEnded += HandleReactionEnded;
        }
    }

    private void Update()
    {
        RefreshTarget();
        UpdatePostHitSettings();

        if (MatchSetupState.EnemyMovement == TrainingEnemyMovement.Move)
        {
            directionTimer -= Time.deltaTime;

            if (directionTimer <= 0f)
            {
                SelectRandomDirection();
            }
        }

        FaceTarget();
    }

    private void FixedUpdate()
    {
        if (!CanMove())
        {
            StopHorizontalMovement();
            return;
        }

        switch (MatchSetupState.EnemyMovement)
        {
            case TrainingEnemyMovement.Move:
                MoveHorizontal(randomDirection);
                break;
            case TrainingEnemyMovement.JumpLow:
            case TrainingEnemyMovement.JumpMiddle:
            case TrainingEnemyMovement.JumpHigh:
                UpdateJumpMovement();
                break;
            case TrainingEnemyMovement.Avoid:
                MoveTowardTarget();
                break;
            default:
                StopHorizontalMovement();
                break;
        }
    }

    private bool CanMove()
    {
        return health != null
            && !health.IsDestroyed
            && (hitReaction == null || !hitReaction.IsReacting);
    }

    private void RefreshTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            return;
        }

        BattleParticipant opponent = participant != null
            ? participant.FindNearestOpponent()
            : null;
        target = opponent != null ? opponent.transform : null;
    }

    private void MoveTowardTarget()
    {
        if (target == null)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        MoveHorizontal(direction.normalized);
    }

    private void MoveHorizontal(Vector3 direction)
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 desiredHorizontal = direction.sqrMagnitude > 0.01f
            ? direction.normalized * moveSpeed
            : Vector3.zero;
        Vector3 nextHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            desiredHorizontal,
            acceleration * Time.fixedDeltaTime
        );
        rb.linearVelocity = new Vector3(
            nextHorizontal.x,
            currentVelocity.y,
            nextHorizontal.z
        );
    }

    private void StopHorizontalMovement()
    {
        MoveHorizontal(Vector3.zero);
    }

    private void UpdateJumpMovement()
    {
        StopHorizontalMovement();
        jumpPhaseTimer -= Time.fixedDeltaTime;

        if (jumpActive && rb.linearVelocity.y < maximumUpwardSpeed)
        {
            rb.AddForce(Vector3.up * jumpAcceleration, ForceMode.Acceleration);
        }

        if (jumpPhaseTimer > 0f)
        {
            return;
        }

        jumpActive = !jumpActive;
        jumpPhaseTimer = jumpActive ? GetJumpDuration() : jumpIdleDuration;
    }

    private float GetJumpDuration()
    {
        switch (MatchSetupState.EnemyMovement)
        {
            case TrainingEnemyMovement.JumpMiddle:
                return 1f;
            case TrainingEnemyMovement.JumpHigh:
                return 2f;
            default:
                return 0.5f;
        }
    }

    private void SelectRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle.normalized;
        randomDirection = new Vector3(direction.x, 0f, direction.y);
        directionTimer = Mathf.Max(0.1f, randomDirectionInterval);
    }

    private void ResetMovementCycle()
    {
        jumpActive = true;
        jumpPhaseTimer = GetJumpDuration();
        SelectRandomDirection();
    }

    private void FaceTarget()
    {
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

    private void HandleDamaged(float damage)
    {
        if (!MatchSetupState.AutoRecovery && !MatchSetupState.AutoGuard)
        {
            return;
        }

        postHitPending = true;
        postHitTimer = 0.05f;

        // 撃破ダメージでもトレーニング用の自動回復が有効なら撃破を防ぐ。
        if (MatchSetupState.AutoRecovery
            && health != null
            && health.CurrentHealth <= 0f)
        {
            health.RestoreHealth(health.MaxHealth);
        }
    }

    private void UpdatePostHitSettings()
    {
        if (!postHitPending)
        {
            return;
        }

        postHitTimer -= Time.deltaTime;

        if (postHitTimer > 0f || (hitReaction != null && hitReaction.IsReacting))
        {
            return;
        }

        ApplyPostHitSettings();
    }

    private void HandleReactionEnded()
    {
        if (postHitPending)
        {
            ApplyPostHitSettings();
        }
    }

    private void ApplyPostHitSettings()
    {
        postHitPending = false;

        if (MatchSetupState.AutoRecovery && health != null)
        {
            health.RestoreHealth(health.MaxHealth);
        }

        if (MatchSetupState.AutoGuard && shieldGuard != null)
        {
            shieldGuard.StartAutomaticGuard(automaticGuardDuration);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
        }

        if (hitReaction != null)
        {
            hitReaction.OnReactionEnded -= HandleReactionEnded;
        }

        postHitPending = false;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        randomDirectionInterval = Mathf.Max(0.1f, randomDirectionInterval);
        jumpAcceleration = Mathf.Max(0f, jumpAcceleration);
        maximumUpwardSpeed = Mathf.Max(0f, maximumUpwardSpeed);
        jumpIdleDuration = Mathf.Max(0.1f, jumpIdleDuration);
        automaticGuardDuration = Mathf.Max(0.1f, automaticGuardDuration);
    }
}
