using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;

    [Header("References")]
    [SerializeField] private Transform rotationRoot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform target;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;

    [Header("Shoot")]
    [SerializeField] private float shootCooldown = 0.25f;
    [SerializeField] private float shootActionLock = 0.35f;
    [SerializeField] private bool turnToTargetBeforeShoot = true;
    [SerializeField] private float turnShotAngle = 60f;
    [SerializeField] private float turnShotActionLock = 0.45f;

    private float shootCooldownTimer;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void Update()
    {
        UpdateCooldown();

        if (Input.GetKeyDown(shootKey) && CanShoot())
        {
            Shoot();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Shoot()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("Projectile Prefab is not assigned.", this);
            return;
        }

        bool didTurnShot = TurnToTargetIfNeeded();
        movementController?.ClearStepInputBuffer();

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 shootDirection = GetShootDirection(spawnPoint);
        HomingProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(shootDirection, Vector3.up)
        );

        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(target, shootDirection, transform, enableHoming);
        
        float actionLock = didTurnShot ? turnShotActionLock : shootActionLock;
        movementController?.ApplyActionLock(actionLock, true);

        shootCooldownTimer = shootCooldown;
    }

    private Vector3 GetShootDirection(Transform spawnPoint)
    {
        if (target != null)
        {
            Vector3 targetDirection = target.position - spawnPoint.position;

            if (targetDirection.sqrMagnitude > 0.01f)
            {
                return targetDirection.normalized;
            }
        }

        return spawnPoint.forward;
    }

    private bool TurnToTargetIfNeeded()
    {
        if (!turnToTargetBeforeShoot || target == null)
        {
            return false;
        }

        Transform root = rotationRoot != null ? rotationRoot : transform;
        Vector3 targetDirection = target.position - root.position;
        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude <= 0.01f)
        {
            return false;
        }

        targetDirection.Normalize();
        float angleToTarget = Vector3.Angle(root.forward, targetDirection);

        // 正面から大きく外れているときだけ、射撃前に敵方向へ向き直る。
        if (angleToTarget < turnShotAngle)
        {
            return false;
        }

        root.rotation = Quaternion.LookRotation(targetDirection, Vector3.up);
        return true;
    }

    private bool CanShoot()
    {
        return shootCooldownTimer <= 0f
            && (movementController == null || !movementController.IsActionLocked());
    }

    private void UpdateCooldown()
    {
        if (shootCooldownTimer > 0f)
        {
            shootCooldownTimer -= Time.deltaTime;
        }
    }
}
