using UnityEngine;
using UnityEngine.UI;
using TMPro; // Optional — only needed if you use TextMeshProUGUI

public class RaycastMaterialChanger : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;               // Player camera
    public GameObject interactableObject;     // Object to interact with
    public Renderer targetRenderer;           // Object whose material will change
    public Material newMaterial;              // Material to apply
    public MonoBehaviour scriptToEnable;      // Script to enable immediately after interaction
    public MonoBehaviour secondScriptToEnable; // Script to enable after pressing OK
    public MonoBehaviour playerMovementScript; // ✅ Player movement or controller script to pause/resume

    [Header("UI References")]
    public GameObject itemPanel;              // The pop-up panel
    public TextMeshProUGUI itemTitleText;     // Optional text field for item name
    public TextMeshProUGUI itemDescriptionText; // Optional text field for item info
    public Button okButton;                   // OK button

    [Header("Settings")]
    public float interactDistance = 5f;
    public KeyCode interactionKey = KeyCode.Q;
    public float spinSpeed = 90f;

    [Header("Item Info")]
    public string itemTitle = "New Item Acquired!";
    [TextArea] public string itemDescription = "You have obtained a mysterious artifact.";

    private bool isInteracted = false;
    private bool panelActive = false;

    void Start()
    {
        // Ensure all necessary scripts are off initially
        if (scriptToEnable != null)
            scriptToEnable.enabled = false;
        if (secondScriptToEnable != null)
            secondScriptToEnable.enabled = false;

        // Hide the panel
        if (itemPanel != null)
            itemPanel.SetActive(false);

        // Hook up OK button
        if (okButton != null)
            okButton.onClick.AddListener(OnOkButtonPressed);
    }

    void Update()
    {
        // Rotate interactable object for visual feedback
        if (!isInteracted && interactableObject != null)
        {
            interactableObject.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // Interaction input
        if (Input.GetKeyDown(interactionKey) && !panelActive)
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
                // ✅ Change target material
                targetRenderer.material = newMaterial;
                Debug.Log($"[Interaction] Changed material on {targetRenderer.gameObject.name}");

                // Remove interactable
                isInteracted = true;
                Destroy(interactableObject);
                Debug.Log("[Interaction] Interactable removed.");

                // Enable first script immediately
                if (scriptToEnable != null)
                {
                    scriptToEnable.enabled = true;
                    Debug.Log($"[Interaction] Enabled script: {scriptToEnable.GetType().Name}");
                }

                // Show the “item acquired” panel
                ShowItemPanel();
            }
        }
    }

    void ShowItemPanel()
    {
        if (itemPanel == null) return;

        // Pause gameplay
        Time.timeScale = 0f;
        panelActive = true;

        // Optionally disable player movement
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        // Show panel
        itemPanel.SetActive(true);
        if (itemTitleText != null) itemTitleText.text = itemTitle;
        if (itemDescriptionText != null) itemDescriptionText.text = itemDescription;

        Debug.Log("[UI] Item panel shown — game paused.");
    }

    public void OnOkButtonPressed()
    {
        if (itemPanel == null) return;

        // ✅ Hide panel
        itemPanel.SetActive(false);
        panelActive = false;

        // ✅ Resume game time
        Time.timeScale = 1f;

        // ✅ Re-enable player movement
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        // ✅ Enable second script (if assigned)
        if (secondScriptToEnable != null)
        {
            secondScriptToEnable.enabled = true;
            Debug.Log($"[UI] Enabled script: {secondScriptToEnable.GetType().Name}");
        }

        Debug.Log("[UI] OK button pressed — gameplay resumed.");
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
