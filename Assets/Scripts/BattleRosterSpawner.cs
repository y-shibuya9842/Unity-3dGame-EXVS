using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-600)]
public class BattleRosterSpawner : MonoBehaviour
{
    private const string BattleSceneName = "SampleScene";

    [Header("出撃位置")]
    [SerializeField] private Vector3 partnerOffset = new Vector3(-6f, 0f, -4f);
    [SerializeField] private Vector3 enemyOneOffset = new Vector3(-6f, 0f, 18f);
    [SerializeField] private Vector3 enemyTwoOffset = new Vector3(6f, 0f, 18f);

    [Header("識別色")]
    [SerializeField] private Color partnerColor = new Color(0.15f, 0.8f, 1f, 1f);
    [SerializeField] private Color enemyOneColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color enemyTwoColor = new Color(1f, 0.25f, 0.65f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != BattleSceneName)
        {
            return;
        }

        BattleManager manager = BattleManager.GetOrCreate();

        if (manager.GetComponent<BattleRosterSpawner>() == null)
        {
            manager.gameObject.AddComponent<BattleRosterSpawner>();
        }
    }

    private void Start()
    {
        BuildRoster();
    }

    private void BuildRoster()
    {
        BattleParticipant player = FindPlayer();

        if (player == null)
        {
            Debug.LogWarning("4機編成の複製元となる自機が見つかりません。", this);
            return;
        }

        DisableLegacyEnemy(player);
        ConfigurePlayer(player);

        BattleParticipant partner = SpawnUnit(
            player,
            "PartnerMech",
            MatchMechSlot.Partner,
            BattleTeam.Player,
            partnerOffset,
            partnerColor,
            false
        );
        BattleParticipant enemyOne = SpawnUnit(
            player,
            "EnemyMech1",
            MatchMechSlot.EnemyOne,
            BattleTeam.Enemy,
            enemyOneOffset,
            enemyOneColor,
            true
        );
        SpawnUnit(
            player,
            "EnemyMech2",
            MatchMechSlot.EnemyTwo,
            BattleTeam.Enemy,
            enemyTwoOffset,
            enemyTwoColor,
            true
        );

        if (partner == null || enemyOne == null)
        {
            Debug.LogWarning("4機編成の生成が完了しませんでした。", this);
            return;
        }

        SetInitialTarget(player, enemyOne);
    }

    private static BattleParticipant FindPlayer()
    {
        BattleParticipant fallback = null;
        BattleParticipant[] participants = FindObjectsByType<BattleParticipant>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (BattleParticipant participant in participants)
        {
            if (participant.Team != BattleTeam.Player)
            {
                continue;
            }

            if (participant.GetComponent<MechLoadoutController>() != null)
            {
                return participant;
            }

            fallback ??= participant;
        }

        return fallback;
    }

    private static void ConfigurePlayer(BattleParticipant player)
    {
        player.gameObject.name = "PlayerMech";
        player.SetTeam(BattleTeam.Player);
        player.SetDisplayName(GetDisplayName(MatchMechSlot.Player));
        player.GetComponent<MechHealth>()?.ResetHealth();
    }

    private BattleParticipant SpawnUnit(
        BattleParticipant source,
        string objectName,
        MatchMechSlot slot,
        BattleTeam team,
        Vector3 offset,
        Color color,
        bool useTrainingEnemyController)
    {
        Vector3 position = source.transform.TransformPoint(offset);
        Quaternion rotation = GetSpawnRotation(position, source.transform.position, team);
        GameObject unitObject = Instantiate(source.gameObject, position, rotation);
        unitObject.name = objectName;

        BattleParticipant participant = unitObject.GetComponent<BattleParticipant>();

        if (participant == null)
        {
            Destroy(unitObject);
            return null;
        }

        participant.SetTeam(team);
        participant.SetDisplayName(GetDisplayName(slot));
        DisablePlayerControl(unitObject, useTrainingEnemyController);
        RecolorModel(unitObject, color);
        ResetPhysics(unitObject);
        unitObject.GetComponent<MechHealth>()?.ResetHealth();

        if (useTrainingEnemyController)
        {
            unitObject.AddComponent<TrainingEnemyController>();
        }

        return participant;
    }

    private static Quaternion GetSpawnRotation(
        Vector3 spawnPosition,
        Vector3 playerPosition,
        BattleTeam team)
    {
        if (team == BattleTeam.Player)
        {
            return Quaternion.identity;
        }

        Vector3 direction = playerPosition - spawnPosition;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : Quaternion.identity;
    }

    private static void DisablePlayerControl(
        GameObject unitObject,
        bool keepShieldForAutomaticGuard)
    {
        PlayerShooter shooter = unitObject.GetComponent<PlayerShooter>();
        shooter?.SetPlayerInputEnabled(false);

        ShieldGuard shield = unitObject.GetComponent<ShieldGuard>();
        shield?.SetPlayerInputEnabled(false);

        SetEnabled<MechLoadoutController>(unitObject, false);
        SetEnabled<PlayerMechController>(unitObject, false);
        SetEnabled<PlayerShooter>(unitObject, false);
        SetEnabled<ChargeShotController>(unitObject, false);
        SetEnabled<SubWeaponController>(unitObject, false);
        SetEnabled<SpecialShotController>(unitObject, false);
        SetEnabled<MeleeAttackController>(unitObject, false);
        SetEnabled<SpecialMeleeController>(unitObject, false);
        SetEnabled<LockOnController>(unitObject, false);
        SetEnabled<AwakeningController>(unitObject, false);
        SetEnabled<AwakeningBurstAttackController>(unitObject, false);
        SetEnabled<TransformationController>(unitObject, false);
        SetEnabled<EnemyCombatAI>(unitObject, false);

        if (!keepShieldForAutomaticGuard)
        {
            SetEnabled<ShieldGuard>(unitObject, false);
        }
    }

    private static void DisableLegacyEnemy(BattleParticipant player)
    {
        BattleParticipant[] participants = FindObjectsByType<BattleParticipant>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (BattleParticipant participant in participants)
        {
            if (participant != player && participant.Team == BattleTeam.Enemy)
            {
                participant.gameObject.SetActive(false);
            }
        }
    }

    private static void SetInitialTarget(
        BattleParticipant player,
        BattleParticipant enemy)
    {
        LockOnController lockOn = player.GetComponent<LockOnController>();
        lockOn?.SetTarget(enemy.transform);

        if (Camera.main == null)
        {
            return;
        }

        VersusLockOnCamera camera = Camera.main.GetComponent<VersusLockOnCamera>();

        if (camera != null)
        {
            camera.SetAttachTarget(player.transform);
            camera.ChangeLookTarget(enemy.transform);
        }
    }

    private static void ResetPhysics(GameObject unitObject)
    {
        Rigidbody body = unitObject.GetComponent<Rigidbody>();

        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private static void RecolorModel(GameObject unitObject, Color color)
    {
        MaterialPropertyBlock properties = new MaterialPropertyBlock();

        foreach (Renderer renderer in unitObject.GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            renderer.SetPropertyBlock(properties);
        }
    }

    private static string GetDisplayName(MatchMechSlot slot)
    {
        string mechId = MatchSetupState.GetSelectedMech(slot);
        return mechId == MatchSetupState.DefaultMechId ? "ガンダム" : mechId;
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
}
