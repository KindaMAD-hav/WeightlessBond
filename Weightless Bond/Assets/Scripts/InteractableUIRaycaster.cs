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
    public RawImage interactIcon;

    [Header("Audio")]
    public AudioClip appearSound;

    private AudioSource audioSource;
    private bool wasVisible = false;         // tracks per-frame visibility
    private bool hasPlayedOnceThisScene = false; // ensures one-time play per scene load

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (interactIcon != null) interactIcon.enabled = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (cam == null || interactIcon == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        bool hitInteractable = Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask);

        if (hitInteractable)
        {
            // First frame becoming visible
            if (!wasVisible)
            {
                if (!hasPlayedOnceThisScene && audioSource != null && appearSound != null)
                {
                    audioSource.PlayOneShot(appearSound);
                    hasPlayedOnceThisScene = true; // lock for the rest of the scene
                }
            }

            interactIcon.enabled = true;
            wasVisible = true;
        }
        else
        {
            interactIcon.enabled = false;
            wasVisible = false;
        }
    }
}
