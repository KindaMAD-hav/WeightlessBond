using System.Collections;
using UnityEngine;

public class ResettableObject : MonoBehaviour
{
    [Header("What to record")]
    public bool useLocalSpace = false;     // reset relative to parent?

    [Header("Stability")]
    public bool disableCollidersDuringReset = true;
    public int settleFixedSteps = 1;

    [Header("Fall after reset")]
    public bool wakeWithGravity = true;    // make sure it starts falling
    public bool nudgeDown = true;          // give a tiny downward kick so it breaks contact
    public float downwardVelocity = 0.5f;  // m/s

    Vector3 initialPos;
    Quaternion initialRot;

    void Start()
    {
        if (useLocalSpace)
        {
            initialPos = transform.localPosition;
            initialRot = transform.localRotation;
        }
        else
        {
            initialPos = transform.position;
            initialRot = transform.rotation;
        }
    }

    public void ResetObject()
    {
        StartCoroutine(DoSafeReset());
    }

    IEnumerator DoSafeReset()
    {
        var rb = GetComponent<Rigidbody>();
        var cols = disableCollidersDuringReset ? GetComponentsInChildren<Collider>() : null;

        // --- Quiesce physics ---
        bool hadRB = rb != null;
        bool prevKinematic = false;
        CollisionDetectionMode prevCD = CollisionDetectionMode.Discrete;

        if (hadRB)
        {
            prevKinematic = rb.isKinematic;
            prevCD = rb.collisionDetectionMode;

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (cols != null) foreach (var c in cols) c.enabled = false;

        // --- Teleport ---
        if (useLocalSpace)
        {
            transform.localPosition = initialPos;
            transform.localRotation = initialRot;
        }
        else
        {
            transform.position = initialPos;
            transform.rotation = initialRot;
        }
        Physics.SyncTransforms();

        // Let contacts update
        for (int i = 0; i < Mathf.Max(1, settleFixedSteps); i++)
            yield return new WaitForFixedUpdate();

        if (cols != null) foreach (var c in cols) c.enabled = true;

        // --- Restore & FALL ---
        if (hadRB)
        {
            rb.isKinematic = prevKinematic;

            // Ensure gravity can act and the RB is awake
            if (wakeWithGravity)
            {
                rb.useGravity = true;
                rb.WakeUp();
            }

            // Optional small downward kick so it immediately starts moving
            if (!rb.isKinematic && wakeWithGravity && nudgeDown && downwardVelocity > 0f)
            {
                // set a small downward velocity (doesn't add forces)
                var v = rb.linearVelocity;
                if (v.y > -downwardVelocity) v.y = -downwardVelocity;
                rb.linearVelocity = v;
            }

            // Good for fast movers after a teleport
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}
