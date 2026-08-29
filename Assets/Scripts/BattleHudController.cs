using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHudController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private MechHealth playerHealth;
    [SerializeField] private PlayerMechController playerMovement;
    [SerializeField] private PlayerShooter playerShooter;

    [Header("Health UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthGauge;

    [Header("Boost UI")]
    [SerializeField] private Slider boostGauge;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
        }

        if (playerMovement != null)
        {
            playerMovement.OnBoostChanged += UpdateBoost;
        }

        if (playerShooter != null)
        {
            playerShooter.OnAmmoChanged += UpdateAmmo;
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (playerMovement != null)
        {
            UpdateBoost(playerMovement.CurrentBoost, playerMovement.MaxBoost);
        }

        if (playerShooter != null)
        {
            UpdateAmmo(playerShooter.CurrentAmmo, playerShooter.MaxAmmo);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealth;
        }

        if (playerMovement != null)
        {
            playerMovement.OnBoostChanged -= UpdateBoost;
        }

        if (playerShooter != null)
        {
            playerShooter.OnAmmoChanged -= UpdateAmmo;
        }
    }

    private void UpdateHealth(float current, float maximum)
    {
        if (healthText != null)
        {
            healthText.text = $"HP {Mathf.CeilToInt(current)}";
        }

        UpdateGauge(healthGauge, current, maximum);
    }

    private void UpdateBoost(float current, float maximum)
    {
        UpdateGauge(boostGauge, current, maximum);
    }

    private void UpdateAmmo(int current, int maximum)
    {
        if (ammoText != null)
        {
            ammoText.text = $"AMMO {current}/{maximum}";
        }
    }

    private static void UpdateGauge(Slider gauge, float current, float maximum)
    {
        if (gauge == null)
        {
            return;
        }

        gauge.minValue = 0f;
        gauge.maxValue = Mathf.Max(1f, maximum);
        gauge.value = Mathf.Clamp(current, 0f, gauge.maxValue);
    }
}
