using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class MechLoadoutController : MonoBehaviour
{
    [Header("機体設定")]
    [SerializeField] private MechDefinition definition;

    public MechDefinition Definition => definition;

    private void Awake()
    {
        ApplyDefinition();
    }

    public void ApplyDefinition()
    {
        if (definition == null)
        {
            return;
        }

        GetComponent<MechHealth>()?.SetMaximumHealth(definition.MaxHealth);
        GetComponent<BattleParticipant>()?.SetUnitCost(definition.UnitCost);
        GetComponent<LockOnController>()?.SetRedLockDistance(definition.RedLockDistance);
        GetComponent<PlayerShooter>()?.SetWeaponDefinition(definition.MainShot);
        GetComponent<ChargeShotController>()?.SetWeaponDefinition(definition.ChargeShot);
        GetComponent<SubWeaponController>()?.SetWeaponDefinition(definition.SubShot);
        GetComponent<SpecialShotController>()?.SetWeaponDefinition(definition.SpecialShot);
        GetComponent<MeleeAttackController>()?.SetWeaponDefinition(definition.Melee);
        GetComponent<SpecialMeleeController>()?.SetWeaponDefinition(definition.SpecialMelee);
        GetComponent<AwakeningBurstAttackController>()?.SetWeaponDefinition(
            definition.BurstAttack
        );

        TransformationController transformation = GetComponent<TransformationController>();

        if (transformation != null)
        {
            transformation.enabled = definition.SupportsTransformation;
        }
    }
}
