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

    private void Start()
    {
        ConnectPlayerCamera();
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

    private void ConnectPlayerCamera()
    {
        BattleParticipant participant = GetComponent<BattleParticipant>();

        if (participant == null || participant.Team != BattleTeam.Player || Camera.main == null)
        {
            return;
        }

        VersusLockOnCamera camera = Camera.main.GetComponent<VersusLockOnCamera>();

        if (camera == null)
        {
            camera = Camera.main.gameObject.AddComponent<VersusLockOnCamera>();
        }

        LockOnController lockOn = GetComponent<LockOnController>();
        camera.SetAttachTarget(transform);
        camera.ChangeLookTarget(lockOn != null ? lockOn.CurrentTarget : null);
        lockOn?.SetLockOnCamera(camera);
    }
}
