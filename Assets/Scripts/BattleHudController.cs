using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class BattleHudController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private MechLoadoutController playerLoadout;
    [SerializeField] private MechHealth playerHealth;
    [SerializeField] private PlayerMechController playerMovement;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private SubWeaponController subWeapon;
    [SerializeField] private SpecialShotController specialShot;
    [SerializeField] private ChargeShotController chargeShot;
    [SerializeField] private AwakeningController awakeningController;
    [SerializeField] private LockOnController lockOnController;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleParticipant partnerParticipant;

    private MechHealth partnerHealth;
    private bool partnerHealthSubscribed;
    private BattleParticipant targetParticipant;
    private MechHealth targetHealth;

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

    [Header("レーダー")]
    [SerializeField, InspectorName("マップ中心 (X/Z)")]
    private Vector2 radarWorldCenter = Vector2.zero;
    [SerializeField, InspectorName("マップ表示範囲 (幅/奥行き)")]
    private Vector2 radarWorldSize = new Vector2(100f, 100f);

    private BattleHudView hudView;

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
        EnsureHudView();

        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        ConnectPartner();
    }

    private void OnEnable()
    {
        EnsureHudView();

        if (!Application.isPlaying)
        {
            hudView?.SetPreviewValues();
            return;
        }

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

        if (lockOnController != null)
        {
            lockOnController.OnTargetChanged += HandleTargetChanged;
            HandleTargetChanged(lockOnController.CurrentTarget);
        }

        if (battleManager != null)
        {
            battleManager.OnTimeChanged += UpdateTime;
            battleManager.OnTeamCostChanged += UpdateTeamCost;
        }
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdateMachineName();
        ConfigureWeaponList();
        ConnectPartner();

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
        if (!Application.isPlaying)
        {
            return;
        }

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

        if (lockOnController != null)
        {
            lockOnController.OnTargetChanged -= HandleTargetChanged;
        }

        if (battleManager != null)
        {
            battleManager.OnTimeChanged -= UpdateTime;
            battleManager.OnTeamCostChanged -= UpdateTeamCost;
        }

        DisconnectPartner();
        DisconnectTarget();
    }

    private void UpdateHealth(float current, float maximum)
    {
        if (healthText != null)
        {
            healthText.text = $"HP {Mathf.CeilToInt(current)}";
        }

        UpdateGauge(healthGauge, current, maximum);
    }

    private void UpdateMachineName()
    {
        if (hudView == null)
        {
            return;
        }

        MechDefinition definition = playerLoadout != null ? playerLoadout.Definition : null;
        hudView.MachineNameText.text = definition != null
            ? definition.MechName
            : "UNKNOWN";
    }

    private void UpdateBoost(float current, float maximum)
    {
        UpdateGauge(boostGauge, current, maximum);
    }

    private void UpdateAmmo(int current, int maximum)
    {
        if (hudView != null && playerShooter != null)
        {
            hudView.SetMainWeapon(GetWeaponName(GetDefinition()?.MainShot, "MAIN SHOT"),
                current, current > 0, IsConfigured(GetDefinition()?.MainShot));
            return;
        }

        if (ammoText != null)
        {
            ammoText.text = $"AMMO {current}";
        }
    }

    private void UpdateSubAmmo(int current, int maximum)
    {
        if (hudView != null && subWeapon != null)
        {
            hudView.SetSubWeapon(GetWeaponName(GetDefinition()?.SubShot, "SUB SHOT"),
                current, current > 0, IsConfigured(GetDefinition()?.SubShot));
            return;
        }

        if (subAmmoText != null)
        {
            subAmmoText.text = $"SUB {current}/{maximum}";
        }
    }

    private void UpdateSpecialShotAmmo(int current, int maximum)
    {
        if (hudView != null && specialShot != null)
        {
            hudView.SetSpecialWeapon(
                GetWeaponName(GetDefinition()?.SpecialShot, "SPECIAL SHOT"),
                current,
                current > 0,
                IsConfigured(GetDefinition()?.SpecialShot)
            );
            return;
        }

        if (specialShotAmmoText != null)
        {
            specialShotAmmoText.text = $"SPECIAL {current}/{maximum}";
        }
    }

    private void UpdateCharge(float chargeRate)
    {
        if (hudView != null)
        {
            hudView.SetCharge(chargeRate, IsConfigured(GetDefinition()?.ChargeShot));
            return;
        }

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
            targetText.text = hudView != null
                ? remainingCost.ToString()
                : $"COST {remainingCost}";
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
        EnsureHudView();
        GameObject player = FindPlayerMech();

        if (player != null)
        {
            playerLoadout ??= player.GetComponent<MechLoadoutController>();
            playerHealth ??= player.GetComponent<MechHealth>();
            playerMovement ??= player.GetComponent<PlayerMechController>();
            playerShooter ??= player.GetComponent<PlayerShooter>();
            subWeapon ??= player.GetComponent<SubWeaponController>();
            specialShot ??= player.GetComponent<SpecialShotController>();
            chargeShot ??= player.GetComponent<ChargeShotController>();
            awakeningController ??= player.GetComponent<AwakeningController>();
            lockOnController ??= player.GetComponent<LockOnController>();
        }

        battleManager ??= BattleManager.GetOrCreate();
        healthText = hudView != null
            ? hudView.HealthText
            : FindNamedComponent<TMP_Text>(transform, "HpText");
        healthGauge = hudView != null ? hudView.HealthGauge : healthGauge;
        ammoText = hudView != null
            ? hudView.MainAmmoText
            : FindNamedComponent<TMP_Text>(transform, "AmmoText");
        subAmmoText = hudView != null ? hudView.SubAmmoText : subAmmoText;
        specialShotAmmoText = hudView != null
            ? hudView.SpecialAmmoText
            : specialShotAmmoText;
        chargeGauge = hudView != null ? hudView.ChargeGauge : chargeGauge;
        awakeningGauge = hudView != null ? hudView.AwakeningGauge : awakeningGauge;
        boostGauge = hudView != null
            ? hudView.BoostGauge
            : FindNamedComponent<Slider>(transform, "BoostGauge");
        timerText = hudView != null ? hudView.TimerText : timerText;
        playerCostText = hudView != null ? hudView.PlayerCostText : playerCostText;
        enemyCostText = hudView != null ? hudView.EnemyCostText : enemyCostText;
    }

    private void UpdatePartnerHealth(float current, float maximum)
    {
        hudView?.SetPartner(
            partnerParticipant != null ? partnerParticipant.DisplayName : "PARTNER",
            current,
            maximum,
            partnerParticipant != null
        );
    }

    private void EnsureHudView()
    {
        if (hudView == null)
        {
            hudView = BattleHudView.Ensure(transform);
        }
    }

    private void ConnectPartner()
    {
        GameObject player = FindPlayerMech();
        BattleParticipant playerParticipant = player != null
            ? player.GetComponent<BattleParticipant>()
            : null;
        BattleParticipant foundPartner = null;

        if (playerParticipant != null)
        {
            foreach (BattleParticipant participant in BattleParticipant.AllParticipants)
            {
                if (participant != null
                    && participant != playerParticipant
                    && participant.Team == playerParticipant.Team)
                {
                    foundPartner = participant;
                    break;
                }
            }
        }

        if (foundPartner != partnerParticipant)
        {
            DisconnectPartner();
            partnerParticipant = foundPartner;
            partnerHealth = partnerParticipant != null
                ? partnerParticipant.GetComponent<MechHealth>()
                : null;
        }

        if (partnerHealth != null && !partnerHealthSubscribed)
        {
            partnerHealth.OnHealthChanged += UpdatePartnerHealth;
            partnerHealthSubscribed = true;
        }

        if (partnerHealth != null)
        {
            UpdatePartnerHealth(partnerHealth.CurrentHealth, partnerHealth.MaxHealth);
        }
        else
        {
            hudView?.SetPartner(string.Empty, 0f, 1f, false);
        }
    }

    private void DisconnectPartner()
    {
        if (partnerHealth != null && partnerHealthSubscribed)
        {
            partnerHealth.OnHealthChanged -= UpdatePartnerHealth;
        }

        partnerHealthSubscribed = false;
        partnerHealth = null;
    }

    private void HandleTargetChanged(Transform target)
    {
        DisconnectTarget();

        if (target == null)
        {
            hudView?.SetTarget(string.Empty, 0f, 1f, false);
            return;
        }

        targetParticipant = target.GetComponentInParent<BattleParticipant>();
        targetHealth = target.GetComponentInParent<MechHealth>();

        if (targetHealth == null)
        {
            hudView?.SetTarget(string.Empty, 0f, 1f, false);
            return;
        }

        targetHealth.OnHealthChanged += UpdateTargetHealth;
        UpdateTargetHealth(targetHealth.CurrentHealth, targetHealth.MaxHealth);
    }

    private void UpdateTargetHealth(float current, float maximum)
    {
        string targetName = targetParticipant != null
            ? targetParticipant.DisplayName
            : "ENEMY";
        hudView?.SetTarget(targetName, current, maximum, true);
    }

    private void DisconnectTarget()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= UpdateTargetHealth;
        }

        targetHealth = null;
        targetParticipant = null;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (partnerParticipant == null)
            {
                ConnectPartner();
            }

            UpdateWeaponAmmoStates();
            UpdateRadar();
        }
    }

    private void UpdateRadar()
    {
        BattleParticipant player = playerHealth != null
            ? playerHealth.GetComponent<BattleParticipant>()
            : null;
        hudView?.UpdateRadar(
            player,
            BattleParticipant.AllParticipants,
            radarWorldCenter,
            radarWorldSize
        );
    }

    private void ConfigureWeaponList()
    {
        UpdateWeaponAmmoStates();
        hudView?.SetCharge(
            chargeShot != null ? chargeShot.ChargeRate : 0f,
            IsConfigured(GetDefinition()?.ChargeShot)
        );
    }

    private void UpdateWeaponAmmoStates()
    {
        if (hudView == null)
        {
            return;
        }

        MechDefinition definition = GetDefinition();
        hudView.SetMainWeapon(GetWeaponName(definition?.MainShot, "MAIN SHOT"),
            playerShooter != null ? playerShooter.CurrentAmmo : 0,
            playerShooter != null && playerShooter.CurrentAmmo > 0,
            IsConfigured(definition?.MainShot));
        hudView.SetSubWeapon(GetWeaponName(definition?.SubShot, "SUB SHOT"),
            subWeapon != null ? subWeapon.CurrentAmmo : 0,
            subWeapon != null && subWeapon.CurrentAmmo > 0,
            IsConfigured(definition?.SubShot));
        hudView.SetSpecialWeapon(GetWeaponName(definition?.SpecialShot, "SPECIAL SHOT"),
            specialShot != null ? specialShot.CurrentAmmo : 0,
            specialShot != null && specialShot.CurrentAmmo > 0,
            IsConfigured(definition?.SpecialShot));
    }

    private MechDefinition GetDefinition()
    {
        return playerLoadout != null ? playerLoadout.Definition : null;
    }

    private bool IsConfigured(RangedWeaponDefinition definition)
    {
        return definition != null || playerLoadout == null;
    }

    private static string GetWeaponName(
        RangedWeaponDefinition definition,
        string fallbackName)
    {
        return definition != null ? definition.WeaponName : fallbackName;
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

    private void OnValidate()
    {
        radarWorldSize.x = Mathf.Max(1f, radarWorldSize.x);
        radarWorldSize.y = Mathf.Max(1f, radarWorldSize.y);
    }
}
