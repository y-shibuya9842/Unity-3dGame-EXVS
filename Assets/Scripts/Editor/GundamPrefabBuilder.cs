using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GundamPrefabBuilder
{
    private const string DataRoot = "Assets/GameData/Mechs/Gundam";
    private const string WeaponRoot = DataRoot + "/Weapons";
    private const string ProjectileRoot = DataRoot + "/Projectiles";
    private const string PrefabRoot = "Assets/Prefabs/Mechs";
    private const string GundamPrefabPath = PrefabRoot + "/Gundam.prefab";
    private const string SourceProjectilePath = "Assets/Prefabs/Projectile.prefab";
    private const string ModelPrefabPath =
        "Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab";

    static GundamPrefabBuilder()
    {
        EditorApplication.delayCall += GenerateIfMissing;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("EXVS/機体/ガンダムを再生成")]
    public static void Regenerate()
    {
        Generate(true);
    }

    private static void GenerateIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GundamPrefabPath);
        bool needsModelCleanup = prefab != null
            && prefab.GetComponentInChildren<CharacterController>(true) != null;
        MeleeWeaponDefinition neutralSpecialMelee = AssetDatabase.LoadAssetAtPath<
            MeleeWeaponDefinition
        >(WeaponRoot + "/SpecialMelee_JavelinThrow.asset");
        bool needsSpecialMeleeUpdate = neutralSpecialMelee != null
            && neutralSpecialMelee.ProjectilePrefab == null;

        if (prefab == null || needsModelCleanup || needsSpecialMeleeUpdate)
        {
            Generate(needsModelCleanup);
        }
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += GenerateIfMissing;
        }
    }

    private static void Generate(bool overwritePrefab)
    {
        try
        {
            EnsureFolder(DataRoot);
            EnsureFolder(WeaponRoot);
            EnsureFolder(ProjectileRoot);
            EnsureFolder(PrefabRoot);

            WeaponAssets weapons = CreateWeaponAssets();
            MechDefinition definition = CreateMechDefinition(weapons);

            if (overwritePrefab || AssetDatabase.LoadAssetAtPath<GameObject>(GundamPrefabPath) == null)
            {
                CreateGundamPrefab(definition);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ガンダムの機体設定とプレハブを生成しました。");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static WeaponAssets CreateWeaponAssets()
    {
        HomingProjectile beam = CreateProjectile(
            "BeamRifleProjectile",
            0.22f,
            42f,
            8f,
            3f,
            75f,
            0.25f,
            20f,
            2f
        );
        HomingProjectile chargedBeam = CreateProjectile(
            "ChargedBeamProjectile",
            0.34f,
            50f,
            6f,
            3.5f,
            140f,
            0.4f,
            100f,
            6f
        );
        HomingProjectile napalm = CreateProjectile(
            "SuperNapalmProjectile",
            0.6f,
            28f,
            3f,
            4f,
            95f,
            0.7f,
            35f,
            2f
        );
        HomingProjectile bazooka = CreateProjectile(
            "HyperBazookaProjectile",
            0.42f,
            32f,
            5f,
            4f,
            110f,
            0.35f,
            100f,
            7f
        );
        HomingProjectile sideBazooka = CreateProjectile(
            "SideHyperBazookaProjectile",
            0.4f,
            30f,
            4f,
            4f,
            75f,
            0.3f,
            45f,
            5f
        );
        HomingProjectile supportShell = CreateProjectile(
            "SupportShellProjectile",
            0.3f,
            34f,
            7f,
            4f,
            13.5f,
            0.2f,
            10f,
            1.5f
        );
        HomingProjectile rushSupport = CreateProjectile(
            "RushSupportProjectile",
            0.45f,
            27f,
            10f,
            4f,
            134f,
            0.55f,
            70f,
            5f
        );
        HomingProjectile lastShooting = CreateProjectile(
            "LastShootingProjectile",
            0.5f,
            55f,
            8f,
            4f,
            317f,
            0.8f,
            100f,
            10f
        );
        HomingProjectile beamJavelin = CreateProjectile(
            "BeamJavelinProjectile",
            0.32f,
            34f,
            6f,
            3.5f,
            90f,
            0.45f,
            100f,
            5f
        );

        WeaponAssets assets = new WeaponAssets
        {
            Main = CreateRanged("Main_BeamRifle", "ビーム・ライフル", beam, 8, 3f,
                WeaponReloadMode.OneByOne, 0.25f, 0.35f, 0f, 1, 0f, 0f, 2.5f),
            Charge = CreateRanged("Charge_BeamRifleMaximum", "ビーム・ライフル【最大出力】",
                chargedBeam, 1, 1f, WeaponReloadMode.OneByOne, 0f, 0.7f, 0f, 1, 0f, 0f, 2.5f),
            DirectionalCharge = CreateRanged("Charge_SuperNapalm", "スーパー・ナパーム",
                napalm, 1, 1f, WeaponReloadMode.OneByOne, 0f, 0.8f, 0.15f, 1, 0f, 0f, 2.5f),
            Sub = CreateRanged("Sub_HyperBazooka", "ハイパー・バズーカ",
                bazooka, 2, 4.5f, WeaponReloadMode.FullMagazine, 0.45f, 0.5f, 0.12f, 1, 0f, 0f, 2f),
            DirectionalSub = CreateRanged("Sub_SideHyperBazooka", "ハイパー・バズーカ【移動撃ち】",
                sideBazooka, 2, 4.5f, WeaponReloadMode.FullMagazine, 0.5f, 0.3f, 0f, 2, 0.16f, 2f, 2f),
            SpecialShot = CreateRanged("SpecialShot_GuncannonGuntank", "ガンキャノン＆ガンタンク 呼出",
                supportShell, 2, 12f, WeaponReloadMode.FullMagazine, 0.8f, 0.8f, 0.25f, 10, 0.08f, 8f, 2f),
            DirectionalSpecialShot = CreateRanged("SpecialShot_RushSupport", "ガンキャノン＆ガンタンク 突撃",
                rushSupport, 2, 12f, WeaponReloadMode.FullMagazine, 0.8f, 0.8f, 0.2f, 1, 0f, 0f, 2f),
            Burst = CreateRanged("Burst_LastShooting", "ラストシューティング",
                lastShooting, 1, 1f, WeaponReloadMode.FullMagazine, 0f, 1.8f, 0.75f, 1, 0f, 0f, 2f),
            Melee = CreateMelee("Melee_BeamSaber", "ビーム・サーベル", 70f, 4f, 3, 0.81f, 24f, 0.45f),
            ForwardMelee = CreateMelee("Melee_ForwardThrust", "前格闘 突き", 80f, 4.5f, 1, 1f, 27f, 0.4f),
            SideMelee = CreateMelee("Melee_DualSaber", "横格闘 二刀流", 65f, 4f, 3, 0.9f, 25f, 0.45f),
            BackwardMelee = CreateMelee("Melee_Counter", "後格闘 格闘カウンター", 80f, 3f, 1, 1f, 0f, 0.2f),
            BoostDashMelee = CreateMelee("Melee_BoostDash", "BD格闘 三段斬り", 75f, 5f, 3, 0.84f, 30f, 0.5f),
            SpecialMelee = CreateMelee("SpecialMelee_GundamHammer", "前特殊格闘 ガンダム・ハンマー", 129f, 4f, 1, 1f, 26f, 0.45f),
            SideSpecialMelee = CreateMelee("SpecialMelee_HammerSpin", "横特殊格闘 ハンマー回転", 157f, 3.5f, 1, 1f, 20f, 0.4f),
            BackwardSpecialMelee = CreateMelee("SpecialMelee_JavelinStab", "後特殊格闘 ビーム・ジャベリン突き", 134f, 4.5f, 1, 1f, 24f, 0.45f),
            NeutralSpecialMelee = CreateMelee("SpecialMelee_JavelinThrow", "N特殊格闘 ビーム・ジャベリン投擲", 90f, 8f, 1, 1f, 0f, 0.2f, beamJavelin)
        };

        return assets;
    }

    private static MechDefinition CreateMechDefinition(WeaponAssets weapons)
    {
        MechDefinition definition = CreateOrLoad<MechDefinition>(DataRoot + "/Gundam.asset");
        SerializedObject serialized = new SerializedObject(definition);
        Set(serialized, "mechName", "ガンダム");
        Set(serialized, "pilotName", "アムロ・レイ");
        Set(serialized, "unitCost", 2000);
        Set(serialized, "maxHealth", 660f);
        Set(serialized, "boostDashCount", 6);
        Set(serialized, "redLockDistance", 30f);
        Set(serialized, "supportsTransformation", false);
        Set(serialized, "mainShot", weapons.Main);
        Set(serialized, "chargeShot", weapons.Charge);
        Set(serialized, "subShot", weapons.Sub);
        Set(serialized, "specialShot", weapons.SpecialShot);
        Set(serialized, "burstAttack", weapons.Burst);
        Set(serialized, "melee", weapons.Melee);
        Set(serialized, "specialMelee", weapons.NeutralSpecialMelee);
        Set(serialized, "forwardMelee", weapons.ForwardMelee);
        Set(serialized, "sideMelee", weapons.SideMelee);
        Set(serialized, "backwardMelee", weapons.BackwardMelee);
        Set(serialized, "boostDashMelee", weapons.BoostDashMelee);
        Set(serialized, "directionalChargeShot", weapons.DirectionalCharge);
        Set(serialized, "directionalSubShot", weapons.DirectionalSub);
        Set(serialized, "directionalSpecialShot", weapons.DirectionalSpecialShot);
        Set(serialized, "forwardSpecialMelee", weapons.SpecialMelee);
        Set(serialized, "sideSpecialMelee", weapons.SideSpecialMelee);
        Set(serialized, "backwardSpecialMelee", weapons.BackwardSpecialMelee);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void CreateGundamPrefab(MechDefinition definition)
    {
        GameObject root = new GameObject("Gundam");

        try
        {
            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 1f;
            rigidbody.angularDamping = 5f;
            rigidbody.useGravity = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.5f;

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            PlayerMechController movement = root.AddComponent<PlayerMechController>();
            MechHealth health = root.AddComponent<MechHealth>();
            BattleParticipant participant = root.AddComponent<BattleParticipant>();
            LockOnController lockOn = root.AddComponent<LockOnController>();
            PlayerShooter shooter = root.AddComponent<PlayerShooter>();
            ChargeShotController charge = root.AddComponent<ChargeShotController>();
            SubWeaponController sub = root.AddComponent<SubWeaponController>();
            SpecialShotController specialShot = root.AddComponent<SpecialShotController>();
            MeleeAttackController melee = root.AddComponent<MeleeAttackController>();
            SpecialMeleeController specialMelee = root.AddComponent<SpecialMeleeController>();
            HitReactionController hitReaction = root.AddComponent<HitReactionController>();
            ShieldGuard shield = root.AddComponent<ShieldGuard>();
            AwakeningController awakening = root.AddComponent<AwakeningController>();
            AwakeningBurstAttackController burst = root.AddComponent<AwakeningBurstAttackController>();
            TransformationController transformation = root.AddComponent<TransformationController>();
            MechLoadoutController loadout = root.AddComponent<MechLoadoutController>();

            Transform firePoint = CreateChild(root.transform, "FirePoint", new Vector3(0f, 1.45f, 0.75f));
            CreateChild(root.transform, "LockOnPoint", new Vector3(0f, 1.1f, 0f));
            Animator animator = AddPlaceholderModel(root.transform);

            SetReference(movement, "audioSource", audioSource);
            SetReference(participant, "health", health);
            SetReference(lockOn, "player", root.transform);
            SetReference(lockOn, "playerParticipant", participant);
            SetReference(lockOn, "playerShooter", shooter);
            SetReference(shooter, "rotationRoot", root.transform);
            SetReference(shooter, "firePoint", firePoint);
            SetReference(shooter, "movementController", movement);
            SetReference(shooter, "lockOnController", lockOn);
            SetReference(charge, "firePoint", firePoint);
            SetReference(charge, "mainShooter", shooter);
            SetReference(charge, "movementController", movement);
            SetReference(charge, "lockOnController", lockOn);
            SetReference(sub, "firePoint", firePoint);
            SetReference(sub, "movementController", movement);
            SetReference(sub, "lockOnController", lockOn);
            SetReference(specialShot, "rotationRoot", root.transform);
            SetReference(specialShot, "firePoint", firePoint);
            SetReference(specialShot, "movementController", movement);
            SetReference(specialShot, "lockOnController", lockOn);
            SetReference(specialShot, "animator", animator);
            SetReference(melee, "lockOnController", lockOn);
            SetReference(melee, "movementController", movement);
            SetReference(melee, "animator", animator);
            SetReference(specialMelee, "lockOnController", lockOn);
            SetReference(specialMelee, "movementController", movement);
            SetReference(specialMelee, "animator", animator);
            SetReference(hitReaction, "movementController", movement);
            SetReference(hitReaction, "animator", animator);
            SetReference(shield, "movementController", movement);
            SetReference(awakening, "health", health);
            SetReference(awakening, "movementController", movement);
            SetReference(awakening, "playerShooter", shooter);
            SetReference(awakening, "meleeController", melee);
            SetReference(burst, "awakeningController", awakening);
            SetReference(burst, "movementController", movement);
            SetReference(burst, "lockOnController", lockOn);
            SetReference(burst, "firePoint", firePoint);
            SetReference(burst, "animator", animator);
            SetReference(loadout, "definition", definition);
            SetReference(movement, "transformationController", transformation);
            transformation.enabled = false;

            PrefabUtility.SaveAsPrefabAsset(root, GundamPrefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Animator AddPlaceholderModel(Transform parent)
    {
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefabPath);

        if (modelPrefab == null)
        {
            Debug.LogWarning("Robot Kyleが見つからないため、Gundamプレハブは仮モデルなしで生成されます。");
            return null;
        }

        GameObject model = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
        PrefabUtility.UnpackPrefabInstance(
            model,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction
        );
        model.name = "Model";
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // 仮モデル側の移動・入力・当たり判定を外し、見た目とアニメーターだけを使用する。
        foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
        {
            UnityEngine.Object.DestroyImmediate(behaviour);
        }

        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        foreach (Rigidbody rigidbody in model.GetComponentsInChildren<Rigidbody>(true))
        {
            UnityEngine.Object.DestroyImmediate(rigidbody);
        }

        return model.GetComponentInChildren<Animator>();
    }

    private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child.transform;
    }

    private static HomingProjectile CreateProjectile(
        string assetName,
        float size,
        float speed,
        float homing,
        float lifetime,
        float damage,
        float stun,
        float down,
        float knockback)
    {
        string path = ProjectileRoot + "/" + assetName + ".prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null
            && !AssetDatabase.CopyAsset(SourceProjectilePath, path))
        {
            throw new InvalidOperationException("弾プレハブを複製できませんでした: " + path);
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            contents.name = assetName;
            HomingProjectile projectile = contents.GetComponent<HomingProjectile>();
            SerializedObject serialized = new SerializedObject(projectile);
            Set(serialized, "projectileSize", size);
            Set(serialized, "speed", speed);
            Set(serialized, "homingStrength", homing);
            Set(serialized, "lifetime", lifetime);
            Set(serialized, "damage", damage);
            Set(serialized, "hitStunDuration", stun);
            Set(serialized, "downValue", down);
            Set(serialized, "knockbackSpeed", knockback);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<HomingProjectile>();
    }

    private static RangedWeaponDefinition CreateRanged(
        string assetName,
        string weaponName,
        HomingProjectile projectile,
        int ammo,
        float reload,
        WeaponReloadMode reloadMode,
        float cooldown,
        float actionLock,
        float startup,
        int projectileCount,
        float projectileInterval,
        float spread,
        float chargeTime)
    {
        RangedWeaponDefinition definition = CreateOrLoad<RangedWeaponDefinition>(
            WeaponRoot + "/" + assetName + ".asset"
        );
        SerializedObject serialized = new SerializedObject(definition);
        Set(serialized, "weaponName", weaponName);
        Set(serialized, "projectilePrefab", projectile);
        Set(serialized, "maxAmmo", ammo);
        Set(serialized, "reloadTime", reload);
        Set(serialized, "reloadMode", (int)reloadMode);
        Set(serialized, "cooldown", cooldown);
        Set(serialized, "actionLockDuration", actionLock);
        Set(serialized, "startupTime", startup);
        Set(serialized, "projectileCount", projectileCount);
        Set(serialized, "projectileInterval", projectileInterval);
        Set(serialized, "spreadAngle", spread);
        Set(serialized, "chargeTime", chargeTime);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static MeleeWeaponDefinition CreateMelee(
        string assetName,
        string weaponName,
        float damage,
        float range,
        int comboCount,
        float comboMultiplier,
        float rushSpeed,
        float rushDuration,
        HomingProjectile projectilePrefab = null)
    {
        MeleeWeaponDefinition definition = CreateOrLoad<MeleeWeaponDefinition>(
            WeaponRoot + "/" + assetName + ".asset"
        );
        SerializedObject serialized = new SerializedObject(definition);
        Set(serialized, "weaponName", weaponName);
        Set(serialized, "damage", damage);
        Set(serialized, "attackRange", range);
        Set(serialized, "hitAngle", 70f);
        Set(serialized, "startupTime", 0.15f);
        Set(serialized, "recoveryTime", 0.35f);
        Set(serialized, "hitStunDuration", 0.4f);
        Set(serialized, "downValue", comboCount <= 1 ? 100f : 35f);
        Set(serialized, "knockbackSpeed", 4f);
        Set(serialized, "rushSpeed", rushSpeed);
        Set(serialized, "rushDuration", rushDuration);
        Set(serialized, "boostCost", 20f);
        Set(serialized, "projectilePrefab", projectilePrefab);
        Set(serialized, "maxComboCount", comboCount);
        Set(serialized, "comboInputWindow", 0.35f);
        Set(serialized, "comboDamageMultiplier", comboMultiplier);
        Set(serialized, "comboDownValueMultiplier", 1.15f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static T CreateOrLoad<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        Set(serialized, propertyName, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        serialized.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, string value)
    {
        serialized.FindProperty(propertyName).stringValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, int value)
    {
        serialized.FindProperty(propertyName).intValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, float value)
    {
        serialized.FindProperty(propertyName).floatValue = value;
    }

    private static void Set(SerializedObject serialized, string propertyName, bool value)
    {
        serialized.FindProperty(propertyName).boolValue = value;
    }

    private sealed class WeaponAssets
    {
        public RangedWeaponDefinition Main;
        public RangedWeaponDefinition Charge;
        public RangedWeaponDefinition DirectionalCharge;
        public RangedWeaponDefinition Sub;
        public RangedWeaponDefinition DirectionalSub;
        public RangedWeaponDefinition SpecialShot;
        public RangedWeaponDefinition DirectionalSpecialShot;
        public RangedWeaponDefinition Burst;
        public MeleeWeaponDefinition Melee;
        public MeleeWeaponDefinition ForwardMelee;
        public MeleeWeaponDefinition SideMelee;
        public MeleeWeaponDefinition BackwardMelee;
        public MeleeWeaponDefinition BoostDashMelee;
        public MeleeWeaponDefinition SpecialMelee;
        public MeleeWeaponDefinition SideSpecialMelee;
        public MeleeWeaponDefinition BackwardSpecialMelee;
        public MeleeWeaponDefinition NeutralSpecialMelee;
    }
}
