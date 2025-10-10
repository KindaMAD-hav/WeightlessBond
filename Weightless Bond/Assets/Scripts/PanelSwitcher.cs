using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelSwitcher : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Assign all your panels here. Make sure only one is active at a time.")]
    public GameObject[] panels;

    [Header("Buttons (Optional)")]
    [Tooltip("Optional Back button that returns to the previous panel.")]
    public Button backButton;

    private Stack<GameObject> panelHistory = new Stack<GameObject>();
    private GameObject currentPanel;

    void Start()
    {
        // Hide all panels at start
        foreach (var panel in panels)
        {
            if (panel != null) panel.SetActive(false);
        }

        // Optionally assign back button behavior
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
    }

    /// <summary>
    /// Shows the specified panel and hides the current one.
    /// </summary>
    public void ShowPanel(GameObject newPanel)
    {
        if (newPanel == null) return;

        // Hide current
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            panelHistory.Push(currentPanel);
        }

        // Show new
        newPanel.SetActive(true);
        currentPanel = newPanel;

        Debug.Log($"[PanelSwitcher] Switched to {newPanel.name}");

        // Enable/disable back button based on history
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

        // Hide current
        if (currentPanel != null)
            currentPanel.SetActive(false);

        // Show previous
        currentPanel = panelHistory.Pop();
        currentPanel.SetActive(true);

        Debug.Log($"[PanelSwitcher] Went back to {currentPanel.name}");

        // Disable back button if no more panels in history
        if (backButton != null)
            backButton.gameObject.SetActive(panelHistory.Count > 0);
    }

    /// <summary>
    /// Optional: Clear navigation history and show only this panel.
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

