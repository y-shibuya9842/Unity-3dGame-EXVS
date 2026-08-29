using System;
using System.Collections;
using UnityEngine;

public class SubWeaponController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode subWeaponKey = KeyCode.Q;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;

    [Header("Volley")]
    [SerializeField] private int projectileCount = 2;
    [SerializeField] private float projectileInterval = 0.08f;
    [SerializeField] private float spreadAngle = 6f;
    [SerializeField] private float actionLockDuration = 0.45f;
    [SerializeField] private float useCooldown = 0.5f;

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 2;
    [SerializeField] private float ammoRecoveryTime = 6f;

    private int currentAmmo;
    private float cooldownTimer;
    private float ammoRecoveryTimer;
    private Coroutine volleyCoroutine;
    private Transform volleyTarget;
    private PlayerMechController targetMovementController;
    private int targetGuidanceCutVersion;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    public event Action<int, int> OnAmmoChanged;

    private void Awake()
    {
        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        currentAmmo = maxAmmo;
        ammoRecoveryTimer = ammoRecoveryTime;
    }

    private void Update()
    {
        UpdateTimers();

        if (Input.GetKeyDown(subWeaponKey))
        {
            TryUse();
        }
    }

    public bool TryUse()
    {
        if (!CanUse())
        {
            return false;
        }

        SetCurrentAmmo(currentAmmo - 1);
        ammoRecoveryTimer = ammoRecoveryTime;
        cooldownTimer = useCooldown;
        movementController?.ClearStepInputBuffer();
        movementController?.ApplyActionLock(actionLockDuration, true);
        CaptureTargetingState();
        volleyCoroutine = StartCoroutine(FireVolley());
        return true;
    }

    private bool CanUse()
    {
        return projectilePrefab != null
            && currentAmmo > 0
            && cooldownTimer <= 0f
            && volleyCoroutine == null
            && (movementController == null || !movementController.IsActionLocked());
    }

    private IEnumerator FireVolley()
    {
        int count = Mathf.Max(1, projectileCount);

        for (int i = 0; i < count; i++)
        {
            FireProjectile(i, count);

            if (i < count - 1 && projectileInterval > 0f)
            {
                yield return new WaitForSeconds(projectileInterval);
            }
        }

        volleyCoroutine = null;
    }

    private void FireProjectile(int index, int count)
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;
        bool guidanceWasCut = WasGuidanceCut();
        Vector3 baseDirection = guidanceWasCut
            ? spawnPoint.forward
            : GetBaseDirection(spawnPoint, volleyTarget);
        float angle = count <= 1
            ? 0f
            : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, (float)index / (count - 1));
        Vector3 shootDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;

        HomingProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(shootDirection, Vector3.up)
        );
        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(
            volleyTarget,
            shootDirection,
            transform,
            enableHoming && !guidanceWasCut
        );
    }

    private void CaptureTargetingState()
    {
        volleyTarget = lockOnController != null ? lockOnController.CurrentTarget : null;
        targetMovementController = volleyTarget != null
            ? volleyTarget.GetComponentInParent<PlayerMechController>()
            : null;
        targetGuidanceCutVersion = targetMovementController != null
            ? targetMovementController.GuidanceCutVersion
            : 0;
    }

    private bool WasGuidanceCut()
    {
        return targetMovementController != null
            && targetMovementController.GuidanceCutVersion != targetGuidanceCutVersion;
    }

    private static Vector3 GetBaseDirection(Transform spawnPoint, Transform target)
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

    private void UpdateTimers()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (currentAmmo >= maxAmmo)
        {
            return;
        }

        ammoRecoveryTimer -= Time.deltaTime;

        if (ammoRecoveryTimer <= 0f)
        {
            SetCurrentAmmo(currentAmmo + 1);
            ammoRecoveryTimer = ammoRecoveryTime;
        }
    }

    private void SetCurrentAmmo(int value)
    {
        int nextAmmo = Mathf.Clamp(value, 0, maxAmmo);

        if (nextAmmo == currentAmmo)
        {
            return;
        }

        currentAmmo = nextAmmo;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    private void OnDisable()
    {
        if (volleyCoroutine != null)
        {
            StopCoroutine(volleyCoroutine);
            volleyCoroutine = null;
        }
    }

    private void OnValidate()
    {
        projectileCount = Mathf.Max(1, projectileCount);
        projectileInterval = Mathf.Max(0f, projectileInterval);
        spreadAngle = Mathf.Clamp(spreadAngle, 0f, 180f);
        actionLockDuration = Mathf.Max(0f, actionLockDuration);
        useCooldown = Mathf.Max(0f, useCooldown);
        maxAmmo = Mathf.Max(1, maxAmmo);
        ammoRecoveryTime = Mathf.Max(0.01f, ammoRecoveryTime);
    }
}
