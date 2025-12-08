using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Controller Support")]
    public Button firstSelectedButton; // Button pertama yang dipilih (biasanya Play Button)

    void Start()
    {
        // Play menu music via AudioManager (if exists)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuMusic();
        }

        // Select first button untuk controller navigation
        // Ini biar pas masuk main menu, button langsung ke-select
        if (firstSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
        }
        else
        {
            // Fallback: cari button pertama di scene
            Button firstButton = FindObjectOfType<Button>();
            if (firstButton != null)
            {
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }
    }

    // Fungsi buat PlayButton
    public void StartGame()
    {
        Debug.Log("[MainMenu] Starting game...");

        // Switch to gameplay music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        SceneManager.LoadScene("SampleScene");
    }

    // Fungsi buat QuitButton
    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}