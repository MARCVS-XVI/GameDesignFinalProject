using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void LoadTitleScreen()
    {
        SceneManager.LoadScene("Title");
    }

    public void LoadInstructionsScreen()
    {
        SceneManager.LoadScene("Instructions");
    }

    public void LoadCreditsScreen()
    {
        SceneManager.LoadScene("Credits");
    }

    public void LoadGameScreen()
    {
        SceneManager.LoadScene("Game");
    }

    public void LoadPauseScreen()
    {
        SceneManager.LoadScene("Pause");
    }   

    public void LoadWinScreen()
    {
        SceneManager.LoadScene("Win");
    }

    public void LoadLossScreen()
    {
        SceneManager.LoadScene("Loss");
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

}
