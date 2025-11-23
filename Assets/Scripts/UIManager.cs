using UnityEngine;
using TMPro; // <--- tambahin ini
using UnityEngine.SceneManagement;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public GameObject gameHUD;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("HUD Elements")]
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI plantsAliveText;
    public TextMeshProUGUI enemiesAliveText;

    [Header("Win/Lose Elements")]
    public TextMeshProUGUI finalScoreText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (gameHUD != null) gameHUD.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged += UpdateWaveHUD;
            GameManager.Instance.OnEnemyCountChanged += UpdateEnemiesHUD;
            GameManager.Instance.OnPlantsCountChanged += UpdatePlantsHUD;
            GameManager.Instance.OnGameWon += ShowWinScreen;
            GameManager.Instance.OnGameLost += ShowLoseScreen;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged -= UpdateWaveHUD;
            GameManager.Instance.OnEnemyCountChanged -= UpdateEnemiesHUD;
            GameManager.Instance.OnPlantsCountChanged -= UpdatePlantsHUD;
            GameManager.Instance.OnGameWon -= ShowWinScreen;
            GameManager.Instance.OnGameLost -= ShowLoseScreen;
        }
    }

    void UpdateWaveHUD(int currentWave, int totalWaves)
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave + 1}/{totalWaves}";
    }

    void UpdatePlantsHUD(int plantsAlive)
    {
        if (plantsAliveText != null)
            plantsAliveText.text = $"Plants: {plantsAlive}";
    }

    void UpdateEnemiesHUD(int enemiesAlive)
    {
        if (enemiesAliveText != null)
            enemiesAliveText.text = $"Enemies: {enemiesAlive}";
    }

    public void ShowWinScreen()
    {
        if (gameHUD != null) gameHUD.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;

        if (finalScoreText != null && GameManager.Instance != null)
        {
            int plantsAlive = GameManager.Instance.GetAlivePlantsCount();
            finalScoreText.text = $"Plants Saved: {plantsAlive}";
        }
    }

    public void ShowLoseScreen()
    {
        if (gameHUD != null) gameHUD.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
        Time.timeScale = 0f;

        if (finalScoreText != null && GameManager.Instance != null)
        {
            int plantsAlive = GameManager.Instance.GetAlivePlantsCount();
            finalScoreText.text = $"Plants Saved: {plantsAlive}";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
