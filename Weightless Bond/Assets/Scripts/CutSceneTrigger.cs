using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public RawImage cutsceneDisplay;
    public GameObject cutsceneUI;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 5f;
    public LayerMask interactableLayer;
    public Camera playerCamera;

    [Header("Cutscene Settings")]
    public bool isFinalCutscene = false;
    public string finalSceneName = "MainMenu";
    public float finalCutsceneDelay = 12f;   // seconds before loading finalSceneName

    private bool isPlaying = false;
    private bool sceneChangeQueued = false;

    void Start()
    {
        if (cutsceneUI) cutsceneUI.SetActive(false);
        if (!playerCamera) playerCamera = Camera.main;
    }

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out var hit, interactRange, interactableLayer))
        {
            var trigger = hit.collider.GetComponentInParent<CutsceneTrigger>();
            if (trigger == this && !isPlaying)
            {
                PlayCutscene();
            }
        }
    }

    private void PlayCutscene()
    {
        if (!videoPlayer || !cutsceneUI) return;

        isPlaying = true;
        cutsceneUI.SetActive(true);

        if (isFinalCutscene && BGMHandler.Instance != null)
        {
            BGMHandler.Instance.FadeOutMusic(2f); // fade BGM over 2s
            if (!sceneChangeQueued)
            {
                sceneChangeQueued = true;
                StartCoroutine(LoadFinalSceneAfterDelay(finalCutsceneDelay));
            }
        }

        videoPlayer.loopPointReached += OnCutsceneEnd;
        videoPlayer.Play();
    }

    private void OnCutsceneEnd(VideoPlayer vp)
    {
        isPlaying = false;

        // Keep UI up if we're going to switch scenes soon; otherwise hide it
        if (!isFinalCutscene && cutsceneUI) cutsceneUI.SetActive(false);

        videoPlayer.loopPointReached -= OnCutsceneEnd;
    }

    private IEnumerator LoadFinalSceneAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene(finalSceneName);
    }
}
