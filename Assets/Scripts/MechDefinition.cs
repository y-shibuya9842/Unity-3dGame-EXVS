using UnityEngine;

[CreateAssetMenu(
    fileName = "MechDefinition",
    menuName = "EXVS/機体/機体設定"
)]
public class MechDefinition : ScriptableObject
{
    [Header("基本情報")]
    [SerializeField] private string mechName = "New Mech";
    [SerializeField] private string pilotName = "New Pilot";
    [SerializeField] private int unitCost = 2000;
    [SerializeField] private float maxHealth = 600f;
    [SerializeField] private int boostDashCount = 6;
    [SerializeField] private float redLockDistance = 30f;
    [SerializeField] private bool supportsTransformation;

    [Header("現在使用する射撃武装")]
    [SerializeField] private RangedWeaponDefinition mainShot;
    [SerializeField] private RangedWeaponDefinition chargeShot;
    [SerializeField] private RangedWeaponDefinition subShot;
    [SerializeField] private RangedWeaponDefinition specialShot;
    [SerializeField] private RangedWeaponDefinition burstAttack;

    [Header("現在使用する格闘武装")]
    [SerializeField] private MeleeWeaponDefinition melee;
    [SerializeField] private MeleeWeaponDefinition specialMelee;

    [Header("方向入力による派生武装")]
    [SerializeField] private RangedWeaponDefinition directionalChargeShot;
    [SerializeField] private RangedWeaponDefinition directionalSubShot;
    [SerializeField] private RangedWeaponDefinition directionalSpecialShot;
    [SerializeField] private MeleeWeaponDefinition forwardSpecialMelee;
    [SerializeField] private MeleeWeaponDefinition sideSpecialMelee;
    [SerializeField] private MeleeWeaponDefinition backwardSpecialMelee;

    public string MechName => mechName;
    public string PilotName => pilotName;
    public int UnitCost => unitCost;
    public float MaxHealth => maxHealth;
    public int BoostDashCount => boostDashCount;
    public float RedLockDistance => redLockDistance;
    public bool SupportsTransformation => supportsTransformation;
    public RangedWeaponDefinition MainShot => mainShot;
    public RangedWeaponDefinition ChargeShot => chargeShot;
    public RangedWeaponDefinition SubShot => subShot;
    public RangedWeaponDefinition SpecialShot => specialShot;
    public RangedWeaponDefinition BurstAttack => burstAttack;
    public MeleeWeaponDefinition Melee => melee;
    public MeleeWeaponDefinition SpecialMelee => specialMelee;
    public RangedWeaponDefinition DirectionalChargeShot => directionalChargeShot;
    public RangedWeaponDefinition DirectionalSubShot => directionalSubShot;
    public RangedWeaponDefinition DirectionalSpecialShot => directionalSpecialShot;
    public MeleeWeaponDefinition ForwardSpecialMelee => forwardSpecialMelee;
    public MeleeWeaponDefinition SideSpecialMelee => sideSpecialMelee;
    public MeleeWeaponDefinition BackwardSpecialMelee => backwardSpecialMelee;

    private void OnValidate()
    {
        unitCost = Mathf.Max(1, unitCost);
        maxHealth = Mathf.Max(1f, maxHealth);
        boostDashCount = Mathf.Max(1, boostDashCount);
        redLockDistance = Mathf.Max(0f, redLockDistance);
    }
}
