using System;
using System.Collections;
using UnityEngine;

public class SpecialShotController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode specialShotKey = KeyCode.C;

    [Header("References")]
    [SerializeField] private Transform rotationRoot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private HomingProjectile projectilePrefab;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private Animator animator;

    [Header("Weapon Data")]
    [SerializeField] private RangedWeaponDefinition weaponDefinition;

    [Header("Special Shot")]
    [SerializeField] private float startupTime = 0.35f;
    [SerializeField] private float totalActionLock = 0.9f;
    [SerializeField] private float useCooldown = 1f;
    [SerializeField] private float recoilSpeed = 3f;
    [SerializeField] private string animationTrigger = "SpecialShot";

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 1;
    [SerializeField] private float ammoRecoveryTime = 10f;

    private Rigidbody rb;
    private int currentAmmo;
    private float cooldownTimer;
    private float ammoRecoveryTimer;
    private Coroutine shotCoroutine;
    private Transform preparedTarget;
    private PlayerMechController targetMovementController;
    private int targetGuidanceCutVersion;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => GetMaxAmmo();

    public event Action<int, int> OnAmmoChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        currentAmmo = GetMaxAmmo();
        ammoRecoveryTimer = GetReloadTime();
    }

    private void OnEnable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted += CancelBeforeFiring;
        }
    }

    private void Update()
    {
        UpdateTimers();

        if (Input.GetKeyDown(specialShotKey))
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
        ammoRecoveryTimer = GetReloadTime();
        cooldownTimer = GetCooldown();
        movementController?.ClearStepInputBuffer();
        movementController?.ApplyActionLock(GetActionLockDuration(), true);
        FaceCurrentTarget();
        CaptureTargetingState();

        if (animator != null && !string.IsNullOrWhiteSpace(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        shotCoroutine = StartCoroutine(FireAfterStartup());
        return true;
    }

    private bool CanUse()
    {
        return GetProjectilePrefab() != null
            && currentAmmo > 0
            && cooldownTimer <= 0f
            && shotCoroutine == null
            && (movementController == null || !movementController.IsActionLocked());
    }

    private IEnumerator FireAfterStartup()
    {
        float activeStartupTime = GetStartupTime();

        if (activeStartupTime > 0f)
        {
            yield return new WaitForSeconds(activeStartupTime);
        }

        FireProjectile();
        ApplyRecoil();
        shotCoroutine = null;
    }

    private void FireProjectile()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;
        bool guidanceWasCut = WasGuidanceCut();
        Vector3 shootDirection = guidanceWasCut
            ? spawnPoint.forward
            : GetShootDirection(spawnPoint, preparedTarget);
        HomingProjectile projectile = Instantiate(
            GetProjectilePrefab(),
            spawnPoint.position,
            Quaternion.LookRotation(shootDirection, Vector3.up)
        );
        bool enableHoming = lockOnController == null
            || lockOnController.CurrentLockState == LockState.Red;
        projectile.Launch(
            preparedTarget,
            shootDirection,
            transform,
            enableHoming && !guidanceWasCut
        );
    }

    private void CaptureTargetingState()
    {
        preparedTarget = lockOnController != null ? lockOnController.CurrentTarget : null;
        targetMovementController = preparedTarget != null
            ? preparedTarget.GetComponentInParent<PlayerMechController>()
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

    private void FaceCurrentTarget()
    {
        Transform target = lockOnController != null ? lockOnController.CurrentTarget : null;

        if (target == null)
        {
            return;
        }

        Transform root = rotationRoot != null ? rotationRoot : transform;
        Vector3 direction = target.position - root.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ApplyRecoil()
    {
        float activeRecoilSpeed = GetRecoilSpeed();

        if (rb == null || activeRecoilSpeed <= 0f)
        {
            return;
        }

        Transform root = rotationRoot != null ? rotationRoot : transform;
        Vector3 recoilDirection = -Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(
            recoilDirection.x * activeRecoilSpeed,
            velocity.y,
            recoilDirection.z * activeRecoilSpeed
        );
    }

    private void CancelBeforeFiring()
    {
        if (shotCoroutine == null)
        {
            return;
        }

        StopCoroutine(shotCoroutine);
        shotCoroutine = null;
    }

    private void UpdateTimers()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (currentAmmo >= GetMaxAmmo())
        {
            return;
        }

        ammoRecoveryTimer -= Time.deltaTime;

        if (ammoRecoveryTimer <= 0f)
        {
            int reloadedAmmo = weaponDefinition != null
                ? weaponDefinition.GetReloadedAmmo(currentAmmo)
                : currentAmmo + 1;
            SetCurrentAmmo(reloadedAmmo);
            ammoRecoveryTimer = GetReloadTime();
        }
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
        return weaponDefinition != null ? weaponDefinition.Cooldown : useCooldown;
    }

    private float GetActionLockDuration()
    {
        return weaponDefinition != null
            ? weaponDefinition.ActionLockDuration
            : totalActionLock;
    }

    private float GetStartupTime()
    {
        return weaponDefinition != null ? weaponDefinition.StartupTime : startupTime;
    }

    private float GetRecoilSpeed()
    {
        return weaponDefinition != null ? weaponDefinition.RecoilSpeed : recoilSpeed;
    }

    private void OnDisable()
    {
        if (movementController != null)
        {
            movementController.OnBoostStarted -= CancelBeforeFiring;
        }

        CancelBeforeFiring();
    }

    private void OnValidate()
    {
        startupTime = Mathf.Max(0f, startupTime);
        totalActionLock = Mathf.Max(startupTime, totalActionLock);
        useCooldown = Mathf.Max(0f, useCooldown);
        recoilSpeed = Mathf.Max(0f, recoilSpeed);
        maxAmmo = Mathf.Max(1, maxAmmo);
        ammoRecoveryTime = Mathf.Max(0.01f, ammoRecoveryTime);
    }
}
