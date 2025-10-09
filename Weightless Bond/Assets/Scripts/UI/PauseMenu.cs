using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign your pause menu UI (Canvas or panel) here.")]
    public GameObject pauseMenuUI;

    [Header("Settings")]
    [Tooltip("Optional: Lock or unlock cursor when pausing.")]
    public bool lockCursorWhenPlaying = true;

    public static bool IsPaused { get; private set; } = false;

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
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
            if (lockCursorWhenPlaying)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    // Optional UI Buttons
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
