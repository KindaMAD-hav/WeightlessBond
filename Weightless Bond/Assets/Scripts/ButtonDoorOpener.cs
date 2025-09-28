using UnityEngine;

public class ButtonDoorOpener : MonoBehaviour
{
    [Header("Button Settings")]
    public Transform button;             // The button object itself
    public float pressDepth = 0.2f;      // How much the button moves down
    public float pressSpeed = 5f;        // Speed of press animation

    [Header("Door Settings")]
    public Transform door;               // Door object to open
    public float raiseAmount = 5f;       // How much to raise on Y
    public float raiseSpeed = 2f;        // Units per second

    private Vector3 buttonStartPos;
    private Vector3 buttonPressedPos;

    private Vector3 doorStartPos;
    private Vector3 doorTargetPos;

    private bool isPressed = false;
    private bool doorOpening = false;
    private bool doorOpened = false;

    void Start()
    {
        if (button != null)
        {
            buttonStartPos = button.localPosition;
            buttonPressedPos = buttonStartPos - new Vector3(0, pressDepth, 0);
        }

        if (door != null)
        {
            doorStartPos = door.position;
            doorTargetPos = doorStartPos + Vector3.up * raiseAmount;
        }
    }

    void Update()
    {
        // Animate button press
        if (button != null)
        {
            Vector3 target = isPressed ? buttonPressedPos : buttonStartPos;
            button.localPosition = Vector3.MoveTowards(button.localPosition, target, pressSpeed * Time.deltaTime);
        }

        // Animate door
        if (doorOpening && door != null)
        {
            door.position = Vector3.MoveTowards(door.position, doorTargetPos, raiseSpeed * Time.deltaTime);
            if (Vector3.Distance(door.position, doorTargetPos) < 0.01f)
            {
                door.position = doorTargetPos;
                doorOpening = false;
                doorOpened = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (doorOpened) return;

        // Any collider hitting the button will press it
        isPressed = true;
        doorOpening = true;

        Debug.Log("Button pressed! Door opening...");
    }

    private void OnTriggerExit(Collider other)
    {
        // Optional: button goes back up when object leaves
        isPressed = false;
    }
}
