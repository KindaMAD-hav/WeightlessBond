using UnityEngine;

public class RaycastMaterialChanger : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;           // Player's camera
    public GameObject interactableObject; // The object player interacts with
    public Renderer targetRenderer;       // The object whose material will change
    public Material newMaterial;          // The new material to apply

    [Header("Settings")]
    public float interactDistance = 5f;
    public KeyCode interactionKey = KeyCode.E;
    public float spinSpeed = 180f;        // How fast it spins before disappearing
    public float disappearDelay = 2f;     // Time in seconds before disappearing

    private bool isInteracted = false;

    void Update()
    {
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
        }

        // If interacted, spin the object until it disappears
        if (isInteracted && interactableObject != null)
        {
            interactableObject.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }

    void TryInteract()
    {
        if (playerCamera == null || targetRenderer == null || newMaterial == null || interactableObject == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.gameObject == interactableObject)
            {
                // Change target material
                targetRenderer.material = newMaterial;
                Debug.Log("Material changed on: " + targetRenderer.gameObject.name);

                // Start spinning & disappear countdown
                if (!isInteracted)
                {
                    isInteracted = true;
                    Invoke(nameof(RemoveInteractable), disappearDelay);
                }
            }
            else
            {
                Debug.Log("Looked at " + hit.collider.name + " but it's not the interactable object.");
            }
        }
    }

    void RemoveInteractable()
    {
        if (interactableObject != null)
        {
            Destroy(interactableObject); // Or use: interactableObject.SetActive(false);
            Debug.Log("Interactable disappeared!");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactDistance);
        }
    }
}
