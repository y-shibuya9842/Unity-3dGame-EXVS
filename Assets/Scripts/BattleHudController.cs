using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHudController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private MechHealth playerHealth;
    [SerializeField] private PlayerMechController playerMovement;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private BattleManager battleManager;

    [Header("Health UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthGauge;

    [Header("Boost UI")]
    [SerializeField] private Slider boostGauge;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Battle UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerCostText;
    [SerializeField] private TMP_Text enemyCostText;

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

        if (battleManager != null)
        {
            battleManager.OnTimeChanged += UpdateTime;
            battleManager.OnTeamCostChanged += UpdateTeamCost;
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

        if (battleManager != null)
        {
            UpdateTime(battleManager.RemainingTime);
            UpdateTeamCost(BattleTeam.Player, battleManager.PlayerTeamCost);
            UpdateTeamCost(BattleTeam.Enemy, battleManager.EnemyTeamCost);
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

        if (battleManager != null)
        {
            battleManager.OnTimeChanged -= UpdateTime;
            battleManager.OnTeamCostChanged -= UpdateTeamCost;
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

    private void UpdateTime(float remainingTime)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }

    private void UpdateTeamCost(BattleTeam team, int remainingCost)
    {
        TMP_Text targetText = team == BattleTeam.Player ? playerCostText : enemyCostText;

        if (targetText != null)
        {
            targetText.text = $"COST {remainingCost}";
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
