using UnityEngine;

public class RaycastMaterialChanger : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;           // Player's camera
    public GameObject interactableObject; // The object player interacts with
    public Renderer targetRenderer;       // The object whose material will change
    public Material newMaterial;          // The new material to apply
    public MonoBehaviour scriptToEnable;  // The script you want enabled after interaction

    [Header("Settings")]
    public float interactDistance = 5f;
    public KeyCode interactionKey = KeyCode.E;
    public float spinSpeed = 90f;         // How fast the interactable spins

    private bool isInteracted = false;

    void Start()
    {
        // Make sure the extra script starts disabled
        if (scriptToEnable != null)
            scriptToEnable.enabled = false;
    }

    void Update()
    {
        // Keep spinning until interacted
        if (!isInteracted && interactableObject != null)
        {
            interactableObject.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // Check for interaction
        if (Input.GetKeyDown(interactionKey))
        {
            TryInteract();
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

                // Immediately remove interactable
                isInteracted = true;
                Destroy(interactableObject);
                Debug.Log("Interactable disappeared!");

                // Enable the script
                if (scriptToEnable != null)
                {
                    scriptToEnable.enabled = true;
                    Debug.Log("Script enabled: " + scriptToEnable.GetType().Name);
                }
            }
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
