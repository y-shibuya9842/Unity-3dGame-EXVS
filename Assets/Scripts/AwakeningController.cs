using System;
using System.Collections;
using UnityEngine;

public class AwakeningController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode awakeningKey = KeyCode.R;

    [Header("Gauge")]
    [SerializeField] private float maxGauge = 100f;
    [SerializeField] private float activationCost = 100f;
    [SerializeField] private float gaugeGainPerDamage = 0.2f;

    [Header("Effect")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private float movementSpeedMultiplier = 1.2f;
    [SerializeField] private float shootingIntervalMultiplier = 0.8f;

    [Header("References")]
    [SerializeField] private MechHealth health;
    [SerializeField] private PlayerMechController movementController;
    [SerializeField] private PlayerShooter playerShooter;

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
        movementController?.SetMovementSpeedMultiplier(movementSpeedMultiplier);
        playerShooter?.SetShootingIntervalMultiplier(shootingIntervalMultiplier);
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
    }

    private void OnValidate()
    {
        maxGauge = Mathf.Max(1f, maxGauge);
        activationCost = Mathf.Clamp(activationCost, 1f, maxGauge);
        gaugeGainPerDamage = Mathf.Max(0f, gaugeGainPerDamage);
        duration = Mathf.Max(0.1f, duration);
        movementSpeedMultiplier = Mathf.Max(0.01f, movementSpeedMultiplier);
        shootingIntervalMultiplier = Mathf.Max(0.01f, shootingIntervalMultiplier);
    }
}
