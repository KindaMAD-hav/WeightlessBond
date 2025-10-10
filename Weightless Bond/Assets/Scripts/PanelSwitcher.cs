using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelSwitcher : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Assign all your panels here. No panel will be active by default.")]
    public GameObject[] panels;

    [Header("Buttons (Optional)")]
    [Tooltip("Optional Back button that returns to the previous panel.")]
    public Button backButton;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private GameObject currentPanel;

    void Awake()
    {
        // 🔒 Ensure all panels are off before anything else runs
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        currentPanel = null;
        Debug.Log("[PanelSwitcher] Awake: All panels hidden, no default panel set.");
    }

    void Start()
    {
        // ✅ Just make sure all panels remain off — do NOT activate anything automatically
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        currentPanel = null;

        // Hook up Back button (optional)
        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
            backButton.gameObject.SetActive(false); // Hide at start
        }

        Debug.Log("[PanelSwitcher] Start: Waiting for ShowPanel() call — no panel is active.");
    }

    /// <summary>
    /// Shows the specified panel and hides the current one.
    /// </summary>
    public void ShowPanel(GameObject newPanel)
    {
        if (newPanel == null)
        {
            Debug.LogWarning("[PanelSwitcher] Tried to show a null panel — ignored.");
            return;
        }

        if (currentPanel == newPanel)
        {
            Debug.Log("[PanelSwitcher] Tried to switch to the same panel — ignored.");
            return;
        }

        // ✅ Hide the current panel if one is active
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            panelHistory.Push(currentPanel);
        }

        // ✅ Show the new panel
        newPanel.SetActive(true);
        currentPanel = newPanel;

        Debug.Log($"[PanelSwitcher] Switched to {newPanel.name}");

        // ✅ Enable/disable back button
        if (backButton != null)
            backButton.gameObject.SetActive(panelHistory.Count > 0);
    }

    /// <summary>
    /// Goes back to the previous panel if available.
    /// </summary>
    public void GoBack()
    {
        if (panelHistory.Count == 0)
        {
            Debug.Log("[PanelSwitcher] No previous panel to return to.");
            return;
        }

        // Hide the current panel
        if (currentPanel != null)
            currentPanel.SetActive(false);

        // Show the previous one
        currentPanel = panelHistory.Pop();
        currentPanel.SetActive(true);

        Debug.Log($"[PanelSwitcher] Went back to {currentPanel.name}");

        // Disable back button if no more history
        if (backButton != null)
            backButton.gameObject.SetActive(panelHistory.Count > 0);
    }

    /// <summary>
    /// Clears history and shows only this panel (useful for resetting navigation).
    /// </summary>
    public void ResetToPanel(GameObject panel)
    {
        foreach (var p in panels)
        {
            if (p != null)
                p.SetActive(false);
        }

        panelHistory.Clear();
        currentPanel = panel;

        if (panel != null)
            panel.SetActive(true);

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        Debug.Log($"[PanelSwitcher] Reset navigation to {panel?.name ?? "None"}");
    }
}
