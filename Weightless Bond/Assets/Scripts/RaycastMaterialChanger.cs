using UnityEngine;
using UnityEngine.UI;
using TMPro; // Optional, only if using TextMeshProUGUI

public class RaycastMaterialChanger : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;               // Player's camera
    public GameObject interactableObject;     // The object player interacts with
    public Renderer targetRenderer;           // The object whose material will change
    public Material newMaterial;              // The new material to apply
    public MonoBehaviour scriptToEnable;      // Script 1 to enable after interaction
    public MonoBehaviour secondScriptToEnable; // Script 2 to enable after OK button pressed

    [Header("UI References")]
    public GameObject itemPanel;              // Panel that shows after interaction
    public TextMeshProUGUI itemTitleText;     // Optional: for title
    public TextMeshProUGUI itemDescriptionText; // Optional: for description
    public Button okButton;                   // OK button on the panel

    [Header("Settings")]
    public float interactDistance = 5f;
    public KeyCode interactionKey = KeyCode.Q;
    public float spinSpeed = 90f;

    [Header("Item Info")]
    public string itemTitle = "Mysterious Artifact";
    [TextArea] public string itemDescription = "An ancient relic humming with strange energy...";

    private bool isInteracted = false;
    private bool panelActive = false;

    void Start()
    {
        // Disable extra scripts and UI at start
        if (scriptToEnable != null)
            scriptToEnable.enabled = false;
        if (secondScriptToEnable != null)
            secondScriptToEnable.enabled = false;

        if (itemPanel != null)
            itemPanel.SetActive(false);

        if (okButton != null)
            okButton.onClick.AddListener(OnOkButtonPressed);
    }

    void Update()
    {
        // Make object spin until interacted
        if (!isInteracted && interactableObject != null)
        {
            interactableObject.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // Check for player interaction
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
                // Change material
                targetRenderer.material = newMaterial;
                Debug.Log($"Material changed on: {targetRenderer.gameObject.name}");

                // Remove interactable
                isInteracted = true;
                Destroy(interactableObject);
                Debug.Log("Interactable removed.");

                // Enable first script (e.g. summoner, unlocker)
                if (scriptToEnable != null)
                {
                    scriptToEnable.enabled = true;
                    Debug.Log($"Enabled script: {scriptToEnable.GetType().Name}");
                }

                // Show item acquisition panel
                ShowItemPanel();
            }
        }
    }

    void ShowItemPanel()
    {
        if (itemPanel == null) return;

        panelActive = true;
        itemPanel.SetActive(true);

        // Update UI text
        if (itemTitleText != null) itemTitleText.text = itemTitle;
        if (itemDescriptionText != null) itemDescriptionText.text = itemDescription;

        // Pause time (optional)
        Time.timeScale = 0f;
        Debug.Log("Item panel opened.");
    }

    void OnOkButtonPressed()
    {
        if (itemPanel == null) return;

        // Hide panel
        itemPanel.SetActive(false);
        panelActive = false;

        // Resume game
        Time.timeScale = 1f;
        Debug.Log("OK pressed — panel closed.");

        // Enable second script
        if (secondScriptToEnable != null)
        {
            secondScriptToEnable.enabled = true;
            Debug.Log($"Enabled script: {secondScriptToEnable.GetType().Name}");
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
