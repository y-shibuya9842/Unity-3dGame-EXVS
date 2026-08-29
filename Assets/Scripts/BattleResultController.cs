using TMPro;
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

    private bool resultDisplayed;

    private void Awake()
    {
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
            retryButton.onClick.AddListener(RetryBattle);
        }
    }

    private void Update()
    {
        if (resultDisplayed && Input.GetKeyDown(KeyCode.Return))
        {
            RetryBattle();
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
    }

    public void RetryBattle()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnBattleEnded -= ShowResult;
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryBattle);
        }

        if (resultDisplayed && pauseOnBattleEnd)
        {
            Time.timeScale = 1f;
        }
    }
}
