using UnityEngine;

public class MainMenuCursor : MonoBehaviour
{
    void Awake()
    {
        // Make sure game is unpaused and cursor is usable on the menu
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
