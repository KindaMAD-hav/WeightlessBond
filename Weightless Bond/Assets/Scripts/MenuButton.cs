using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuButtons : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainMenuUI;   // The first canvas with the menu buttons
    public GameObject cutsceneUI;   // Canvas/RawImage UI showing the cutscene

    [Header("Video Setup")]
    public VideoPlayer videoPlayer; // Reference to the VideoPlayer
    public VideoClip startCutscene; // The cutscene video clip

    private bool isCutscenePlaying = false;

    // Called by the Start button
    public void LoadNextScene()
    {
        if (videoPlayer != null && startCutscene != null && cutsceneUI != null)
        {
            if (!isCutscenePlaying)
            {
                PlayCutscene();
            }
        }
        else
        {
            // If no cutscene is assigned, just load next scene directly
            LoadSceneDirectly();
        }
    }

    private void PlayCutscene()
    {
        isCutscenePlaying = true;

        // Hide the main menu
        if (mainMenuUI != null)
            mainMenuUI.SetActive(false);

        // Show cutscene UI
        cutsceneUI.SetActive(true);

        // Set up the video
        videoPlayer.Stop();
        videoPlayer.clip = startCutscene;
        videoPlayer.isLooping = false;

        // Subscribe to the end event
        videoPlayer.loopPointReached += OnCutsceneEnd;

        // Play cutscene
        videoPlayer.Play();

        Debug.Log("Cutscene started...");
    }

    private void OnCutsceneEnd(VideoPlayer vp)
    {
        Debug.Log("Cutscene finished, loading next scene...");

        // Unsubscribe so it doesn’t fire multiple times
        videoPlayer.loopPointReached -= OnCutsceneEnd;

        LoadSceneDirectly();
    }

    private void LoadSceneDirectly()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
            nextSceneIndex = 0;

        SceneManager.LoadScene(nextSceneIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game called."); // Works in editor
        Application.Quit();              // Works in build
    }
}
