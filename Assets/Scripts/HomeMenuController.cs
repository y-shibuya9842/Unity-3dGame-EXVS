using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeMenuController : MonoBehaviour
{
    public const string HomeSceneName = "HomeScene";
    public const string GameSceneName = "SampleScene";
    public const string OptionsSceneName = "OptionsScene";

    private Button startButton;
    private Button optionsButton;
    private static bool initialScenePending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        initialScenePending = true;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (initialScenePending)
        {
            initialScenePending = false;

            if (scene.name != HomeSceneName)
            {
                SceneManager.LoadScene(HomeSceneName);
                return;
            }
        }

        if (scene.name != HomeSceneName
            || FindFirstObjectByType<HomeMenuController>() != null)
        {
            return;
        }

        new GameObject("HomeMenuController").AddComponent<HomeMenuController>();
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        BuildMenu();
    }

    private void Start()
    {
        StartCoroutine(SelectFirstButton());
    }

    private void BuildMenu()
    {
        Canvas canvas = MenuUiFactory.CreateCanvas("HomeCanvas");
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
        MenuUiFactory.CreateImage(
            "FieldBand",
            root,
            new Vector2(0f, 0f),
            new Vector2(1f, 0.34f),
            new Vector2(0.5f, 0f),
            Vector2.zero,
            Vector2.zero,
            new Color(0.02f, 0.12f, 0.16f, 1f)
        );
        MenuUiFactory.CreateText(
            "MenuLabel",
            root,
            "MAIN MENU",
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
            "EXVS PROTOTYPE",
            72f,
            FontStyles.Bold,
            TextAlignmentOptions.Left,
            MenuUiFactory.WhiteColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(108f, -146f),
            new Vector2(900f, 100f)
        );
        MenuUiFactory.CreateText(
            "ModeLabel",
            root,
            "2 VS 2 BATTLE SYSTEM",
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Left,
            MenuUiFactory.MutedColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -246f),
            new Vector2(520f, 38f)
        );

        RectTransform menuRoot = MenuUiFactory.CreateRect(
            "Commands",
            root,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(112f, -40f),
            new Vector2(520f, 220f)
        );
        startButton = MenuUiFactory.CreateButton(
            "StartButton",
            menuRoot,
            "出撃",
            new Vector2(0f, 48f),
            new Vector2(480f, 76f)
        );
        optionsButton = MenuUiFactory.CreateButton(
            "OptionsButton",
            menuRoot,
            "オプション",
            new Vector2(0f, -48f),
            new Vector2(480f, 76f)
        );
        startButton.onClick.AddListener(StartGame);
        optionsButton.onClick.AddListener(OpenOptions);
        MenuUiFactory.EnsureEventSystem();
    }

    private IEnumerator SelectFirstButton()
    {
        yield return null;
        EventSystem.current?.SetSelectedGameObject(startButton.gameObject);
    }

    private void StartGame()
    {
        SceneManager.LoadScene(MatchSetupState.GameSettingsSceneName);
    }

    private void OpenOptions()
    {
        SceneManager.LoadScene(OptionsSceneName);
    }

    private void OnDestroy()
    {
        startButton?.onClick.RemoveListener(StartGame);
        optionsButton?.onClick.RemoveListener(OpenOptions);
    }
}
