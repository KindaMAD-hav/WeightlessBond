using UnityEngine;

public class DoorOpenerOnAllDestroyed : MonoBehaviour
{
    [Header("Door Settings")]
    public Transform door;          // Door object to move
    public float raiseAmount = 5f;  // How much to raise on Y
    public float raiseSpeed = 2f;   // Units per second

    [Header("Conditions")]
    public GameObject[] requiredObjects; // Assign objects in inspector

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isOpening = false;
    private bool hasOpened = false;

    void Start()
    {
        if (door != null)
        {
            startPos = door.position;
            targetPos = startPos + Vector3.up * raiseAmount;
        }
    }

    void Update()
    {
        if (!hasOpened && AllDestroyed())
        {
            isOpening = true;
        }

        if (isOpening && door != null)
        {
            door.position = Vector3.MoveTowards(door.position, targetPos, raiseSpeed * Time.deltaTime);

            if (Vector3.Distance(door.position, targetPos) < 0.01f)
            {
                door.position = targetPos;
                isOpening = false;
                hasOpened = true;
                Debug.Log("Door fully opened!");
            }
        }
    }

    private bool AllDestroyed()
    {
        foreach (var obj in requiredObjects)
        {
            if (obj != null) return false; // still alive
        }
        return true;
    }
}
