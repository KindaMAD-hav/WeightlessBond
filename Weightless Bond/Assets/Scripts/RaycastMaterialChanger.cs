using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RaycastMaterialChanger : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject interactableObject;
    public Renderer targetRenderer;
    public Material newMaterial;
    public MonoBehaviour scriptToEnable;
    public MonoBehaviour secondScriptToEnable;
    public MonoBehaviour playerMovementScript;

    [Header("UI References")]
    public GameObject itemPanel;
    public TextMeshProUGUI itemTitleText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("Settings")]
    public float interactDistance = 5f;
    public KeyCode interactionKey = KeyCode.Q;
    public KeyCode continueKey = KeyCode.Return; // Press Enter to continue
    public float spinSpeed = 90f;

    [Header("Item Info")]
    public string itemTitle = "New Item Acquired!";
    [TextArea] public string itemDescription = "You have obtained a mysterious artifact.";

    private bool isInteracted = false;
    private bool panelActive = false;

    void Start()
    {
        // Disable any linked scripts initially
        if (scriptToEnable != null)
            scriptToEnable.enabled = false;
        if (secondScriptToEnable != null)
            secondScriptToEnable.enabled = false;

        // Hide panel at start
        if (itemPanel != null)
            itemPanel.SetActive(false);
    }

    void Update()
    {
        // Rotate interactable object visually
        if (!isInteracted && interactableObject != null)
            interactableObject.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // Interact only when not paused or showing item panel
        if (Input.GetKeyDown(interactionKey) && !panelActive && !PauseMenu.IsPaused)
        {
            TryInteract();
        }

        // ✅ Press Enter to close the panel
        if (panelActive && Input.GetKeyDown(continueKey))
        {
            OnContinuePressed();
        }

        // Keep cursor visible if item panel is open
        if (panelActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void TryInteract()
    {
        if (playerCamera == null || targetRenderer == null || newMaterial == null || interactableObject == null)
            return;

        if (PauseMenu.IsPaused)
        {
            Debug.Log("[Interaction] Ignored because game is paused.");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.gameObject == interactableObject)
            {
                // ✅ Change material
                targetRenderer.material = newMaterial;
                Debug.Log($"[Interaction] Changed material on {targetRenderer.gameObject.name}");

                // Destroy the interactable
                isInteracted = true;
                Destroy(interactableObject);
                Debug.Log("[Interaction] Interactable removed.");

                // Enable linked scripts
                if (scriptToEnable != null)
                {
                    scriptToEnable.enabled = true;
                    Debug.Log($"[Interaction] Enabled script: {scriptToEnable.GetType().Name}");
                }

                if (secondScriptToEnable != null)
                {
                    secondScriptToEnable.enabled = true;
                    Debug.Log($"[Interaction] Enabled script: {secondScriptToEnable.GetType().Name}");
                }

                // Show item panel
                ShowItemPanel();
            }
        }
    }

    void ShowItemPanel()
    {
        if (itemPanel == null) return;

        panelActive = true;
        Time.timeScale = 0f;

        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        itemPanel.SetActive(true);
        if (itemTitleText != null) itemTitleText.text = itemTitle;
        if (itemDescriptionText != null) itemDescriptionText.text = itemDescription;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[UI] Item panel shown — press Enter to continue.");
    }

    public void OnContinuePressed()
    {
        if (itemPanel == null || !panelActive) return;

        Debug.Log("[UI] Continue pressed — closing item panel.");

        itemPanel.SetActive(false);
        panelActive = false;

        // Resume gameplay
        Time.timeScale = 1f;

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        StartCoroutine(ReLockCursorNextFrame());
    }

    private IEnumerator ReLockCursorNextFrame()
    {
        yield return null; // wait one frame for input system to settle
        if (!PauseMenu.IsPaused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("[UI] Gameplay resumed — cursor relocked.");
        }
        else
        {
            Debug.Log("[UI] Cursor left unlocked since pause menu is active.");
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
