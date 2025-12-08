using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI References")]
    public GameObject gameHUD;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pauseMenu;
    public GameObject confirmDialog;

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

    [Header("Controller Support")]
    public Button pauseMenuFirstButton; // Button "Resume" di pause menu
    public Button winPanelFirstButton;  // Button "Restart" di win panel
    public Button losePanelFirstButton; // Button "Retry" di lose panel

    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        // Hide panels
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
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (confirmDialog != null)
            confirmDialog.SetActive(false);
        if (gameHUD != null)
            gameHUD.SetActive(true);

        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveChanged += UpdateWaveHUD;
            GameManager.Instance.OnEnemyCountChanged += UpdateEnemiesHUD;
            GameManager.Instance.OnPlantsCountChanged += UpdatePlantsHUD;
            GameManager.Instance.OnGameWon += ShowWinScreen;
            GameManager.Instance.OnGameLost += ShowLoseScreen;
        }
    }

    void Update()
    {
        // Pause Input: ESC (keyboard) atau Start button (joystick button 7)
        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Joystick1Button7))
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
                TogglePause();
            }
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
        StartCoroutine(ShowPanelWithAnimation(winPanel, winPanelFirstButton, true));

        if (winSound != null && audioSource != null)
            audioSource.PlayOneShot(winSound);
    }

    public void ShowLoseScreen()
    {
        StartCoroutine(ShowPanelWithAnimation(losePanel, losePanelFirstButton, false));

        if (loseSound != null && audioSource != null)
            audioSource.PlayOneShot(loseSound);
    }

    IEnumerator ShowPanelWithAnimation(GameObject panel, Button firstButton, bool isWin)
    {
        yield return new WaitForSecondsRealtime(delayBeforeShow);

        if (gameHUD != null)
            gameHUD.SetActive(false);

        if (panel != null)
        {
            panel.SetActive(true);

            if (finalScoreText != null && GameManager.Instance != null)
            {
                int plantsAlive = GameManager.Instance.GetAlivePlantsCount();
                finalScoreText.text = $"Plants Saved: {plantsAlive}";
            }

            yield return StartCoroutine(FadeInPanel(panel, panelFadeInDuration));

            // Select first button untuk controller navigation
            if (firstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }

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
            elapsed += Time.unscaledDeltaTime;
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
        Debug.Log("[UI] Restarting game...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void RetryLevel()
    {
        RestartGame();
    }

    public void BackToMenu()
    {
        Debug.Log("[UI] Back to menu...");
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

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
        Debug.Log("[UI] Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== PAUSE MENU ==========

    public void TogglePause()
    {
        if (pauseMenu == null) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            // Pause
            pauseMenu.SetActive(true);
            Time.timeScale = 0f;

            // Select first button untuk controller
            if (pauseMenuFirstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(pauseMenuFirstButton.gameObject);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseMusic();
            }

            Debug.Log("[UI] Game paused");
        }
        else
        {
            // Resume
            pauseMenu.SetActive(false);
            if (gameHUD != null) gameHUD.SetActive(true);
            Time.timeScale = 1f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeMusic();
            }

            Debug.Log("[UI] Game resumed");
        }
    }

    public void ResumeGame()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (gameHUD != null)
            gameHUD.SetActive(true);

        Time.timeScale = 1f;
        isPaused = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
        }

        Debug.Log("[UI] Game resumed from button");
    }

    // ========== UTILITY ==========
    public void ShowMessage(string message, float duration = 2f)
    {
        StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        Debug.Log($"[UI MESSAGE] {message}");
        yield return new WaitForSeconds(duration);
    }
}