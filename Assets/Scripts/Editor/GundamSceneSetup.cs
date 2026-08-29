using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GundamSceneSetup
{
    private const string GundamPrefabPath = "Assets/Prefabs/Mechs/Gundam.prefab";

    static GundamSceneSetup()
    {
        EditorApplication.delayCall += SetupAutomaticallyIfNeeded;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    [MenuItem("EXVS/機体/現在のシーンをガンダム用に設定")]
    public static void SetupCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("設定対象のシーンが開かれていません。");
            return;
        }

        GameObject playerMech = FindInScene(scene, "PlayerMech");
        GameObject enemyMech = FindInScene(scene, "EnemyMech");
        GameObject gundam = FindInScene(scene, "Gundam");

        if (gundam == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GundamPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("Gundamプレハブが見つかりません: " + GundamPrefabPath);
                return;
            }

            gundam = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            Undo.RegisterCreatedObjectUndo(gundam, "ガンダムをシーンへ配置");
            gundam.name = "Gundam";
            SetSpawnTransform(gundam.transform, playerMech);
        }

        DisableOldPlayerObjects(scene, playerMech);
        ConfigureEnemy(enemyMech);
        ConfigureCamera(scene, gundam, enemyMech);
        ConfigureLockOn(gundam);
        ConfigureBattleParticipant(gundam);
        ConfigureBattleHud(scene, gundam);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = gundam;
        EditorGUIUtility.PingObject(gundam);
        Debug.Log("現在のシーンをガンダムの動作確認用に設定しました。");
    }

    private static void SetupAutomaticallyIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid()
            || !scene.isLoaded
            || string.IsNullOrEmpty(scene.path))
        {
            return;
        }

        GameObject gundam = FindInScene(scene, "Gundam");
        GameObject enemyMech = FindInScene(scene, "EnemyMech");
        bool cameraSetupIsMissing = gundam != null
            && FindComponentInScene<VersusLockOnCamera>(scene) == null;
        bool enemySetupIsMissing = enemyMech != null
            && enemyMech.GetComponent<BattleParticipant>() == null;

        if ((gundam != null && !cameraSetupIsMissing && !enemySetupIsMissing)
            || (gundam == null && FindInScene(scene, "PlayerMech") == null))
        {
            return;
        }

        SetupCurrentScene();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += SetupAutomaticallyIfNeeded;
        }
    }

    private static void SetSpawnTransform(Transform gundam, GameObject playerMech)
    {
        Vector3 position = playerMech != null
            ? playerMech.transform.position
            : Vector3.zero;

        // Gundamプレハブは足元がY=0になる構造なので、地面の高さへ合わせる。
        gundam.position = new Vector3(position.x, 0f, position.z);
        gundam.rotation = playerMech != null
            ? playerMech.transform.rotation
            : Quaternion.identity;
        gundam.localScale = Vector3.one;
    }

    private static void DisableOldPlayerObjects(Scene scene, GameObject playerMech)
    {
        if (playerMech != null && playerMech.activeSelf)
        {
            Undo.RecordObject(playerMech, "既存プレイヤー機を無効化");
            playerMech.SetActive(false);
            EditorUtility.SetDirty(playerMech);
        }

        GameObject lockOnManager = FindInScene(scene, "LockOnManager");

        if (lockOnManager != null && lockOnManager.activeSelf)
        {
            Undo.RecordObject(lockOnManager, "既存ロックオン管理を無効化");
            lockOnManager.SetActive(false);
            EditorUtility.SetDirty(lockOnManager);
        }
    }

    private static void ConfigureCamera(Scene scene, GameObject gundam, GameObject enemyMech)
    {
        VersusLockOnCamera camera = FindComponentInScene<VersusLockOnCamera>(scene);

        if (camera == null)
        {
            Camera mainCamera = FindComponentInScene<Camera>(scene);

            if (mainCamera == null)
            {
                Debug.LogWarning("Main Cameraが見つからないため、カメラ参照は設定していません。");
                return;
            }

            camera = Undo.AddComponent<VersusLockOnCamera>(mainCamera.gameObject);
        }

        Undo.RecordObject(camera, "カメラをガンダムへ設定");
        camera.SetAttachTarget(gundam.transform);
        camera.ChangeLookTarget(enemyMech != null ? enemyMech.transform : null);
        EditorUtility.SetDirty(camera);

        LockOnController lockOn = gundam.GetComponent<LockOnController>();

        if (lockOn != null)
        {
            SetObjectReference(lockOn, "lockOnCamera", camera);
        }
    }

    private static void ConfigureLockOn(GameObject gundam)
    {
        LockOnController lockOn = gundam.GetComponent<LockOnController>();
        BattleParticipant participant = gundam.GetComponent<BattleParticipant>();
        PlayerShooter shooter = gundam.GetComponent<PlayerShooter>();

        if (lockOn == null)
        {
            return;
        }

        SetObjectReference(lockOn, "player", gundam.transform);
        SetObjectReference(lockOn, "playerParticipant", participant);
        SetObjectReference(lockOn, "playerShooter", shooter);
    }

    private static void ConfigureBattleParticipant(GameObject gundam)
    {
        BattleParticipant participant = gundam.GetComponent<BattleParticipant>();
        BattleManager battleManager = Object.FindFirstObjectByType<BattleManager>(
            FindObjectsInactive.Include
        );

        if (participant != null && battleManager != null)
        {
            SetObjectReference(participant, "battleManager", battleManager);
        }
    }

    private static void ConfigureEnemy(GameObject enemyMech)
    {
        if (enemyMech == null)
        {
            return;
        }

        MechHealth health = enemyMech.GetComponent<MechHealth>();

        if (health == null)
        {
            health = Undo.AddComponent<MechHealth>(enemyMech);
        }

        BattleParticipant participant = enemyMech.GetComponent<BattleParticipant>();

        if (participant == null)
        {
            participant = Undo.AddComponent<BattleParticipant>(enemyMech);
        }

        SerializedObject serialized = new SerializedObject(participant);
        SerializedProperty team = serialized.FindProperty("team");
        SerializedProperty healthReference = serialized.FindProperty("health");
        SerializedProperty battleManagerReference = serialized.FindProperty("battleManager");
        team.enumValueIndex = (int)BattleTeam.Enemy;
        healthReference.objectReferenceValue = health;
        battleManagerReference.objectReferenceValue = Object.FindFirstObjectByType<BattleManager>(
            FindObjectsInactive.Include
        );
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(participant);
    }

    private static void ConfigureBattleHud(Scene scene, GameObject gundam)
    {
        BattleHudController hud = FindComponentInScene<BattleHudController>(scene);

        if (hud == null)
        {
            return;
        }

        SetObjectReference(hud, "playerHealth", gundam.GetComponent<MechHealth>());
        SetObjectReference(hud, "playerMovement", gundam.GetComponent<PlayerMechController>());
        SetObjectReference(hud, "playerShooter", gundam.GetComponent<PlayerShooter>());
        SetObjectReference(hud, "subWeapon", gundam.GetComponent<SubWeaponController>());
        SetObjectReference(hud, "specialShot", gundam.GetComponent<SpecialShotController>());
        SetObjectReference(hud, "chargeShot", gundam.GetComponent<ChargeShotController>());
        SetObjectReference(hud, "awakeningController", gundam.GetComponent<AwakeningController>());

        BattleManager battleManager = Object.FindFirstObjectByType<BattleManager>(
            FindObjectsInactive.Include
        );
        SetObjectReference(hud, "battleManager", battleManager);
    }

    private static void SetObjectReference(
        Object target,
        string propertyName,
        Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            Debug.LogWarning(propertyName + "を設定できませんでした。", target);
            return;
        }

        Undo.RecordObject(target, "ガンダムの参照を設定");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);

            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindTransform(root.transform, objectName);

            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindTransform(Transform current, string objectName)
    {
        if (current.name == objectName)
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindTransform(current.GetChild(i), objectName);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
