using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HitReactionController : MonoBehaviour
{
    [Header("Down Gauge")]
    [SerializeField] private float downThreshold = 100f;
    [SerializeField] private float downValueRecoveryPerSecond = 35f;
    [SerializeField] private float recoveryDelay = 1.5f;

    [Header("Down")]
    [SerializeField] private float downDuration = 1.6f;
    [SerializeField] private float downLaunchSpeed = 5f;

    [Header("References")]
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private Animator animator;
    [SerializeField] private string hitAnimationTrigger = "Hit";
    [SerializeField] private string downAnimationBool = "Down";

    private Rigidbody rb;
    private float currentDownValue;
    private float reactionTimer;
    private float recoveryDelayTimer;
    private bool isDown;

    public float CurrentDownValue => currentDownValue;
    public float DownThreshold => downThreshold;
    public bool IsReacting => reactionTimer > 0f;
    public bool IsDown => isDown;

    public event Action OnHitStunStarted;
    public event Action OnDownStarted;
    public event Action OnReactionEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void Update()
    {
        UpdateReaction();
        RecoverDownValue();
    }

    public void ReceiveHit(
        Vector3 attackOrigin,
        float hitStunDuration,
        float downValue,
        float knockbackSpeed)
    {
        if (isDown)
        {
            return;
        }

        currentDownValue = Mathf.Min(currentDownValue + Mathf.Max(0f, downValue), downThreshold);
        recoveryDelayTimer = recoveryDelay;

        if (currentDownValue >= downThreshold)
        {
            StartDown(attackOrigin);
            return;
        }

        reactionTimer = Mathf.Max(reactionTimer, hitStunDuration);
        movementController?.ApplyActionLock(reactionTimer, false);
        ApplyKnockback(attackOrigin, knockbackSpeed, false);
        PlayHitAnimation();
        OnHitStunStarted?.Invoke();
    }

    private void StartDown(Vector3 attackOrigin)
    {
        isDown = true;
        reactionTimer = downDuration;
        movementController?.ApplyActionLock(downDuration, false);
        ApplyKnockback(attackOrigin, downLaunchSpeed, true);

        if (animator != null && !string.IsNullOrWhiteSpace(downAnimationBool))
        {
            animator.SetBool(downAnimationBool, true);
        }

        OnDownStarted?.Invoke();
    }

    private void ApplyKnockback(Vector3 attackOrigin, float speed, bool launchUpward)
    {
        if (speed <= 0f)
        {
            return;
        }

        Vector3 direction = transform.position - attackOrigin;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();
        float verticalSpeed = launchUpward ? speed * 0.6f : rb.linearVelocity.y;
        rb.linearVelocity = new Vector3(direction.x * speed, verticalSpeed, direction.z * speed);
    }

    private void PlayHitAnimation()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(hitAnimationTrigger))
        {
            animator.SetTrigger(hitAnimationTrigger);
        }
    }

    private void UpdateReaction()
    {
        if (reactionTimer <= 0f)
        {
            return;
        }

        reactionTimer -= Time.deltaTime;

        if (reactionTimer > 0f)
        {
            return;
        }

        reactionTimer = 0f;
        isDown = false;

        if (animator != null && !string.IsNullOrWhiteSpace(downAnimationBool))
        {
            animator.SetBool(downAnimationBool, false);
        }

        OnReactionEnded?.Invoke();
    }

    private void RecoverDownValue()
    {
        if (isDown || currentDownValue <= 0f)
        {
            return;
        }

        if (recoveryDelayTimer > 0f)
        {
            recoveryDelayTimer -= Time.deltaTime;
            return;
        }

        currentDownValue = Mathf.Max(
            0f,
            currentDownValue - downValueRecoveryPerSecond * Time.deltaTime
        );
    }

    private void OnDisable()
    {
        reactionTimer = 0f;
        recoveryDelayTimer = 0f;
        currentDownValue = 0f;
        isDown = false;

        if (animator != null && !string.IsNullOrWhiteSpace(downAnimationBool))
        {
            animator.SetBool(downAnimationBool, false);
        }
    }

    private void OnValidate()
    {
        downThreshold = Mathf.Max(1f, downThreshold);
        downValueRecoveryPerSecond = Mathf.Max(0f, downValueRecoveryPerSecond);
        recoveryDelay = Mathf.Max(0f, recoveryDelay);
        downDuration = Mathf.Max(0.01f, downDuration);
        downLaunchSpeed = Mathf.Max(0f, downLaunchSpeed);
    }
}
