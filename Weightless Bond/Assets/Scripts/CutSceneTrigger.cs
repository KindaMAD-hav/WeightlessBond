using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class CutsceneTrigger : MonoBehaviour
{
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;     // VideoPlayer component
    public RawImage cutsceneDisplay;    // UI RawImage that shows the RenderTexture
    public GameObject cutsceneUI;       // Canvas or panel containing the cutscene RawImage

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRange = 5f;        // How far the player can interact
    public LayerMask interactableLayer;     // Layer for interactable objects
    public Camera playerCamera;             // Reference to the player camera

    private bool isPlaying = false;

    void Start()
    {
        if (cutsceneUI != null)
            cutsceneUI.SetActive(false); // Hide cutscene UI at start

        if (playerCamera == null)
            playerCamera = Camera.main;

        Debug.Log("CutsceneTrigger initialized. Ready for interaction.");
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // center of screen
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");

                // Check if this script is on the hit object
                if (hit.collider.gameObject == gameObject || hit.collider.GetComponentInParent<CutsceneTrigger>() == this)
                {
                    Debug.Log("Interact key pressed on correct object. Starting cutscene...");
                    if (!isPlaying)
                        PlayCutscene();
                }
                else
                {
                    Debug.Log("Raycast hit something, but not this cutscene object.");
                }
            }
            else
            {
                Debug.Log("Raycast did not hit anything interactable.");
            }
        }
    }

    private void PlayCutscene()
    {
        if (videoPlayer == null || cutsceneUI == null)
        {
            Debug.LogWarning("VideoPlayer or CutsceneUI not assigned!");
            return;
        }

        isPlaying = true;
        cutsceneUI.SetActive(true);
        videoPlayer.Play();
        Debug.Log("Cutscene started!");

        // Subscribe to end event
        videoPlayer.loopPointReached += OnCutsceneEnd;
    }

    private void OnCutsceneEnd(VideoPlayer vp)
    {
        Debug.Log("Cutscene ended!");
        isPlaying = false;

        if (cutsceneUI != null)
            cutsceneUI.SetActive(false);

        videoPlayer.loopPointReached -= OnCutsceneEnd;
    }
}
