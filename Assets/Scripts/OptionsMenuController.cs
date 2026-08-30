using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    private readonly List<VersusInputAction> actions = new List<VersusInputAction>();
    private readonly List<int> bindingIndices = new List<int>();

    private Button actionButton;
    private Button bindingButton;
    private Button rebindButton;
    private Button resetButton;
    private Button backButton;
    private TMP_Text statusText;
    private int actionIndex;
    private int bindingIndex;
    private bool isRebinding;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != HomeMenuController.OptionsSceneName
            || FindFirstObjectByType<OptionsMenuController>() != null)
        {
            return;
        }

        new GameObject("OptionsMenuController").AddComponent<OptionsMenuController>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        actions.AddRange((VersusInputAction[])Enum.GetValues(typeof(VersusInputAction)));
        BuildMenu();
        RefreshSelection();
    }

    private void Start()
    {
        StartCoroutine(SelectFirstButton());
    }

    private void Update()
    {
        if (!isRebinding
            && VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Search))
        {
            ReturnHome();
        }
    }

    private void BuildMenu()
    {
        Canvas canvas = MenuUiFactory.CreateCanvas("OptionsCanvas");
        Transform root = canvas.transform;

        MenuUiFactory.CreateImage(
            "Background",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            MenuUiFactory.BackgroundColor
        );
        MenuUiFactory.CreateImage(
            "TopLine",
            root,
            new Vector2(0f, 1f),
            Vector2.one,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -32f),
            new Vector2(0f, 5f),
            MenuUiFactory.CyanColor
        );
        MenuUiFactory.CreateText(
            "PageLabel",
            root,
            "OPTIONS",
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.CyanColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -104f),
            new Vector2(300f, 32f)
        );
        MenuUiFactory.CreateText(
            "Title",
            root,
            "操作設定",
            58f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.WhiteColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(108f, -146f),
            new Vector2(700f, 84f)
        );

        RectTransform menuRoot = MenuUiFactory.CreateRect(
            "SettingsCommands",
            root,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(112f, -32f),
            new Vector2(760f, 520f)
        );
        actionButton = MenuUiFactory.CreateButton(
            "ActionButton",
            menuRoot,
            string.Empty,
            new Vector2(0f, 192f),
            new Vector2(720f, 72f)
        );
        bindingButton = MenuUiFactory.CreateButton(
            "BindingButton",
            menuRoot,
            string.Empty,
            new Vector2(0f, 104f),
            new Vector2(720f, 72f)
        );
        rebindButton = MenuUiFactory.CreateButton(
            "RebindButton",
            menuRoot,
            "割り当て変更",
            new Vector2(0f, 16f),
            new Vector2(720f, 72f)
        );
        resetButton = MenuUiFactory.CreateButton(
            "ResetButton",
            menuRoot,
            "すべて初期設定に戻す",
            new Vector2(0f, -72f),
            new Vector2(720f, 72f)
        );
        backButton = MenuUiFactory.CreateButton(
            "BackButton",
            menuRoot,
            "ホームへ戻る",
            new Vector2(0f, -160f),
            new Vector2(720f, 72f)
        );
        statusText = MenuUiFactory.CreateText(
            "Status",
            root,
            string.Empty,
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            MenuUiFactory.CyanColor,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(112f, 64f),
            new Vector2(900f, 42f)
        );

        actionButton.onClick.AddListener(SelectNextAction);
        bindingButton.onClick.AddListener(SelectNextBinding);
        rebindButton.onClick.AddListener(StartRebind);
        resetButton.onClick.AddListener(ResetBindings);
        backButton.onClick.AddListener(ReturnHome);
        MenuUiFactory.EnsureEventSystem();
    }

    private void SelectNextAction()
    {
        actionIndex = (actionIndex + 1) % actions.Count;
        bindingIndex = 0;
        RefreshSelection();
    }

    private void SelectNextBinding()
    {
        if (bindingIndices.Count == 0)
        {
            return;
        }

        bindingIndex = (bindingIndex + 1) % bindingIndices.Count;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (actions.Count == 0)
        {
            return;
        }

        VersusInputAction selectedAction = actions[actionIndex];
        InputAction inputAction = VersusInputManager.Instance.GetAction(selectedAction);
        bindingIndices.Clear();

        for (int i = 0; i < inputAction.bindings.Count; i++)
        {
            if (!inputAction.bindings[i].isComposite)
            {
                bindingIndices.Add(i);
            }
        }

        bindingIndex = bindingIndices.Count > 0
            ? Mathf.Clamp(bindingIndex, 0, bindingIndices.Count - 1)
            : 0;
        SetButtonLabel(actionButton, "操作項目: " + GetActionName(selectedAction));

        string bindingName = bindingIndices.Count > 0
            ? inputAction.GetBindingDisplayString(bindingIndices[bindingIndex])
            : "割り当てなし";
        SetButtonLabel(bindingButton, "割り当て: " + bindingName);
        rebindButton.interactable = bindingIndices.Count > 0;
        statusText.text = string.Empty;
    }

    private void StartRebind()
    {
        if (bindingIndices.Count == 0 || isRebinding)
        {
            return;
        }

        isRebinding = true;
        SetMenuInteractable(false);
        statusText.text = "新しく割り当てるボタンを入力してください";
        VersusInputAction action = actions[actionIndex];
        int selectedBindingIndex = bindingIndices[bindingIndex];
        VersusInputManager.Instance.StartInteractiveRebind(
            action,
            selectedBindingIndex,
            completed =>
            {
                isRebinding = false;
                SetMenuInteractable(true);
                RefreshSelection();
                statusText.text = completed
                    ? "割り当てを変更しました"
                    : "変更をキャンセルしました";
                EventSystem.current?.SetSelectedGameObject(rebindButton.gameObject);
            }
        );
    }

    private void ResetBindings()
    {
        VersusInputManager.Instance.ResetAllBindings();
        bindingIndex = 0;
        RefreshSelection();
        statusText.text = "すべての割り当てを初期設定へ戻しました";
    }

    private void SetMenuInteractable(bool interactable)
    {
        actionButton.interactable = interactable;
        bindingButton.interactable = interactable;
        rebindButton.interactable = interactable;
        resetButton.interactable = interactable;
        backButton.interactable = interactable;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;

        if (label != null)
        {
            label.text = value;
        }
    }

    private static string GetActionName(VersusInputAction action)
    {
        switch (action)
        {
            case VersusInputAction.Move: return "移動";
            case VersusInputAction.Jump: return "ジャンプ・BD";
            case VersusInputAction.MainShot: return "メイン射撃";
            case VersusInputAction.Melee: return "格闘";
            case VersusInputAction.SubShot: return "サブ射撃";
            case VersusInputAction.SpecialShot: return "特殊射撃";
            case VersusInputAction.SpecialMelee: return "特殊格闘";
            case VersusInputAction.Guard: return "シールドガード";
            case VersusInputAction.Search: return "サーチ切り替え・戻る";
            case VersusInputAction.Awakening: return "覚醒";
            case VersusInputAction.BurstAttack: return "覚醒技";
            case VersusInputAction.OpenInputSettings: return "操作設定を開く";
            case VersusInputAction.Retry: return "決定・再戦";
            default: return action.ToString();
        }
    }

    private IEnumerator SelectFirstButton()
    {
        yield return null;
        EventSystem.current?.SetSelectedGameObject(actionButton.gameObject);
    }

    private void ReturnHome()
    {
        SceneManager.LoadScene(HomeMenuController.HomeSceneName);
    }

    private void OnDestroy()
    {
        actionButton?.onClick.RemoveListener(SelectNextAction);
        bindingButton?.onClick.RemoveListener(SelectNextBinding);
        rebindButton?.onClick.RemoveListener(StartRebind);
        resetButton?.onClick.RemoveListener(ResetBindings);
        backButton?.onClick.RemoveListener(ReturnHome);
    }
}
