using UnityEngine;
using UnityEngine.UI;

public class InteractableUIRaycaster : MonoBehaviour
{
    [Header("Raycast Settings")]
    public Camera cam;                  // Assign your player camera
    public LayerMask interactableMask;  // Set to "Interactable" layer in Inspector
    public float interactRange = 5f;    // Changeable in Inspector

    [Header("UI")]
    public RawImage interactIcon;       // UI element that shows up when aiming

    void Start()
    {
        if (cam == null) cam = Camera.main;
        if (interactIcon != null)
            interactIcon.enabled = false; // hide at start
    }

    void Update()
    {
        if (cam == null || interactIcon == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f)); // center of screen
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask))
        {
            // Hit an interactable → show icon
            interactIcon.enabled = true;
        }
        else
        {
            // Nothing hit → hide icon
            interactIcon.enabled = false;
        }
    }
}
