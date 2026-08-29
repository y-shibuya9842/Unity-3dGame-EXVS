using UnityEngine;

[CreateAssetMenu(
    fileName = "MeleeWeaponDefinition",
    menuName = "EXVS/武装/格闘武装設定"
)]
public class MeleeWeaponDefinition : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string weaponName = "New Melee Weapon";
    [SerializeField] private float damage = 100f;
    [SerializeField] private float attackRange = 4f;
    [SerializeField, Range(1f, 180f)] private float hitAngle = 70f;
    [SerializeField] private float startupTime = 0.15f;
    [SerializeField] private float recoveryTime = 0.35f;

    [Header("命中効果")]
    [SerializeField] private float hitStunDuration = 0.4f;
    [SerializeField] private float downValue = 35f;
    [SerializeField] private float knockbackSpeed = 4f;

    [Header("突進")]
    [SerializeField] private float rushSpeed = 24f;
    [SerializeField] private float rushDuration = 0.45f;
    [SerializeField] private float boostCost = 20f;

    [Header("コンボ")]
    [SerializeField] private int maxComboCount = 3;
    [SerializeField] private float comboInputWindow = 0.35f;
    [SerializeField] private float comboDamageMultiplier = 0.85f;
    [SerializeField] private float comboDownValueMultiplier = 1.15f;

    [Header("アニメーション")]
    [SerializeField] private string neutralAnimationTrigger = "Melee";
    [SerializeField] private string forwardAnimationTrigger = "ForwardMelee";
    [SerializeField] private string backwardAnimationTrigger = "BackwardMelee";
    [SerializeField] private string sideAnimationTrigger = "SideMelee";
    [SerializeField] private string specialAnimationTrigger = "SpecialMelee";

    public string WeaponName => weaponName;
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float HitAngle => hitAngle;
    public float StartupTime => startupTime;
    public float RecoveryTime => recoveryTime;
    public float HitStunDuration => hitStunDuration;
    public float DownValue => downValue;
    public float KnockbackSpeed => knockbackSpeed;
    public float RushSpeed => rushSpeed;
    public float RushDuration => rushDuration;
    public float BoostCost => boostCost;
    public int MaxComboCount => maxComboCount;
    public float ComboInputWindow => comboInputWindow;
    public float ComboDamageMultiplier => comboDamageMultiplier;
    public float ComboDownValueMultiplier => comboDownValueMultiplier;
    public string NeutralAnimationTrigger => neutralAnimationTrigger;
    public string ForwardAnimationTrigger => forwardAnimationTrigger;
    public string BackwardAnimationTrigger => backwardAnimationTrigger;
    public string SideAnimationTrigger => sideAnimationTrigger;
    public string SpecialAnimationTrigger => specialAnimationTrigger;

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        attackRange = Mathf.Max(0.1f, attackRange);
        startupTime = Mathf.Max(0f, startupTime);
        recoveryTime = Mathf.Max(0f, recoveryTime);
        hitStunDuration = Mathf.Max(0f, hitStunDuration);
        downValue = Mathf.Max(0f, downValue);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
        rushSpeed = Mathf.Max(0f, rushSpeed);
        rushDuration = Mathf.Max(0.01f, rushDuration);
        boostCost = Mathf.Max(0f, boostCost);
        maxComboCount = Mathf.Max(1, maxComboCount);
        comboInputWindow = Mathf.Max(0.01f, comboInputWindow);
        comboDamageMultiplier = Mathf.Max(0f, comboDamageMultiplier);
        comboDownValueMultiplier = Mathf.Max(0f, comboDownValueMultiplier);
    }
}
