// GravityHand.cs
using UnityEngine;

public class GravityHand : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform holdPoint; // child of camera
    public LayerMask interactableMask;

    [Header("Targeting")]
    public float maxRayDistance = 12f;
    public float aimRadiusDegrees = 3.0f; // center window to allow highlight when "in the middle"

    [Header("Hold / Move Tuning")]
    public float holdDistance = 3.0f;
    public float minHoldDistance = 1.0f;
    public float maxHoldDistance = 10.0f;
    public float positionStrength = 600f;   // PID P term (force)
    public float velocityDamping = 50f;     // PID D term
    public float angularStrength = 50f;     // torque to stabilize rotation
    public float maxLinearSpeed = 20f;
    public float maxForce = 2000f;
    public float throwForce = 20f;

    [Header("Controls")]
    public KeyCode pickKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public KeyCode throwKey = KeyCode.Mouse1; // right click
    public KeyCode rotateHoldKey = KeyCode.R; // hold and move mouse to rotate
    public float mouseRotateSpeed = 6f;
    public float scrollDistanceStep = 0.5f;

    G_Interactable _aimed;
    G_Interactable _held;
    Vector3 _lastCamRot; // for rotation delta
    Quaternion _heldTargetRot;

    void Reset()
    {
        cam = Camera.main;
        if (!holdPoint)
        {
            var hp = new GameObject("HoldPoint").transform;
            hp.SetParent(cam.transform);
            hp.localPosition = new Vector3(0, 0, holdDistance);
            hp.localRotation = Quaternion.identity;
            holdPoint = hp;
        }
    }

    void Update()
    {
        if (!cam) cam = Camera.main;

        // Aim / highlight
        _aimed = RaycastForInteractable();

        if (_aimed) _aimed.OnFocus(true);

        // Scroll to change distance
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            holdDistance = Mathf.Clamp(holdDistance + scroll * scrollDistanceStep, minHoldDistance, maxHoldDistance);
        }
        holdPoint.localPosition = new Vector3(0, 0, holdDistance);

        // Pick up / drop / throw
        if (Input.GetKeyDown(pickKey))
        {
            if (_held == null && _aimed != null)
                TryPickup(_aimed);
            else if (_held != null)
                Drop();
        }

        if (Input.GetKeyDown(dropKey)) Drop();

        if (Input.GetKeyDown(throwKey)) Throw();

        // Rotate while holding R + mouse move
        if (_held != null && Input.GetKey(rotateHoldKey) && _held.allowRotation)
        {
            float yaw = Input.GetAxis("Mouse X") * mouseRotateSpeed;
            float pitch = -Input.GetAxis("Mouse Y") * mouseRotateSpeed;

            var delta = Quaternion.Euler(pitch, yaw, 0);
            _heldTargetRot = delta * _heldTargetRot;
        }
    }

    void FixedUpdate()
    {
        if (_held == null) return;

        var rb = _held.Body;
        if (!rb) { _held = null; return; }

        // Disable gravity while holding
        rb.useGravity = false;

        // Target position
        Vector3 targetPos = holdPoint.position;
        Vector3 toTarget = targetPos - rb.worldCenterOfMass;
        Vector3 desiredVel = Vector3.ClampMagnitude(toTarget * (positionStrength / Mathf.Max(rb.mass, 0.01f)) - rb.linearVelocity * velocityDamping, maxLinearSpeed);

        Vector3 force = (desiredVel - rb.linearVelocity) * rb.mass;
        force = Vector3.ClampMagnitude(force, maxForce);
        rb.AddForce(force, ForceMode.Force);

        // Stabilize / rotate towards target rotation
        var deltaRot = _heldTargetRot * Quaternion.Inverse(rb.rotation);
        deltaRot.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (!float.IsNaN(axis.x))
        {
            angleDeg = Mathf.DeltaAngle(0, angleDeg);
            Vector3 angVel = rb.angularVelocity;
            Vector3 desiredAngVel = axis * angleDeg * Mathf.Deg2Rad * angularStrength;
            Vector3 torque = (desiredAngVel - angVel) * rb.mass;
            rb.AddTorque(torque, ForceMode.Force);
        }
    }

    G_Interactable RaycastForInteractable()
    {
        var centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(centerRay, out var hit, maxRayDistance, interactableMask, QueryTriggerInteraction.Ignore))
        {
            var gi = hit.collider.GetComponentInParent<G_Interactable>();
            if (gi)
            {
                // Optional tighter "center window" check:
                Vector3 dirToHit = (hit.point - cam.transform.position).normalized;
                float angle = Vector3.Angle(cam.transform.forward, dirToHit);
                if (angle <= aimRadiusDegrees) return gi;
            }
        }
        return null;
    }

    void TryPickup(G_Interactable gi)
    {
        if (Vector3.Distance(cam.transform.position, gi.transform.position) > gi.maxPickupDistance) return;

        _held = gi;
        var rb = gi.Body;
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        _heldTargetRot = rb.rotation;
    }

    void Drop()
    {
        if (_held == null) return;
        var rb = _held.Body;
        rb.useGravity = true;
        _held = null;
    }

    void Throw()
    {
        if (_held == null) return;
        var rb = _held.Body;
        rb.useGravity = true;
        rb.AddForce(cam.transform.forward * throwForce * Mathf.Clamp(rb.mass, 0.5f, 5f), ForceMode.VelocityChange);
        _held = null;
    }
}
