using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectMenuController : MonoBehaviour
{
    private static readonly string[] AvailableMechs =
    {
        MatchSetupState.DefaultMechId
    };

    private readonly Button[] slotButtons = new Button[4];
    private readonly MatchMechSlot[] slots =
    {
        MatchMechSlot.Player,
        MatchMechSlot.Partner,
        MatchMechSlot.EnemyOne,
        MatchMechSlot.EnemyTwo
    };
    private readonly string[] slotNames =
    {
        "自機",
        "味方",
        "敵1",
        "敵2"
    };

    private Button startButton;
    private Button backButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MatchSetupState.CharacterSelectSceneName
            || FindFirstObjectByType<CharacterSelectMenuController>() != null)
        {
            return;
        }

        new GameObject("CharacterSelectMenuController")
            .AddComponent<CharacterSelectMenuController>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        BuildMenu();
        RefreshAllSlots();
    }

    private void Start()
    {
        StartCoroutine(SelectFirstButton());
    }

    private void Update()
    {
        if (VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Search))
        {
            ReturnToSettings();
        }
    }

    private void BuildMenu()
    {
        Canvas canvas = MenuUiFactory.CreateCanvas("CharacterSelectCanvas");
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
            "MOBILE SUIT SELECT",
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.CyanColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -104f),
            new Vector2(420f, 32f)
        );
        MenuUiFactory.CreateText(
            "Title",
            root,
            "キャラクター選択",
            58f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.WhiteColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(108f, -146f),
            new Vector2(800f, 84f)
        );
        MenuUiFactory.CreateText(
            "MechData",
            root,
            "ガンダム  /  COST 2000  /  HP 660",
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            MenuUiFactory.MutedColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -236f),
            new Vector2(720f, 38f)
        );

        RectTransform menuRoot = MenuUiFactory.CreateRect(
            "SelectionCommands",
            root,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(112f, -28f),
            new Vector2(800f, 600f)
        );

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;
            slotButtons[i] = MenuUiFactory.CreateButton(
                "Slot" + i,
                menuRoot,
                string.Empty,
                new Vector2(0f, 220f - i * 88f),
                new Vector2(760f, 72f)
            );
            slotButtons[i].onClick.AddListener(() => SelectNextMech(slotIndex));
        }

        startButton = MenuUiFactory.CreateButton(
            "StartButton",
            menuRoot,
            "この編成で出撃",
            new Vector2(0f, -158f),
            new Vector2(760f, 72f)
        );
        backButton = MenuUiFactory.CreateButton(
            "BackButton",
            menuRoot,
            "ゲーム設定へ戻る",
            new Vector2(0f, -246f),
            new Vector2(760f, 72f)
        );
        startButton.onClick.AddListener(StartBattle);
        backButton.onClick.AddListener(ReturnToSettings);
        MenuUiFactory.EnsureEventSystem();
    }

    private void SelectNextMech(int slotIndex)
    {
        string current = MatchSetupState.GetSelectedMech(slots[slotIndex]);
        int currentIndex = System.Array.IndexOf(AvailableMechs, current);
        int nextIndex = (Mathf.Max(0, currentIndex) + 1) % AvailableMechs.Length;
        MatchSetupState.SetSelectedMech(slots[slotIndex], AvailableMechs[nextIndex]);
        RefreshSlot(slotIndex);
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int slotIndex)
    {
        TMP_Text label = slotButtons[slotIndex].GetComponentInChildren<TMP_Text>();

        if (label != null)
        {
            label.text = slotNames[slotIndex] + ": " + GetMechDisplayName(
                MatchSetupState.GetSelectedMech(slots[slotIndex])
            );
        }
    }

    private static string GetMechDisplayName(string mechId)
    {
        return mechId == MatchSetupState.DefaultMechId ? "ガンダム" : mechId;
    }

    private IEnumerator SelectFirstButton()
    {
        yield return null;
        EventSystem.current?.SetSelectedGameObject(slotButtons[0].gameObject);
    }

    private void StartBattle()
    {
        SceneManager.LoadScene(HomeMenuController.GameSceneName);
    }

    private void ReturnToSettings()
    {
        SceneManager.LoadScene(MatchSetupState.GameSettingsSceneName);
    }

    private void OnDestroy()
    {
        startButton?.onClick.RemoveListener(StartBattle);
        backButton?.onClick.RemoveListener(ReturnToSettings);
    }
}
