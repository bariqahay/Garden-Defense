using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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
    public TextMeshProUGUI winTitleText;
    public TextMeshProUGUI loseTitleText;

    [Header("Animation Settings")]
    public float panelFadeInDuration = 0.5f;
    public float delayBeforeShow = 1f;

    [Header("Audio (Optional)")]
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Hide panels dengan set alpha 0 (untuk fade in animation)
        if (winPanel != null)
        {
            winPanel.SetActive(false);
            SetPanelAlpha(winPanel, 0f);
        }
        if (losePanel != null)
        {
            losePanel.SetActive(false);
            SetPanelAlpha(losePanel, 0f);
        }
        if (gameHUD != null)
            gameHUD.SetActive(true);

        // Subscribe to events
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

    // ========== HUD UPDATES ==========
    void UpdateWaveHUD(int currentWave, int totalWaves)
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave + 1}/{totalWaves}";
    }

    void UpdatePlantsHUD(int plantsAlive)
    {
        if (plantsAliveText != null)
        {
            plantsAliveText.text = $"Plants: {plantsAlive}";

            // Color warning saat plants hampir habis
            if (plantsAlive <= 2)
                plantsAliveText.color = Color.red;
            else if (plantsAlive <= 4)
                plantsAliveText.color = Color.yellow;
            else
                plantsAliveText.color = Color.white;
        }
    }

    void UpdateEnemiesHUD(int enemiesAlive)
    {
        if (enemiesAliveText != null)
            enemiesAliveText.text = $"Enemies: {enemiesAlive}";
    }

    // ========== WIN/LOSE SCREENS ==========
    public void ShowWinScreen()
    {
        StartCoroutine(ShowPanelWithAnimation(winPanel, true));

        if (winSound != null && audioSource != null)
            audioSource.PlayOneShot(winSound);
    }

    public void ShowLoseScreen()
    {
        StartCoroutine(ShowPanelWithAnimation(losePanel, false));

        if (loseSound != null && audioSource != null)
            audioSource.PlayOneShot(loseSound);
    }

    IEnumerator ShowPanelWithAnimation(GameObject panel, bool isWin)
    {
        // Wait delay
        yield return new WaitForSecondsRealtime(delayBeforeShow);

        // Hide HUD
        if (gameHUD != null)
            gameHUD.SetActive(false);

        // Show panel
        if (panel != null)
        {
            panel.SetActive(true);

            // Update final score
            if (finalScoreText != null && GameManager.Instance != null)
            {
                int plantsAlive = GameManager.Instance.GetAlivePlantsCount();
                finalScoreText.text = $"Plants Saved: {plantsAlive}";
            }

            // Fade in animation
            yield return StartCoroutine(FadeInPanel(panel, panelFadeInDuration));
        }

        // Pause game AFTER animation
        Time.timeScale = 0f;
    }

    IEnumerator FadeInPanel(GameObject panel, float duration)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    void SetPanelAlpha(GameObject panel, float alpha)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = alpha;
    }

    // ========== BUTTON ACTIONS ==========
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;

        // Check if MainMenu scene exists
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else
        {
            Debug.LogWarning("MainMenu scene not found! Restarting current scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== UTILITY ==========
    /// <summary>
    /// Show temporary message di HUD
    /// </summary>
    public void ShowMessage(string message, float duration = 2f)
    {
        StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        // Bisa tambah TextMeshPro untuk notification
        Debug.Log($"[UI MESSAGE] {message}");
        yield return new WaitForSeconds(duration);
    }
}