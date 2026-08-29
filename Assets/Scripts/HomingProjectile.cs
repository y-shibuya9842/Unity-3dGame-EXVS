using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class HomingProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float projectileSize = 0.3f;
    [SerializeField] private float speed = 35f;
    [SerializeField] private float homingStrength = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float damage = 75f;
    [SerializeField] private float hitStunDuration = 0.25f;
    [SerializeField] private float downValue = 20f;
    [SerializeField] private float knockbackSpeed = 2f;

    private Transform target;
    private Transform ownerRoot;
    private PlayerMechController targetMovementController;
    private Vector3 moveDirection;
    private bool homingEnabled;
    private float lifeTimer;
    private int targetGuidanceCutVersion;

    public void Launch(Transform newTarget, Vector3 initialDirection, Transform owner, bool enableHoming)
    {
        target = newTarget;
        ownerRoot = owner != null ? owner.root : null;
        targetMovementController = target != null
            ? target.GetComponentInParent<PlayerMechController>()
            : null;
        targetGuidanceCutVersion = targetMovementController != null
            ? targetMovementController.GuidanceCutVersion
            : 0;
        homingEnabled = enableHoming;
        moveDirection = initialDirection.sqrMagnitude > 0.01f
            ? initialDirection.normalized
            : transform.forward;
        lifeTimer = lifetime;
        ApplySize();
    }

    private void Awake()
    {
        ApplySize();
    }

    private void Update()
    {
        UpdateLifetime();
        UpdateMoveDirection();
        Move();
    }

    private void UpdateLifetime()
    {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateMoveDirection()
    {
        if (targetMovementController != null
            && targetMovementController.GuidanceCutVersion != targetGuidanceCutVersion)
        {
            // ステップされた弾は、その後も同じ対象への誘導を再開しない。
            homingEnabled = false;
        }

        if (!homingEnabled || target == null || homingStrength <= 0f)
        {
            return;
        }

        Vector3 targetDirection = (target.position - transform.position).normalized;

        // 誘導の強さに応じて、現在の進行方向を敵方向へ少しずつ寄せる。
        moveDirection = Vector3.RotateTowards(
            moveDirection,
            targetDirection,
            homingStrength * Time.deltaTime,
            0f
        ).normalized;
    }

    private void Move()
    {
        transform.position += moveDirection * speed * Time.deltaTime;

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }
    }

    private void ApplySize()
    {
        transform.localScale = Vector3.one * projectileSize;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        ShieldGuard shield = other.GetComponentInParent<ShieldGuard>();

        if (shield != null && shield.TryBlock(transform.position))
        {
            Destroy(gameObject);
            return;
        }

        MechHealth health = other.GetComponentInParent<MechHealth>();

        if (health != null && health.TakeDamage(damage))
        {
            HitReactionController reaction = other.GetComponentInParent<HitReactionController>();
            reaction?.ReceiveHit(transform.position, hitStunDuration, downValue, knockbackSpeed);
        }

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        projectileSize = Mathf.Max(0.01f, projectileSize);
        speed = Mathf.Max(0f, speed);
        homingStrength = Mathf.Max(0f, homingStrength);
        lifetime = Mathf.Max(0.01f, lifetime);
        damage = Mathf.Max(0f, damage);
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
        downValue = Mathf.Max(0f, downValue);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
    }
}
