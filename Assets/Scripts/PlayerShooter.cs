using System;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rotationRoot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform target;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;

    [Header("Weapon Data")]
    [SerializeField] private RangedWeaponDefinition weaponDefinition;

    [Header("Shoot")]
    [SerializeField] private float shootCooldown = 0.25f;
    [SerializeField] private float shootActionLock = 0.35f;
    [SerializeField] private bool turnToTargetBeforeShoot = true;
    [SerializeField] private float turnShotAngle = 60f;
    [SerializeField] private float turnShotActionLock = 0.45f;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 8;
    [SerializeField] private float ammoRecoveryTime = 3f;

    private float shootCooldownTimer;
    private float ammoRecoveryTimer;
    private float shootingIntervalMultiplier = 1f;
    private bool playerInputEnabled = true;
    private int currentAmmo;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => GetMaxAmmo();

    public event Action<int, int> OnAmmoChanged;

    private void Awake()
    {
        currentAmmo = GetMaxAmmo();
        ammoRecoveryTimer = GetReloadTime();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void Update()
    {
        UpdateCooldown();
        UpdateAmmoRecovery();

        if (playerInputEnabled
            && VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.MainShot))
        {
            TryShoot();
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public bool TryShoot()
    {
        if (!CanShoot())
        {
            return false;
        }

        Shoot();
        return true;
    }

    public void SetPlayerInputEnabled(bool enabled)
    {
        playerInputEnabled = enabled;
    }

    private void Shoot()
    {
        HomingProjectile activeProjectilePrefab = GetProjectilePrefab();

        if (activeProjectilePrefab == null)
        {
            Debug.LogWarning("Projectile Prefab is not assigned.", this);
            return;
        }

        bool didTurnShot = TurnToTargetIfNeeded();
        movementController?.ClearStepInputBuffer();

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 shootDirection = GetShootDirection(spawnPoint);
        HomingProjectile projectile = Instantiate(
            activeProjectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(shootDirection, Vector3.up)
        );

        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(target, shootDirection, transform, enableHoming);
        
        float actionLock = didTurnShot ? turnShotActionLock : GetActionLockDuration();
        movementController?.ApplyActionLock(actionLock, true);

        SetCurrentAmmo(currentAmmo - 1);
        ammoRecoveryTimer = GetReloadTime();
        shootCooldownTimer = GetCooldown() * shootingIntervalMultiplier;
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
            && currentAmmo > 0
            && (movementController == null || !movementController.IsActionLocked());
    }

    private void UpdateCooldown()
    {
        if (shootCooldownTimer > 0f)
        {
            shootCooldownTimer -= Time.deltaTime;
        }
    }

    private void UpdateAmmoRecovery()
    {
        if (currentAmmo >= GetMaxAmmo())
        {
            return;
        }

        if (weaponDefinition != null
            && !weaponDefinition.ShouldStartReload(currentAmmo))
        {
            return;
        }

        ammoRecoveryTimer -= Time.deltaTime;

        if (ammoRecoveryTimer > 0f)
        {
            return;
        }

        int reloadedAmmo = weaponDefinition != null
            ? weaponDefinition.GetReloadedAmmo(currentAmmo)
            : currentAmmo + 1;
        SetCurrentAmmo(reloadedAmmo);
        ammoRecoveryTimer = GetReloadTime();
    }

    private void SetCurrentAmmo(int value)
    {
        int maximum = GetMaxAmmo();
        int nextAmmo = Mathf.Clamp(value, 0, maximum);

        if (nextAmmo == currentAmmo)
        {
            return;
        }

        currentAmmo = nextAmmo;
        OnAmmoChanged?.Invoke(currentAmmo, maximum);
    }

    private HomingProjectile GetProjectilePrefab()
    {
        return weaponDefinition != null && weaponDefinition.ProjectilePrefab != null
            ? weaponDefinition.ProjectilePrefab
            : projectilePrefab;
    }

    private int GetMaxAmmo()
    {
        return weaponDefinition != null ? weaponDefinition.MaxAmmo : Mathf.Max(1, maxAmmo);
    }

    private float GetReloadTime()
    {
        return weaponDefinition != null ? weaponDefinition.ReloadTime : ammoRecoveryTime;
    }

    private float GetCooldown()
    {
        return weaponDefinition != null ? weaponDefinition.Cooldown : shootCooldown;
    }

    private float GetActionLockDuration()
    {
        return weaponDefinition != null
            ? weaponDefinition.ActionLockDuration
            : shootActionLock;
    }

    public void SetShootingIntervalMultiplier(float multiplier)
    {
        shootingIntervalMultiplier = Mathf.Max(0.01f, multiplier);
    }

    private void OnValidate()
    {
        maxAmmo = Mathf.Max(1, maxAmmo);
        ammoRecoveryTime = Mathf.Max(0.01f, ammoRecoveryTime);
    }
}
