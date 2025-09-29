using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class InteractableUIRaycaster : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera cam;
    public LayerMask interactableMask;
    public float interactRange = 5f;

    [Header("UI")]
    public RawImage interactIcon1;  // First icon
    public RawImage interactIcon2;  // Second icon
    public RawImage interactIcon3;  // Third icon

    [Header("Audio")]
    public AudioClip appearSound;
    [Range(0f, 2f)]
    [Tooltip("Volume multiplier for appear sound. 1 = normal, 2 = double loudness.")]
    public float appearVolume = 1f;

    private AudioSource audioSource;
    private bool wasVisible = false;             // tracks per-frame visibility
    private bool hasPlayedOnceThisScene = false; // ensures one-time play per scene load

    void Start()
    {
        if (cam == null) cam = Camera.main;

        // Hide all icons at start
        if (interactIcon1 != null) interactIcon1.enabled = false;
        if (interactIcon2 != null) interactIcon2.enabled = false;
        if (interactIcon3 != null) interactIcon3.enabled = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        bool hitInteractable = Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask);

        if (hitInteractable)
        {
            // First frame becoming visible
            if (!wasVisible)
            {
                if (!hasPlayedOnceThisScene && audioSource != null && appearSound != null)
                {
                    // Clamp final volume so it never exceeds 2.0
                    float finalVolume = Mathf.Clamp01(audioSource.volume) * appearVolume;
                    audioSource.PlayOneShot(appearSound, finalVolume);
                    hasPlayedOnceThisScene = true; // lock for the rest of the scene
                }
            }

            // Show all icons
            SetIconsActive(true);
            wasVisible = true;
        }
        else
        {
            // Hide all icons
            SetIconsActive(false);
            wasVisible = false;
        }
    }

    void SetIconsActive(bool state)
    {
        if (interactIcon1 != null) interactIcon1.enabled = state;
        if (interactIcon2 != null) interactIcon2.enabled = state;
        if (interactIcon3 != null) interactIcon3.enabled = state;
    }
}
