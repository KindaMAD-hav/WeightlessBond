using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // For Back to Menu (Scene 0)

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign your pause menu UI (Canvas or panel) here.")]
    public GameObject pauseMenuUI;

    [Tooltip("Master volume slider (0..1).")]
    public Slider volumeSlider;

    [Header("Buttons")]
    [Tooltip("Button that resumes gameplay.")]
    public Button resumeButton;

    [Tooltip("Button that returns to main menu (Scene 0).")]
    public Button backToMenuButton;

    [Header("Button Images (Optional)")]
    [Tooltip("If assigned, these sprites will be applied to the buttons' Image component on Start.")]
    public Sprite resumeButtonSprite;
    public Sprite backToMenuButtonSprite;

    [Tooltip("If true, apply sprites above to the buttons' Image and set native size on Start.")]
    public bool applyButtonSpritesOnStart = true;

    [Header("Settings")]
    [Tooltip("Optional: Lock or unlock cursor when pausing.")]
    public bool lockCursorWhenPlaying = true;

    public static bool IsPaused { get; private set; } = false;

    private List<AudioSource> pausedAudioSources = new List<AudioSource>();

    // -----------------------------------------------------------
    // 🟢 FIX: Make sure pause menu never shows on Awake
    // -----------------------------------------------------------
    void Awake()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Force hide before Start runs
            Debug.Log("[PauseMenu] Awake: pause menu UI forced off.");
        }

        IsPaused = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        // Wire up UI callbacks
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);

        // Optionally apply button sprites
        if (applyButtonSpritesOnStart)
        {
            if (resumeButton != null && resumeButtonSprite != null)
            {
                var img = resumeButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = resumeButtonSprite;
                    img.SetNativeSize();
                }
            }
            if (backToMenuButton != null && backToMenuButtonSprite != null)
            {
                var img = backToMenuButton.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = backToMenuButtonSprite;
                    img.SetNativeSize();
                }
            }
        }

        // Init volume slider
        if (volumeSlider != null && AudioController.Instance != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = AudioController.Instance.GetVolume();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // 🟢 Removed SetPause(false) — no longer needed here
    }

    void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);
        if (backToMenuButton != null)
            backToMenuButton.onClick.RemoveListener(BackToMenu);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause() => SetPause(!IsPaused);

    private void OnVolumeChanged(float v)
    {
        if (AudioController.Instance != null)
            AudioController.Instance.SetVolume(v);
    }

    public void SetPause(bool pause)
    {
        IsPaused = pause;

        if (pause)
        {
            Time.timeScale = 0f;
            if (pauseMenuUI != null)
                pauseMenuUI.SetActive(true);

            PauseAllAudio();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1f;
            if (pauseMenuUI != null)
                pauseMenuUI.SetActive(false);

            ResumeAllAudio();

            if (lockCursorWhenPlaying)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (pause && volumeSlider != null && AudioController.Instance != null)
            volumeSlider.value = AudioController.Instance.GetVolume();
    }

    void OnEnable()
    {
        Debug.Log("Pause Menu enabled by: " + UnityEngine.StackTraceUtility.ExtractStackTrace());
    }

    // --- Audio Handling ---
    void PauseAllAudio()
    {
        pausedAudioSources.Clear();
        AudioSource[] allSources = FindObjectsOfType<AudioSource>();
        foreach (var src in allSources)
        {
            if (src.isPlaying)
            {
                src.Pause();
                pausedAudioSources.Add(src);
            }
        }
    }

    void ResumeAllAudio()
    {
        foreach (var src in pausedAudioSources)
        {
            if (src != null)
                src.UnPause();
        }
        pausedAudioSources.Clear();
    }

    // --- UI Buttons ---
    public void ResumeGame() => SetPause(false);

    public void BackToMenu()
    {
        if (IsPaused)
            SetPause(false);

        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
