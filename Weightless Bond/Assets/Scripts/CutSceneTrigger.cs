using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

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

    private bool isPlaying = false;

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
            // 2-second fadeout
            BGMHandler.Instance.FadeOutMusic(2f);
        }

        videoPlayer.loopPointReached += OnCutsceneEnd; // <-- method exists below
        videoPlayer.Play();
    }

    private void OnCutsceneEnd(VideoPlayer vp)         // <-- must match delegate signature
    {
        isPlaying = false;
        if (cutsceneUI) cutsceneUI.SetActive(false);
        videoPlayer.loopPointReached -= OnCutsceneEnd;
    }
}
