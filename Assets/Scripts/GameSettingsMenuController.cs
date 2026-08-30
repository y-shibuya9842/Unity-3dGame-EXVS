using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettingsMenuController : MonoBehaviour
{
    private Button movementButton;
    private Button autoRecoveryButton;
    private Button autoGuardButton;
    private Button nextButton;
    private Button backButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MatchSetupState.GameSettingsSceneName
            || FindFirstObjectByType<GameSettingsMenuController>() != null)
        {
            return;
        }

        new GameObject("GameSettingsMenuController")
            .AddComponent<GameSettingsMenuController>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        BuildMenu();
        RefreshLabels();
    }

    private void Start()
    {
        StartCoroutine(SelectFirstButton());
    }

    private void Update()
    {
        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Search))
        {
            ReturnHome();
        }
    }

    private void BuildMenu()
    {
        Canvas canvas = MenuUiFactory.CreateCanvas("GameSettingsCanvas");
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
            "BATTLE SETUP",
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.CyanColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -104f),
            new Vector2(360f, 32f)
        );
        MenuUiFactory.CreateText(
            "Title",
            root,
            "ゲーム設定",
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
            new Vector2(112f, -20f),
            new Vector2(760f, 520f)
        );
        movementButton = MenuUiFactory.CreateButton(
            "MovementButton",
            menuRoot,
            string.Empty,
            new Vector2(0f, 190f),
            new Vector2(720f, 72f)
        );
        autoRecoveryButton = MenuUiFactory.CreateButton(
            "AutoRecoveryButton",
            menuRoot,
            string.Empty,
            new Vector2(0f, 102f),
            new Vector2(720f, 72f)
        );
        autoGuardButton = MenuUiFactory.CreateButton(
            "AutoGuardButton",
            menuRoot,
            string.Empty,
            new Vector2(0f, 14f),
            new Vector2(720f, 72f)
        );
        nextButton = MenuUiFactory.CreateButton(
            "NextButton",
            menuRoot,
            "キャラクター選択へ",
            new Vector2(0f, -96f),
            new Vector2(720f, 72f)
        );
        backButton = MenuUiFactory.CreateButton(
            "BackButton",
            menuRoot,
            "ホームへ戻る",
            new Vector2(0f, -184f),
            new Vector2(720f, 72f)
        );

        movementButton.onClick.AddListener(SelectNextMovement);
        autoRecoveryButton.onClick.AddListener(ToggleAutoRecovery);
        autoGuardButton.onClick.AddListener(ToggleAutoGuard);
        nextButton.onClick.AddListener(OpenCharacterSelect);
        backButton.onClick.AddListener(ReturnHome);
        MenuUiFactory.EnsureEventSystem();
    }

    private void SelectNextMovement()
    {
        int count = System.Enum.GetValues(typeof(TrainingEnemyMovement)).Length;
        int next = ((int)MatchSetupState.EnemyMovement + 1) % count;
        MatchSetupState.EnemyMovement = (TrainingEnemyMovement)next;
        RefreshLabels();
    }

    private void ToggleAutoRecovery()
    {
        MatchSetupState.AutoRecovery = !MatchSetupState.AutoRecovery;
        RefreshLabels();
    }

    private void ToggleAutoGuard()
    {
        MatchSetupState.AutoGuard = !MatchSetupState.AutoGuard;
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        SetButtonLabel(
            movementButton,
            "敵の動作: " + GetMovementName(MatchSetupState.EnemyMovement)
        );
        SetButtonLabel(
            autoRecoveryButton,
            "自動回復: " + GetToggleName(MatchSetupState.AutoRecovery)
        );
        SetButtonLabel(
            autoGuardButton,
            "オートガード: " + GetToggleName(MatchSetupState.AutoGuard)
        );
    }

    private static string GetMovementName(TrainingEnemyMovement movement)
    {
        switch (movement)
        {
            case TrainingEnemyMovement.Move: return "MOVE";
            case TrainingEnemyMovement.JumpLow: return "JUMP (LOW)";
            case TrainingEnemyMovement.JumpMiddle: return "JUMP (MIDDLE)";
            case TrainingEnemyMovement.JumpHigh: return "JUMP (HIGH)";
            case TrainingEnemyMovement.Avoid: return "AVOID";
            default: return "STOP";
        }
    }

    private static string GetToggleName(bool enabled)
    {
        return enabled ? "ON" : "OFF";
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;

        if (label != null)
        {
            label.text = value;
        }
    }

    private IEnumerator SelectFirstButton()
    {
        yield return null;
        EventSystem.current?.SetSelectedGameObject(movementButton.gameObject);
    }

    private void OpenCharacterSelect()
    {
        SceneManager.LoadScene(MatchSetupState.CharacterSelectSceneName);
    }

    private void ReturnHome()
    {
        SceneManager.LoadScene(HomeMenuController.HomeSceneName);
    }

    private void OnDestroy()
    {
        movementButton?.onClick.RemoveListener(SelectNextMovement);
        autoRecoveryButton?.onClick.RemoveListener(ToggleAutoRecovery);
        autoGuardButton?.onClick.RemoveListener(ToggleAutoGuard);
        nextButton?.onClick.RemoveListener(OpenCharacterSelect);
        backButton?.onClick.RemoveListener(ReturnHome);
    }
}
