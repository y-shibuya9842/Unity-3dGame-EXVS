using UnityEngine;

[DefaultExecutionOrder(-500)]
public class TemporaryPartnerSpawner : MonoBehaviour
{
    [SerializeField] private Vector3 spawnOffset = new Vector3(5f, 0f, -3f);
    [SerializeField] private Color partnerColor = new Color(0.15f, 0.8f, 1f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureSpawnerExists()
    {
        BattleManager battleManager = BattleManager.GetOrCreate();

        if (battleManager.GetComponent<TemporaryPartnerSpawner>() == null)
        {
            battleManager.gameObject.AddComponent<TemporaryPartnerSpawner>();
        }
    }

    private void Start()
    {
        if (GetComponent<BattleRosterSpawner>() != null)
        {
            return;
        }

        SpawnPartnerIfNeeded();
    }

    private void SpawnPartnerIfNeeded()
    {
        BattleParticipant player = FindParticipant(BattleTeam.Player, true);

        if (player == null || FindPartner(player) != null)
        {
            return;
        }

        BattleParticipant sourceEnemy = FindParticipant(BattleTeam.Enemy, false);

        if (sourceEnemy == null)
        {
            Debug.LogWarning("仮僚機の複製元となる敵機が見つかりません。", this);
            return;
        }

        Vector3 position = player.transform.TransformPoint(spawnOffset);
        GameObject partnerObject = Instantiate(
            sourceEnemy.gameObject,
            position,
            player.transform.rotation
        );
        partnerObject.name = "PartnerMech";

        BattleParticipant partner = partnerObject.GetComponent<BattleParticipant>();
        partner.SetTeam(BattleTeam.Player);
        partner.SetDisplayName("仮僚機");
        DisableCombatControl(partnerObject);
        RecolorModel(partnerObject);

        MechHealth health = partnerObject.GetComponent<MechHealth>();
        health?.ResetHealth();
    }

    private static BattleParticipant FindParticipant(BattleTeam team, bool preferLoadout)
    {
        BattleParticipant fallback = null;

        foreach (BattleParticipant participant in BattleParticipant.AllParticipants)
        {
            if (participant == null || participant.Team != team)
            {
                continue;
            }

            if (preferLoadout && participant.GetComponent<MechLoadoutController>() != null)
            {
                return participant;
            }

            fallback ??= participant;
        }

        return fallback;
    }

    private static BattleParticipant FindPartner(BattleParticipant player)
    {
        foreach (BattleParticipant participant in BattleParticipant.AllParticipants)
        {
            if (participant != null
                && participant != player
                && participant.Team == player.Team)
            {
                return participant;
            }
        }

        return null;
    }

    private static void DisableCombatControl(GameObject partnerObject)
    {
        SetEnabled<EnemyCombatAI>(partnerObject, false);
        SetEnabled<PlayerMechController>(partnerObject, false);
        SetEnabled<PlayerShooter>(partnerObject, false);
        SetEnabled<ChargeShotController>(partnerObject, false);
        SetEnabled<SubWeaponController>(partnerObject, false);
        SetEnabled<SpecialShotController>(partnerObject, false);
        SetEnabled<MeleeAttackController>(partnerObject, false);
        SetEnabled<SpecialMeleeController>(partnerObject, false);
        SetEnabled<LockOnController>(partnerObject, false);

        Rigidbody body = partnerObject.GetComponent<Rigidbody>();

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private static void SetEnabled<T>(GameObject target, bool enabled)
        where T : Behaviour
    {
        T behaviour = target.GetComponent<T>();

        if (behaviour != null)
        {
            behaviour.enabled = enabled;
        }
    }

    private void RecolorModel(GameObject partnerObject)
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (Renderer renderer in partnerObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", partnerColor);
            properties.SetColor("_Color", partnerColor);
            renderer.SetPropertyBlock(properties);
        }
    }
}
