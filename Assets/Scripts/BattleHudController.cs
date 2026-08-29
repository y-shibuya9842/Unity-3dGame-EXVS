using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHudController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private MechHealth playerHealth;
    [SerializeField] private PlayerMechController playerMovement;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private SubWeaponController subWeapon;
    [SerializeField] private SpecialShotController specialShot;
    [SerializeField] private ChargeShotController chargeShot;
    [SerializeField] private AwakeningController awakeningController;
    [SerializeField] private BattleManager battleManager;

    [Header("Health UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Slider healthGauge;

    [Header("Boost UI")]
    [SerializeField] private Slider boostGauge;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text subAmmoText;
    [SerializeField] private TMP_Text specialShotAmmoText;

    [Header("Charge And Awakening UI")]
    [SerializeField] private Slider chargeGauge;
    [SerializeField] private Slider awakeningGauge;

    [Header("Battle UI")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text playerCostText;
    [SerializeField] private TMP_Text enemyCostText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureControllerExists()
    {
        GameObject battleUi = GameObject.Find("BattleUI");

        if (battleUi != null && battleUi.GetComponent<BattleHudController>() == null)
        {
            battleUi.AddComponent<BattleHudController>();
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

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

        if (subWeapon != null)
        {
            subWeapon.OnAmmoChanged += UpdateSubAmmo;
        }

        if (specialShot != null)
        {
            specialShot.OnAmmoChanged += UpdateSpecialShotAmmo;
        }

        if (chargeShot != null)
        {
            chargeShot.OnChargeChanged += UpdateCharge;
        }

        if (awakeningController != null)
        {
            awakeningController.OnGaugeChanged += UpdateAwakening;
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

        if (subWeapon != null)
        {
            UpdateSubAmmo(subWeapon.CurrentAmmo, subWeapon.MaxAmmo);
        }

        if (specialShot != null)
        {
            UpdateSpecialShotAmmo(specialShot.CurrentAmmo, specialShot.MaxAmmo);
        }

        if (chargeShot != null)
        {
            UpdateCharge(chargeShot.ChargeRate);
        }

        if (awakeningController != null)
        {
            UpdateAwakening(
                awakeningController.CurrentGauge,
                awakeningController.MaxGauge
            );
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

        if (subWeapon != null)
        {
            subWeapon.OnAmmoChanged -= UpdateSubAmmo;
        }

        if (specialShot != null)
        {
            specialShot.OnAmmoChanged -= UpdateSpecialShotAmmo;
        }

        if (chargeShot != null)
        {
            chargeShot.OnChargeChanged -= UpdateCharge;
        }

        if (awakeningController != null)
        {
            awakeningController.OnGaugeChanged -= UpdateAwakening;
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
            ammoText.text = $"AMMO {current}";
        }
    }

    private void UpdateSubAmmo(int current, int maximum)
    {
        if (subAmmoText != null)
        {
            subAmmoText.text = $"SUB {current}/{maximum}";
        }
    }

    private void UpdateSpecialShotAmmo(int current, int maximum)
    {
        if (specialShotAmmoText != null)
        {
            specialShotAmmoText.text = $"SPECIAL {current}/{maximum}";
        }
    }

    private void UpdateCharge(float chargeRate)
    {
        UpdateGauge(chargeGauge, chargeRate, 1f);
    }

    private void UpdateAwakening(float current, float maximum)
    {
        UpdateGauge(awakeningGauge, current, maximum);
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

    private void ResolveReferences()
    {
        GameObject player = FindPlayerMech();

        if (player != null)
        {
            playerHealth ??= player.GetComponent<MechHealth>();
            playerMovement ??= player.GetComponent<PlayerMechController>();
            playerShooter ??= player.GetComponent<PlayerShooter>();
            subWeapon ??= player.GetComponent<SubWeaponController>();
            specialShot ??= player.GetComponent<SpecialShotController>();
            chargeShot ??= player.GetComponent<ChargeShotController>();
            awakeningController ??= player.GetComponent<AwakeningController>();
        }

        battleManager ??= FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
        healthText ??= FindNamedComponent<TMP_Text>(transform, "HpText");
        ammoText ??= FindNamedComponent<TMP_Text>(transform, "AmmoText");
        boostGauge ??= FindNamedComponent<Slider>(transform, "BoostGauge");
    }

    private static GameObject FindPlayerMech()
    {
        BattleParticipant[] participants = FindObjectsByType<BattleParticipant>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (BattleParticipant participant in participants)
        {
            if (participant.Team == BattleTeam.Player)
            {
                return participant.gameObject;
            }
        }

        return null;
    }

    private static T FindNamedComponent<T>(Transform parent, string objectName)
        where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name.Trim() == objectName)
        {
            return parent.GetComponent<T>();
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            T match = FindNamedComponent<T>(parent.GetChild(i), objectName);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
