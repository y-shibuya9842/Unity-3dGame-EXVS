using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public enum VersusInputAction
{
    Move,
    Jump,
    MainShot,
    Melee,
    SubShot,
    SpecialShot,
    SpecialMelee,
    Guard,
    Search,
    Awakening,
    BurstAttack,
    OpenInputSettings,
    Retry
}

[DefaultExecutionOrder(-1000)]
public sealed class VersusInputManager : MonoBehaviour
{
    private const string BindingOverridesKey = "VersusInputBindingOverrides";
    private const string BindingDefaultsVersionKey = "VersusInputBindingDefaultsVersion";
    private const int CurrentBindingDefaultsVersion = 2;
    private static VersusInputManager instance;

    private readonly Dictionary<VersusInputAction, InputAction> actions =
        new Dictionary<VersusInputAction, InputAction>();
    private InputActionAsset inputAsset;
    private InputActionMap gameplayMap;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    public static VersusInputManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject("VersusInputManager");
                instance = managerObject.AddComponent<VersusInputManager>();
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeSceneLoad()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildActions();
        MigrateBindingDefaults();
        LoadBindingOverrides();
        inputAsset.Enable();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ConfigureEventSystem();
    }

    public Vector2 ReadMove()
    {
        return GetAction(VersusInputAction.Move).ReadValue<Vector2>();
    }

    public bool WasPressedThisFrame(VersusInputAction action)
    {
        bool subShotCombination = WasCombinationPressedThisFrame(
            VersusInputAction.MainShot,
            VersusInputAction.Melee
        );
        bool specialShotCombination = WasCombinationPressedThisFrame(
            VersusInputAction.MainShot,
            VersusInputAction.Jump
        );

        if (action == VersusInputAction.MainShot
            && (subShotCombination || specialShotCombination))
        {
            return false;
        }

        if (action == VersusInputAction.Melee && subShotCombination)
        {
            return false;
        }

        if (action == VersusInputAction.Jump && specialShotCombination)
        {
            return false;
        }

        return WasRawPressedThisFrame(action);
    }

    public bool WasReleasedThisFrame(VersusInputAction action)
    {
        return GetAction(action).WasReleasedThisFrame();
    }

    public bool IsPressed(VersusInputAction action)
    {
        return GetAction(action).IsPressed();
    }

    public bool WasSubShotTriggeredThisFrame()
    {
        return WasRawPressedThisFrame(VersusInputAction.SubShot)
            || WasCombinationPressedThisFrame(
                VersusInputAction.MainShot,
                VersusInputAction.Melee
            );
    }

    public bool WasSpecialShotTriggeredThisFrame()
    {
        return WasRawPressedThisFrame(VersusInputAction.SpecialShot)
            || WasCombinationPressedThisFrame(
                VersusInputAction.MainShot,
                VersusInputAction.Jump
            );
    }

    public bool WasChargeInputStartedThisFrame()
    {
        return WasRawPressedThisFrame(VersusInputAction.MainShot)
            || WasRawPressedThisFrame(VersusInputAction.SubShot)
            || WasRawPressedThisFrame(VersusInputAction.SpecialShot);
    }

    public bool IsChargeInputPressed()
    {
        return IsPressed(VersusInputAction.MainShot)
            || IsPressed(VersusInputAction.SubShot)
            || IsPressed(VersusInputAction.SpecialShot);
    }

    private bool WasCombinationPressedThisFrame(
        VersusInputAction first,
        VersusInputAction second)
    {
        return IsPressed(first)
            && IsPressed(second)
            && (WasRawPressedThisFrame(first) || WasRawPressedThisFrame(second));
    }

    private bool WasRawPressedThisFrame(VersusInputAction action)
    {
        return GetAction(action).WasPressedThisFrame();
    }

    public InputAction GetAction(VersusInputAction action)
    {
        return actions[action];
    }

    public string GetBindingDisplayName(VersusInputAction action, int bindingIndex)
    {
        InputAction inputAction = GetAction(action);

        if (bindingIndex < 0 || bindingIndex >= inputAction.bindings.Count)
        {
            return string.Empty;
        }

        return inputAction.GetBindingDisplayString(bindingIndex);
    }

    public void StartInteractiveRebind(
        VersusInputAction action,
        int bindingIndex,
        Action<bool> onFinished)
    {
        InputAction inputAction = GetAction(action);

        if (bindingIndex < 0
            || bindingIndex >= inputAction.bindings.Count
            || inputAction.bindings[bindingIndex].isComposite)
        {
            onFinished?.Invoke(false);
            return;
        }

        CancelInteractiveRebind();
        inputAsset.Disable();
        rebindOperation = inputAction.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation => FinishRebind(operation, false, onFinished))
            .OnComplete(operation => FinishRebind(operation, true, onFinished));
        rebindOperation.Start();
    }

    public void ResetAllBindings()
    {
        CancelInteractiveRebind();
        inputAsset.RemoveAllBindingOverrides();
        SaveBindingOverrides();
    }

    private void FinishRebind(
        InputActionRebindingExtensions.RebindingOperation operation,
        bool completed,
        Action<bool> onFinished)
    {
        operation.Dispose();
        rebindOperation = null;

        if (completed)
        {
            SaveBindingOverrides();
        }

        inputAsset.Enable();
        onFinished?.Invoke(completed);
    }

    private void CancelInteractiveRebind()
    {
        if (rebindOperation == null)
        {
            return;
        }

        rebindOperation.Cancel();
    }

    private void BuildActions()
    {
        inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        gameplayMap = new InputActionMap("Gameplay");
        inputAsset.AddActionMap(gameplayMap);

        InputAction move = AddAction(VersusInputAction.Move, InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");

        AddButton(VersusInputAction.Jump, "<Keyboard>/space", "<Gamepad>/buttonSouth");
        AddButton(
            VersusInputAction.MainShot,
            "<Keyboard>/e",
            "<Mouse>/leftButton",
            "<Gamepad>/leftShoulder"
        );
        AddButton(VersusInputAction.Melee, "<Keyboard>/f", "<Gamepad>/rightShoulder");
        AddButton(VersusInputAction.SubShot, "<Keyboard>/q", "<Gamepad>/buttonEast");
        AddButton(VersusInputAction.SpecialShot, "<Keyboard>/c", "<Gamepad>/leftTrigger");
        AddButton(VersusInputAction.SpecialMelee, "<Keyboard>/v", "<Gamepad>/rightTrigger");
        // 列挙値は保存済み設定との互換性のため残すが、シールド専用ボタンは作らない。
        AddAction(VersusInputAction.Guard, InputActionType.Button);
        AddButton(VersusInputAction.Search, "<Keyboard>/tab", "<Gamepad>/buttonWest");
        AddButton(VersusInputAction.Awakening, "<Keyboard>/r", "<Gamepad>/buttonNorth");
        // 覚醒技は覚醒と同じ入力を参照するため、個別の割り当ては作らない。
        AddAction(VersusInputAction.BurstAttack, InputActionType.Button);
        AddButton(
            VersusInputAction.OpenInputSettings,
            "<Keyboard>/f10",
            "<Gamepad>/select"
        );
        AddButton(VersusInputAction.Retry, "<Keyboard>/enter", "<Gamepad>/start");
    }

    private InputAction AddAction(VersusInputAction action, InputActionType type)
    {
        InputAction inputAction = gameplayMap.AddAction(action.ToString(), type);
        actions.Add(action, inputAction);
        return inputAction;
    }

    private void AddButton(VersusInputAction action, params string[] bindings)
    {
        InputAction inputAction = AddAction(action, InputActionType.Button);

        foreach (string binding in bindings)
        {
            inputAction.AddBinding(binding);
        }
    }

    private void SaveBindingOverrides()
    {
        PlayerPrefs.SetString(BindingOverridesKey, inputAsset.SaveBindingOverridesAsJson());
        PlayerPrefs.Save();
    }

    private static void MigrateBindingDefaults()
    {
        if (PlayerPrefs.GetInt(BindingDefaultsVersionKey, 0)
            >= CurrentBindingDefaultsVersion)
        {
            return;
        }

        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.SetInt(BindingDefaultsVersionKey, CurrentBindingDefaultsVersion);
        PlayerPrefs.Save();
    }

    private void LoadBindingOverrides()
    {
        string json = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            inputAsset.LoadBindingOverridesFromJson(json);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureEventSystem();
    }

    private static void ConfigureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            return;
        }

        InputSystemUIInputModule inputSystemModule =
            eventSystem.GetComponent<InputSystemUIInputModule>();

        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            inputSystemModule.AssignDefaultActions();
        }

        StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();

        if (legacyModule != null)
        {
            legacyModule.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        CancelInteractiveRebind();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        inputAsset?.Disable();
        instance = null;
    }
}
