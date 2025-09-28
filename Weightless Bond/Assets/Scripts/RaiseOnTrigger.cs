using UnityEngine;

public class RaiseOnTrigger : MonoBehaviour
{
    [Header("Target Object to Move")]
    public Transform targetObject;

    [Header("Raise Settings")]
    public float raiseAmount = 5f;
    public float raiseSpeed = 2f; // units per second

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isOpening = false;
    private bool hasOpened = false;

    private void Start()
    {
        if (targetObject != null)
        {
            startPos = targetObject.position;
            targetPos = startPos + Vector3.up * raiseAmount;
        }
    }

    private void Update()
    {
        if (isOpening && targetObject != null)
        {
            targetObject.position = Vector3.MoveTowards(
                targetObject.position,
                targetPos,
                raiseSpeed * Time.deltaTime
            );

            if (Vector3.Distance(targetObject.position, targetPos) < 0.01f)
            {
                targetObject.position = targetPos;
                isOpening = false;
                hasOpened = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetObject != null && !hasOpened)
        {
            isOpening = true;
            Debug.Log($"{targetObject.name} is opening smoothly!");
        }
    }
}
