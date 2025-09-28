using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        // Save starting position & rotation
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void ResetObject()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
