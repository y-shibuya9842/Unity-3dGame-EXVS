using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleResultController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;

    [Header("Result")]
    [SerializeField] private BattleTeam playerTeam = BattleTeam.Player;
    [SerializeField] private string winText = "WIN";
    [SerializeField] private string loseText = "LOSE";
    [SerializeField] private bool pauseOnBattleEnd = true;
    [SerializeField, Min(0.1f)] private float resultDisplayDuration = 3f;
    [SerializeField] private string returnSceneName = HomeMenuController.HomeSceneName;

    private bool resultDisplayed;
    private Coroutine returnCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != HomeMenuController.GameSceneName)
        {
            return;
        }

        BattleManager manager = BattleManager.GetOrCreate();

        if (manager.GetComponent<BattleResultController>() == null)
        {
            manager.gameObject.AddComponent<BattleResultController>();
        }
    }

    private void Awake()
    {
        battleManager ??= GetComponent<BattleManager>();
        battleManager ??= BattleManager.GetOrCreate();
        EnsureResultView();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnBattleEnded += ShowResult;
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(ReturnHome);
        }
    }

    private void Update()
    {
        if (resultDisplayed
            && VersusInputManager.Instance.WasPressedThisFrame(VersusInputAction.Retry))
        {
            ReturnHome();
        }
    }

    private void ShowResult(BattleTeam winner)
    {
        resultDisplayed = true;

        if (resultText != null)
        {
            resultText.text = winner == playerTeam ? winText : loseText;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (pauseOnBattleEnd)
        {
            Time.timeScale = 0f;
        }

        returnCoroutine = StartCoroutine(ReturnHomeAfterDelay());
    }

    private IEnumerator ReturnHomeAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resultDisplayDuration);
        returnCoroutine = null;
        ReturnHome();
    }

    public void ReturnHome()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(returnSceneName);
    }

    private void EnsureResultView()
    {
        if (resultPanel != null && resultText != null)
        {
            return;
        }

        Canvas canvas = MenuUiFactory.CreateCanvas("BattleResultCanvas", 200);
        resultPanel = canvas.gameObject;
        MenuUiFactory.CreateImage(
            "Backdrop",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0.015f, 0.025f, 0.86f)
        );
        MenuUiFactory.CreateImage(
            "ResultLine",
            canvas.transform,
            new Vector2(0.2f, 0.5f),
            new Vector2(0.8f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -78f),
            new Vector2(0f, 6f),
            MenuUiFactory.CyanColor
        );
        MenuUiFactory.CreateText(
            "BattleEndLabel",
            canvas.transform,
            "BATTLE FINISHED",
            22f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            MenuUiFactory.CyanColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 74f),
            new Vector2(600f, 40f)
        );
        resultText = MenuUiFactory.CreateText(
            "Result",
            canvas.transform,
            string.Empty,
            96f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            MenuUiFactory.WhiteColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(800f, 130f)
        );
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnBattleEnded -= ShowResult;
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(ReturnHome);
        }

        if (resultDisplayed && pauseOnBattleEnd)
        {
            Time.timeScale = 1f;
        }
    }

    private void OnValidate()
    {
        resultDisplayDuration = Mathf.Max(0.1f, resultDisplayDuration);
    }
}
