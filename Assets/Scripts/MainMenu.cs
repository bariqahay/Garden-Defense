using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Fungsi buat PlayButton
    public void StartGame()
    {
        // Bisa pake nama scene atau index
        SceneManager.LoadScene("SampleScene");
        // atau SceneManager.LoadScene(1);  // kalo SampleScene ada di index 1
    }

    // Fungsi buat QuitButton
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed"); // biar muncul di editor
    }
}
