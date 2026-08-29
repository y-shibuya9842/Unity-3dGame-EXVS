using System;
using System.Collections;
using UnityEngine;

public enum AwakeningType
{
    Fighting,
    Shooting,
    Mobility,
    Custom
}

public class AwakeningController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode awakeningKey = KeyCode.R;

    [Header("Gauge")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float activationCost = 100f;
    [SerializeField] private float gaugeGainPerDamage = 0.2f;

    [Header("Effect Type")]
    [SerializeField] private AwakeningType awakeningType = AwakeningType.Custom;

    [Header("Custom Effect")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private float movementSpeedMultiplier = 1.2f;
    [SerializeField] private float shootingIntervalMultiplier = 0.8f;
    [SerializeField] private float meleeDamageMultiplier = 1.1f;

    [Header("References")]
    [SerializeField] private MechHealth health;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private MeleeAttackController meleeController;

    private float currentGauge;
    private bool isAwakened;

    public float CurrentGauge => currentGauge;
    public float MaxGauge => maxGauge;
    public bool IsAwakened => isAwakened;

    public event Action<float, float> OnGaugeChanged;
    public event Action OnAwakeningStarted;
    public event Action OnAwakeningEnded;

    private void Awake()
    {
        if (health == null)
        {
            health = GetComponent<MechHealth>();
        }

        if (movementController == null)
        {
            movementController = GetComponent<PlayerMechController>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }

        if (meleeController == null)
        {
            meleeController = GetComponent<MeleeAttackController>();
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += AddGaugeFromDamage;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(awakeningKey) && CanActivate())
        {
            StartCoroutine(AwakeningRoutine());
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= AddGaugeFromDamage;
        }

        StopAllCoroutines();
        ResetEffects();
    }

    private bool CanActivate()
    {
        return !isAwakened && currentGauge >= activationCost;
    }

    private IEnumerator AwakeningRoutine()
    {
        isAwakened = true;
        SetGauge(currentGauge - activationCost);
        movementController?.CancelActionLock();
        ApplySelectedEffects();
        OnAwakeningStarted?.Invoke();

        yield return new WaitForSeconds(duration);

        ResetEffects();
        OnAwakeningEnded?.Invoke();
    }

    private void AddGaugeFromDamage(float damage)
    {
        if (!isAwakened)
        {
            SetGauge(currentGauge + damage * gaugeGainPerDamage);
        }
    }

    private void SetGauge(float value)
    {
        float nextGauge = Mathf.Clamp(value, 0f, maxGauge);

        if (Mathf.Approximately(nextGauge, currentGauge))
        {
            return;
        }

        currentGauge = nextGauge;
        OnGaugeChanged?.Invoke(currentGauge, maxGauge);
    }

    private void ResetEffects()
    {
        isAwakened = false;
        movementController?.SetMovementSpeedMultiplier(1f);
        playerShooter?.SetShootingIntervalMultiplier(1f);
        meleeController?.SetDamageMultiplier(1f);
    }

    private void ApplySelectedEffects()
    {
        float moveMultiplier;
        float shootMultiplier;
        float meleeMultiplier;

        switch (awakeningType)
        {
            case AwakeningType.Fighting:
                moveMultiplier = 1.15f;
                shootMultiplier = 0.95f;
                meleeMultiplier = 1.25f;
                break;
            case AwakeningType.Shooting:
                moveMultiplier = 1.1f;
                shootMultiplier = 0.7f;
                meleeMultiplier = 1f;
                break;
            case AwakeningType.Mobility:
                moveMultiplier = 1.35f;
                shootMultiplier = 0.9f;
                meleeMultiplier = 1.05f;
                break;
            default:
                moveMultiplier = movementSpeedMultiplier;
                shootMultiplier = shootingIntervalMultiplier;
                meleeMultiplier = meleeDamageMultiplier;
                break;
        }

        movementController?.SetMovementSpeedMultiplier(moveMultiplier);
        playerShooter?.SetShootingIntervalMultiplier(shootMultiplier);
        meleeController?.SetDamageMultiplier(meleeMultiplier);
    }

    private void OnValidate()
    {
        maxGauge = Mathf.Max(1f, maxGauge);
        activationCost = Mathf.Clamp(activationCost, 1f, maxGauge);
        gaugeGainPerDamage = Mathf.Max(0f, gaugeGainPerDamage);
        duration = Mathf.Max(0.1f, duration);
        movementSpeedMultiplier = Mathf.Max(0.01f, movementSpeedMultiplier);
        shootingIntervalMultiplier = Mathf.Max(0.01f, shootingIntervalMultiplier);
        meleeDamageMultiplier = Mathf.Max(0.01f, meleeDamageMultiplier);
    }
}
