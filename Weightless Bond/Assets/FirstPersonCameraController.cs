using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 100f;
    public float verticalSensitivity = 100f;
    public float horizontalSensitivity = 100f;

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

    // References
    private Transform playerBody;
    private FirstPersonController playerController;
    private Camera cam;

    // Rotation variables
    private float xRotation = 0f;
    private float mouseX;
    private float mouseY;

    // Head bob variables
    private Vector3 originalCameraPosition;
    private float bobTimer = 0f;
    private bool wasMoving = false;

    // Camera sway variables
    private Vector3 swayPosition;

    // FOV variables
    private float targetFOV;

    void Start()
    {
        // Get references
        playerBody = transform.parent; // PlayerCameraHolder
        if (playerBody != null)
        {
            playerController = playerBody.parent.GetComponent<FirstPersonController>(); // Player
        }
        cam = GetComponent<Camera>();

        // Store original camera position for head bob
        originalCameraPosition = transform.localPosition;
        targetFOV = normalFOV;

        // Ensure cursor is locked
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseInput();
        HandleCameraRotation();
        HandleHeadBob();
        HandleCameraSway();
        HandleFOVChange();
        HandleCursorToggle();
    }

    void HandleMouseInput()
    {
        mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;
    }

    void HandleCameraRotation()
    {
        // Rotate player body horizontally
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }

        // Rotate camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleHeadBob()
    {
        if (!enableHeadBob || playerController == null) return;

        bool isMoving = (playerController.IsWalking() || playerController.IsRunning()) && playerController.IsGrounded();

        if (isMoving)
        {
            // Calculate bob
            float speedMultiplier = playerController.IsRunning() ? 1.5f : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;

            float bobX = Mathf.Sin(bobTimer) * bobAmplitude * 0.5f;
            float bobY = Mathf.Sin(bobTimer * 2) * bobAmplitude;

            Vector3 bobOffset = new Vector3(bobX, bobY, 0);
            Vector3 targetPosition = originalCameraPosition + bobOffset;

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition,
                bobSmoothing * Time.deltaTime);

            wasMoving = true;
        }
        else if (wasMoving)
        {
            // Return to original position smoothly
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalCameraPosition,
                bobSmoothing * Time.deltaTime);

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

        // Calculate sway based on mouse movement
        Vector3 targetSway = new Vector3(-mouseY * swayAmount, mouseX * swayAmount, 0);
        swayPosition = Vector3.Lerp(swayPosition, targetSway, swaySmoothing * Time.deltaTime);

        // Apply sway to camera position (additive to head bob)
        Vector3 finalPosition = transform.localPosition + swayPosition;

        if (!enableHeadBob || playerController == null || (!playerController.IsWalking() && !playerController.IsRunning()))
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition,
                originalCameraPosition + swayPosition, swaySmoothing * Time.deltaTime);
        }
    }

    void HandleFOVChange()
    {
        if (cam == null || playerController == null) return;

        // Change FOV based on running state
        targetFOV = playerController.IsRunning() ? runningFOV : normalFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    void HandleCursorToggle()
    {
        // Toggle cursor with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
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

    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
        horizontalSensitivity = sensitivity;
        verticalSensitivity = sensitivity;
    }

    public void SetMouseSensitivity(float horizontal, float vertical)
    {
        horizontalSensitivity = horizontal;
        verticalSensitivity = vertical;
    }

    // Public methods for external camera control
    public void AddCameraShake(float intensity, float duration)
    {
        StartCoroutine(CameraShake(intensity, duration));
    }

    private System.Collections.IEnumerator CameraShake(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}