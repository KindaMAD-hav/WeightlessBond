using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Mouse Stability")]
    [Tooltip("Maximum mouse delta per frame (degrees). Prevents spikes on hitches/focus changes).")]
    public float maxDeltaPerFrame = 8f;

    bool wasLockedLastFrame = false;
    bool skipOneFrameMouse = false;

    [Header("Mouse Sensitivity")]
    public float horizontalSensitivity = 100f;
    public float verticalSensitivity = 100f;

    [Header("Camera Limits")]
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Camera Effects")]
    public bool enableHeadBob = true;
    public float bobFrequency = 2f;
    public float bobAmplitude = 0.05f;
    public float bobSmoothing = 5f;

    [Header("Camera Sway")]
    public bool enableSway = true;
    public float swayAmount = 0.02f;
    public float swaySmoothing = 4f;

    [Header("FOV Settings")]
    public float normalFOV = 60f;
    public float runningFOV = 70f;
    public float fovTransitionSpeed = 2f;

    [Header("Targets")]
    [Tooltip("Who receives YAW (left/right). Drag the Player root (with FirstPersonController). If empty, auto-find.")]
    [SerializeField] private Transform yawTarget;

    // Refs
    private FirstPersonController playerController;
    private Camera cam;

    // Rotation
    private float xRotation = 0f; // pitch
    private float mouseX, mouseY;

    // Head bob
    private Vector3 originalCameraPosition;
    private float bobTimer = 0f;
    private bool wasMoving = false;

    // Sway
    private Vector3 swayPosition;

    // FOV
    private float targetFOV;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Auto-find a good yaw target if none assigned:
        if (!yawTarget)
        {
            // Try to find the FirstPersonController in parents and use its transform
            var fpc = GetComponentInParent<FirstPersonController>();
            if (fpc) yawTarget = fpc.transform;
            else if (transform.parent) yawTarget = transform.parent; // fallback: holder
        }

        // Cache controller if present
        if (!playerController && yawTarget)
            playerController = yawTarget.GetComponent<FirstPersonController>();
    }

    void Start()
    {
        originalCameraPosition = transform.localPosition;
        targetFOV = normalFOV;

        // Own cursor here. If you keep it here, remove it from FirstPersonController.Start()
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCursorToggle();               // may set skipOneFrameMouse
        HandleMouseInput();                 // safe read
        HandleCameraRotation();             // apply yaw/pitch

        HandleHeadBob();
        HandleCameraSway();
        HandleFOVChange();
    }

    void HandleMouseInput()
    {
        // If cursor just got locked/unlocked or we asked to skip, kill one frame of input to avoid spikes
        if (skipOneFrameMouse)
        {
            mouseX = mouseY = 0f;
            skipOneFrameMouse = false;
            return;
        }

        // Raw mouse deltas; DO NOT multiply by Time.deltaTime
        float rawX = Input.GetAxisRaw("Mouse X");
        float rawY = Input.GetAxisRaw("Mouse Y");

        // Scale by sensitivities
        mouseX = rawX * horizontalSensitivity;
        mouseY = rawY * verticalSensitivity;

        // Clamp per-frame to nuke rare +huge deltas
        mouseX = Mathf.Clamp(mouseX, -maxDeltaPerFrame, maxDeltaPerFrame);
        mouseY = Mathf.Clamp(mouseY, -maxDeltaPerFrame, maxDeltaPerFrame);
    }

    void HandleCameraRotation()
    {
        // Yaw on player root (yawTarget)
        if (yawTarget) yawTarget.Rotate(Vector3.up * mouseX, Space.Self);

        // Pitch on camera
        xRotation = Mathf.Clamp(xRotation - mouseY, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleHeadBob()
    {
        if (!enableHeadBob || playerController == null) return;

        bool isMoving = (playerController.IsWalking() || playerController.IsRunning()) && playerController.IsGrounded();

        if (isMoving)
        {
            float speedMult = playerController.IsRunning() ? 1.5f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMult;

            float bobX = Mathf.Sin(bobTimer) * bobAmplitude * 0.5f;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmplitude;

            Vector3 targetPos = originalCameraPosition + new Vector3(bobX, bobY, 0f);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, bobSmoothing * Time.deltaTime);
            wasMoving = true;
        }
        else if (wasMoving)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalCameraPosition, bobSmoothing * Time.deltaTime);
            if (Vector3.Distance(transform.localPosition, originalCameraPosition) < 0.01f)
            {
                transform.localPosition = originalCameraPosition;
                bobTimer = 0f;
                wasMoving = false;
            }
        }
    }

    void HandleCameraSway()
    {
        if (!enableSway) return;

        Vector3 targetSway = new Vector3(-mouseY * swayAmount, mouseX * swayAmount, 0f);
        swayPosition = Vector3.Lerp(swayPosition, targetSway, swaySmoothing * Time.deltaTime);

        // Add to current local position (plays nicely with bob)
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalCameraPosition + swayPosition, swaySmoothing * Time.deltaTime);
    }

    void HandleFOVChange()
    {
        if (!cam) return;

        float desired = (playerController != null && playerController.IsRunning()) ? runningFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desired, fovTransitionSpeed * Time.deltaTime);
    }

    void HandleCursorToggle()
    {
        // Optional: toggle with Esc
        if (Input.GetKeyDown(KeyCode.Escape)) ToggleCursor();

        bool isLocked = Cursor.lockState == CursorLockMode.Locked;
        if (isLocked != wasLockedLastFrame)
        {
            // Cursor state changed this frame; discard next mouse read
            skipOneFrameMouse = true;
            wasLockedLastFrame = isLocked;
        }
    }

    public void ToggleCursor()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Convenience setters
    public void SetMouseSensitivity(float horizontal, float vertical)
    {
        horizontalSensitivity = horizontal;
        verticalSensitivity = vertical;
    }
}
