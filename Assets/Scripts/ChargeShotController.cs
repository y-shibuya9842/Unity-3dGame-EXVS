using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ChargeShotController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private HomingProjectile chargedProjectilePrefab;
    [SerializeField] private PlayerShooter mainShooter;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;

    [Header("Weapon Data")]
    [SerializeField] private RangedWeaponDefinition weaponDefinition;

    [Header("Charge Shot")]
    [SerializeField] private float chargeTime = 2f;
    [SerializeField] private float actionLockDuration = 0.65f;

    private float currentChargeTime;
    private bool isCharging;

    public float ChargeRate => GetChargeTime() > 0f
        ? Mathf.Clamp01(currentChargeTime / GetChargeTime())
        : 1f;
    public bool IsFullyCharged => isCharging && ChargeRate >= 1f;

    public event Action<float> OnChargeChanged;
    public event Action OnChargeCompleted;

    private void Awake()
    {
        if (mainShooter == null)
        {
            mainShooter = GetComponent<PlayerShooter>();
        }

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }
    }

    private void Update()
    {
        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.MainShot))
        {
            BeginCharge();
        }

        if (isCharging && VersusInputManager.Instance.IsPressed(VersusInputAction.MainShot))
        {
            UpdateCharge();
        }

        if (isCharging
            && VersusInputManager.Instance.WasReleasedThisFrame(VersusInputAction.MainShot))
        {
            ReleaseCharge();
        }
    }

    private void BeginCharge()
    {
        isCharging = true;
        currentChargeTime = 0f;

        // 長押し判定中はPlayerShooter側の即時発射を一時停止する。
        mainShooter?.SetPlayerInputEnabled(false);
        OnChargeChanged?.Invoke(0f);
    }

    private void UpdateCharge()
    {
        bool wasFullyCharged = IsFullyCharged;
        currentChargeTime = Mathf.Min(currentChargeTime + Time.deltaTime, GetChargeTime());
        OnChargeChanged?.Invoke(ChargeRate);

        if (!wasFullyCharged && IsFullyCharged)
        {
            OnChargeCompleted?.Invoke();
        }
    }

    private void ReleaseCharge()
    {
        bool fireChargedShot = IsFullyCharged;
        isCharging = false;
        mainShooter?.SetPlayerInputEnabled(true);

        if (fireChargedShot)
        {
            TryFireChargedShot();
        }
        else
        {
            // 最大まで溜めずに離した場合は通常のメイン射撃として扱う。
            mainShooter?.TryShoot();
        }

        currentChargeTime = 0f;
        OnChargeChanged?.Invoke(0f);
    }

    private bool TryFireChargedShot()
    {
        HomingProjectile activeProjectilePrefab = GetProjectilePrefab();

        if (activeProjectilePrefab == null
            || (movementController != null && movementController.IsActionLocked()))
        {
            return false;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;
        Vector3 shootDirection = GetShootDirection(spawnPoint, target);
        HomingProjectile projectile = Instantiate(
            activeProjectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(shootDirection, Vector3.up)
        );
        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(target, shootDirection, transform, enableHoming);

        movementController?.ClearStepInputBuffer();
        movementController?.ApplyActionLock(GetActionLockDuration(), true);
        return true;
    }

    private HomingProjectile GetProjectilePrefab()
    {
        return weaponDefinition != null && weaponDefinition.ProjectilePrefab != null
            ? weaponDefinition.ProjectilePrefab
            : chargedProjectilePrefab;
    }

    private float GetChargeTime()
    {
        return weaponDefinition != null ? weaponDefinition.ChargeTime : chargeTime;
    }

    private float GetActionLockDuration()
    {
        return weaponDefinition != null
            ? weaponDefinition.ActionLockDuration
            : actionLockDuration;
    }

    private static Vector3 GetShootDirection(Transform spawnPoint, Transform target)
    {
        if (target != null)
        {
            Vector3 direction = target.position - spawnPoint.position;

            if (direction.sqrMagnitude > 0.01f)
            {
                return direction.normalized;
            }
        }

        return spawnPoint.forward;
    }

    private void OnDisable()
    {
        if (isCharging)
        {
            mainShooter?.SetPlayerInputEnabled(true);
        }

        isCharging = false;
        currentChargeTime = 0f;
    }

    private void OnValidate()
    {
        chargeTime = Mathf.Max(0.01f, chargeTime);
        actionLockDuration = Mathf.Max(0f, actionLockDuration);
    }
}
