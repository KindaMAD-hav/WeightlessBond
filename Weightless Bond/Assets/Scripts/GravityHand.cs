using UnityEngine;
using System.Collections.Generic;

public class GravityHand : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform holdPoint;
    public LayerMask interactableMask;

    [Header("Targeting")]
    public float maxRayDistance = 12f;
    public float aimRadiusDegrees = 3f;

    [Header("Hold / Move Tuning")]
    public float holdDistance = 3f, minHoldDistance = 1f, maxHoldDistance = 10f;
    public float positionStrength = 600f, velocityDamping = 50f, angularStrength = 50f;
    public float maxLinearSpeed = 20f, maxForce = 2000f;
    public float throwForce = 20f;

    [Header("Controls")]
    public KeyCode pickKey = KeyCode.E, dropKey = KeyCode.Q;
    public KeyCode throwKey = KeyCode.Mouse0;
    public KeyCode throwAndSelfKey = KeyCode.Mouse1;
    public KeyCode rotateHoldKey = KeyCode.R;
    public float mouseRotateSpeed = 6f, scrollDistanceStep = 0.5f;

    G_Interactable _aimed, _held;
    Quaternion _heldTargetRot;
    FirstPersonController _player;

    private Collider[] _playerColliders;
    private readonly List<(Collider a, Collider b)> _ignored = new();

    void Awake()
    {
        if (!cam) cam = Camera.main;
        _player = GetComponentInParent<FirstPersonController>();

        _playerColliders = GetComponentInParent<Collider>()
            ? GetComponentsInParent<Collider>()
            : new Collider[0];
    }

    void OnDisable() => EndIgnorePlayerCollisions();

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

        // Aim/highlight
        _aimed = RaycastForInteractable();
        if (_aimed) _aimed.OnFocus(true);

        // Distance scroll
        var scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            holdDistance = Mathf.Clamp(holdDistance + scroll * scrollDistanceStep, minHoldDistance, maxHoldDistance);
        holdPoint.localPosition = new Vector3(0, 0, holdDistance);

        // --- Interaction Handling ---
        if (Input.GetKeyDown(pickKey))
        {
            // First check for switches
            if (_held == null)
            {
                if (Physics.Raycast(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f)), out RaycastHit hit, maxRayDistance, interactableMask))
                {
                    var resetSwitch = hit.collider.GetComponentInParent<ResetSwitch>();
                    if (resetSwitch != null)
                    {
                        resetSwitch.ActivateSwitch();
                        return; // don't try to grab object in the same frame
                    }
                }
            }

            // Otherwise handle pickup/drop
            if (_held == null && _aimed != null) TryPickup(_aimed);
            else if (_held != null) Drop();
        }

        if (Input.GetKeyDown(dropKey)) Drop();

        // Throws
        if (Input.GetKeyDown(throwKey)) ThrowObjectOnly();
        if (Input.GetKeyDown(throwAndSelfKey)) ThrowWithMomentumTransfer();

        // Rotate held object
        if (_held != null && Input.GetKey(rotateHoldKey) && _held.allowRotation)
        {
            float yaw = Input.GetAxis("Mouse X") * mouseRotateSpeed;
            float pitch = -Input.GetAxis("Mouse Y") * mouseRotateSpeed;
            _heldTargetRot = Quaternion.Euler(pitch, yaw, 0) * _heldTargetRot;
        }
    }

    void FixedUpdate()
    {
        if (_held == null) return;
        var rb = _held.Body;
        if (!rb) { _held = null; return; }

        rb.useGravity = false;

        // Position follow
        Vector3 targetPos = holdPoint.position;
        Vector3 toTarget = targetPos - rb.worldCenterOfMass;

        Vector3 desiredVel = Vector3.ClampMagnitude(
            toTarget * (positionStrength / Mathf.Max(rb.mass, 0.01f)) - rb.linearVelocity * velocityDamping,
            maxLinearSpeed);

        Vector3 force = (desiredVel - rb.linearVelocity) * rb.mass;
        rb.AddForce(Vector3.ClampMagnitude(force, maxForce), ForceMode.Force);

        // Rotation stabilize
        var deltaRot = _heldTargetRot * Quaternion.Inverse(rb.rotation);
        deltaRot.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (!float.IsNaN(axis.x))
        {
            angleDeg = Mathf.DeltaAngle(0, angleDeg);
            Vector3 desiredAngVel = axis * angleDeg * Mathf.Deg2Rad * angularStrength;
            Vector3 torque = (desiredAngVel - rb.angularVelocity) * rb.mass;
            rb.AddTorque(torque, ForceMode.Force);
        }
    }

    // --- Helpers ---
    void BeginIgnorePlayerCollisions(G_Interactable gi)
    {
        if (_playerColliders == null || _playerColliders.Length == 0) return;
        var heldCols = gi.GetComponentsInChildren<Collider>(includeInactive: false);
        foreach (var pc in _playerColliders)
        {
            if (pc == null) continue;
            foreach (var hc in heldCols)
            {
                if (hc == null || hc == pc) continue;
                Physics.IgnoreCollision(pc, hc, true);
                _ignored.Add((pc, hc));
            }
        }
    }

    void EndIgnorePlayerCollisions()
    {
        for (int i = 0; i < _ignored.Count; i++)
        {
            var (a, b) = _ignored[i];
            if (a && b) Physics.IgnoreCollision(a, b, false);
        }
        _ignored.Clear();
    }

    G_Interactable RaycastForInteractable()
    {
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out var hit, maxRayDistance, interactableMask, QueryTriggerInteraction.Ignore))
        {
            var gi = hit.collider.GetComponentInParent<G_Interactable>();
            if (gi)
            {
                Vector3 dirToHit = (hit.point - cam.transform.position).normalized;
                if (Vector3.Angle(cam.transform.forward, dirToHit) <= aimRadiusDegrees) return gi;
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

        BeginIgnorePlayerCollisions(gi);
    }

    void Drop()
    {
        if (_held == null) return;
        var rb = _held.Body;
        rb.useGravity = true;
        _held = null;

        EndIgnorePlayerCollisions();
    }

    void ThrowObjectOnly()
    {
        if (_held == null) return;
        var rb = _held.Body;
        rb.useGravity = true;
        rb.AddForce(cam.transform.forward * throwForce * Mathf.Clamp(rb.mass, 0.5f, 5f), ForceMode.VelocityChange);
        _held = null;

        EndIgnorePlayerCollisions();
    }

    void ThrowWithMomentumTransfer()
    {
        if (_held == null) return;

        var rb = _held.Body;
        rb.useGravity = true;

        // 1) Give object Δv
        Vector3 deltaV = cam.transform.forward * throwForce;
        rb.AddForce(deltaV, ForceMode.VelocityChange);

        // 2) Equal & opposite impulse to player
        float mEff = _held.GetThrowbackMassLike();
        Vector3 J = mEff * deltaV;
        if (_player != null) _player.AddImpulse(-J);

        _held = null;
        EndIgnorePlayerCollisions();
    }
}
