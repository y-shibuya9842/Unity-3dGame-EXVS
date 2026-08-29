using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputRebindingController : MonoBehaviour
{
    [Header("設定画面")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private bool pauseWhileOpen = true;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown actionDropdown;
    [SerializeField] private TMP_Dropdown bindingDropdown;
    [SerializeField] private TMP_Text currentBindingText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Button resetButton;

    private readonly List<VersusInputAction> displayedActions =
        new List<VersusInputAction>();
    private readonly List<int> displayedBindingIndices = new List<int>();
    private bool isPanelOpen;

    private void Start()
    {
        BuildActionOptions();

        if (actionDropdown != null)
        {
            actionDropdown.onValueChanged.AddListener(HandleActionChanged);
        }

        if (bindingDropdown != null)
        {
            bindingDropdown.onValueChanged.AddListener(_ => RefreshBindingDisplay());
        }

        if (rebindButton != null)
        {
            rebindButton.onClick.AddListener(StartRebind);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetBindings);
        }

        HandleActionChanged(0);

        if (settingsPanel != null && settingsPanel != gameObject)
        {
            SetPanelOpen(false);
        }
    }

    private void Update()
    {
        if (settingsPanel != null
            && VersusInputManager.Instance.WasPressedThisFrame(
                VersusInputAction.OpenInputSettings
            ))
        {
            SetPanelOpen(!isPanelOpen);
        }
    }

    private void BuildActionOptions()
    {
        displayedActions.Clear();
        displayedActions.AddRange((VersusInputAction[])Enum.GetValues(typeof(VersusInputAction)));

        if (actionDropdown == null)
        {
            return;
        }

        actionDropdown.ClearOptions();
        List<string> names = new List<string>();

        foreach (VersusInputAction action in displayedActions)
        {
            names.Add(GetJapaneseActionName(action));
        }

        actionDropdown.AddOptions(names);
    }

    private void HandleActionChanged(int actionIndex)
    {
        displayedBindingIndices.Clear();

        if (bindingDropdown == null
            || actionIndex < 0
            || actionIndex >= displayedActions.Count)
        {
            return;
        }

        InputAction action = VersusInputManager.Instance.GetAction(
            displayedActions[actionIndex]
        );
        List<string> bindingNames = new List<string>();

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
            {
                continue;
            }

            displayedBindingIndices.Add(i);
            string partName = binding.isPartOfComposite ? $"{binding.name}: " : string.Empty;
            bindingNames.Add(partName + action.GetBindingDisplayString(i));
        }

        bindingDropdown.ClearOptions();
        bindingDropdown.AddOptions(bindingNames);
        bindingDropdown.SetValueWithoutNotify(0);
        RefreshBindingDisplay();
    }

    private void RefreshBindingDisplay()
    {
        if (!TryGetSelection(out VersusInputAction action, out int bindingIndex))
        {
            return;
        }

        if (currentBindingText != null)
        {
            currentBindingText.text = VersusInputManager.Instance.GetBindingDisplayName(
                action,
                bindingIndex
            );
        }

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }

    private void StartRebind()
    {
        if (!TryGetSelection(out VersusInputAction action, out int bindingIndex))
        {
            return;
        }

        if (statusText != null)
        {
            statusText.text = "割り当てるキーまたはボタンを押してください";
        }

        SetButtonsInteractable(false);
        VersusInputManager.Instance.StartInteractiveRebind(
            action,
            bindingIndex,
            completed =>
            {
                SetButtonsInteractable(true);
                RefreshBindingDisplay();

                if (!completed && statusText != null)
                {
                    statusText.text = "変更をキャンセルしました";
                }
            }
        );
    }

    private void ResetBindings()
    {
        VersusInputManager.Instance.ResetAllBindings();
        HandleActionChanged(actionDropdown != null ? actionDropdown.value : 0);
    }

    private bool TryGetSelection(out VersusInputAction action, out int bindingIndex)
    {
        int actionIndex = actionDropdown != null ? actionDropdown.value : 0;
        int visibleBindingIndex = bindingDropdown != null ? bindingDropdown.value : 0;

        if (actionIndex < 0
            || actionIndex >= displayedActions.Count
            || visibleBindingIndex < 0
            || visibleBindingIndex >= displayedBindingIndices.Count)
        {
            action = default;
            bindingIndex = -1;
            return false;
        }

        action = displayedActions[actionIndex];
        bindingIndex = displayedBindingIndices[visibleBindingIndex];
        return true;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (rebindButton != null)
        {
            rebindButton.interactable = interactable;
        }

        if (resetButton != null)
        {
            resetButton.interactable = interactable;
        }
    }

    private static string GetJapaneseActionName(VersusInputAction action)
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
            case VersusInputAction.Search: return "サーチ切り替え";
            case VersusInputAction.Awakening: return "覚醒";
            case VersusInputAction.BurstAttack: return "覚醒技";
            case VersusInputAction.OpenInputSettings: return "操作設定を開く";
            case VersusInputAction.Retry: return "再戦";
            default: return action.ToString();
        }
    }

    public void SetPanelOpen(bool open)
    {
        isPanelOpen = open;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(open);
        }

        if (pauseWhileOpen)
        {
            Time.timeScale = open ? 0f : 1f;
        }
    }

    private void OnDestroy()
    {
        if (isPanelOpen && pauseWhileOpen)
        {
            Time.timeScale = 1f;
        }

        if (actionDropdown != null)
        {
            actionDropdown.onValueChanged.RemoveListener(HandleActionChanged);
        }

        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveListener(StartRebind);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetBindings);
        }
    }
}
