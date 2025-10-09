using UnityEngine;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign your pause menu UI (Canvas or panel) here.")]
    public GameObject pauseMenuUI;

    [Header("Settings")]
    [Tooltip("Optional: Lock or unlock cursor when pausing.")]
    public bool lockCursorWhenPlaying = true;

    public static bool IsPaused { get; private set; } = false;

    // Keep track of which AudioSources were playing when paused
    private List<AudioSource> pausedAudioSources = new List<AudioSource>();

    void Start()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        SetPause(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        SetPause(!IsPaused);
    }

    public void SetPause(bool pause)
    {
        IsPaused = pause;

        if (pause)
        {
            Time.timeScale = 0f;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(true);

            PauseAllAudio();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);

            ResumeAllAudio();

            if (lockCursorWhenPlaying)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    // --- Audio Handling ---

    void PauseAllAudio()
    {
        pausedAudioSources.Clear();

        // Find all active AudioSources in the scene
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();

        foreach (var src in allSources)
        {
            // If the source is playing, pause it and remember it
            if (src.isPlaying)
            {
                src.Pause();
                pausedAudioSources.Add(src);
            }
        }
    }

    void ResumeAllAudio()
    {
        // Resume only those that were playing before pause
        foreach (var src in pausedAudioSources)
        {
            if (src != null)
                src.UnPause();
        }

        pausedAudioSources.Clear();
    }

    // --- UI Buttons ---

    public void ResumeGame() => SetPause(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
