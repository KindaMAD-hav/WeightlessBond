using UnityEngine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;
    public float maxLookAngle = 80f;
    public float minLookAngle = -80f;

    [Header("Camera Effects")]
    public bool enableHeadBob = true;
    public float bobAmount = 0.05f;
    public float bobSpeed = 14f;
    public bool enableCameraSway = true;
    public float swayAmount = 0.02f;
    public float swaySpeed = 2f;

    [Header("Field of View")]
    public float normalFOV = 60f;
    public float runningFOV = 70f;
    public float fovTransitionSpeed = 2f;

    [Header("Camera Shake")]
    public bool enableCameraShake = true;
    public float shakeDecay = 5f;

    [Header("References")]
    public FirstPersonController playerController;
    public Transform cameraHolder; // Parent transform for camera positioning

    // Private variables
    private Camera playerCamera;
    private float xRotation = 0f;
    private Vector3 originalCameraPosition;

    // Head bob variables
    private float bobTimer = 0f;

    // Camera sway variables
    private float swayTimer = 0f;

    // Camera shake variables
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeIntensity = 0f;

    // FOV variables
    private float targetFOV;

    // Smoothing variables
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;
    public float mouseSmoothTime = 0.03f;

    void Start()
    {
        // Get components
        playerCamera = GetComponent<Camera>();

        if (playerController == null)
            playerController = GetComponentInParent<FirstPersonController>();

        if (cameraHolder == null)
            cameraHolder = transform.parent;

        // Store original position
        originalCameraPosition = transform.localPosition;

        // Initialize FOV
        targetFOV = normalFOV;
        playerCamera.fieldOfView = normalFOV;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraEffects();
        HandleFOVChanges();
        ApplyCameraPosition();
    }

    void HandleMouseLook()
    {
        // Get mouse input
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime,
            Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime
        );

        // Smooth mouse movement
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta,
            ref currentMouseDeltaVelocity, mouseSmoothTime);

        // Apply horizontal rotation to player body (if we have a player controller)
        if (playerController != null)
        {
            playerController.transform.Rotate(Vector3.up * currentMouseDelta.x);
        }
        else if (cameraHolder != null)
        {
            cameraHolder.Rotate(Vector3.up * currentMouseDelta.x);
        }

        // Apply vertical rotation to camera
        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleCameraEffects()
    {
        if (playerController == null) return;

        Vector3 newPosition = originalCameraPosition;

        // Head bob effect
        if (enableHeadBob && playerController.IsMoving && playerController.IsGrounded)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;

            newPosition.y += bobOffset;
            newPosition.x += bobOffsetX;
        }
        else
        {
            bobTimer = 0f;
        }

        // Camera sway effect
        if (enableCameraSway)
        {
            swayTimer += Time.deltaTime * swaySpeed;
            float swayX = Mathf.Sin(swayTimer) * swayAmount;
            float swayY = Mathf.Cos(swayTimer * 1.2f) * swayAmount * 0.5f;

            newPosition.x += swayX;
            newPosition.y += swayY;
        }

        // Apply camera shake
        if (enableCameraShake && shakeIntensity > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeIntensity = Mathf.MoveTowards(shakeIntensity, 0, shakeDecay * Time.deltaTime);
            newPosition += shakeOffset;
        }

        transform.localPosition = newPosition;
    }

    void HandleFOVChanges()
    {
        if (playerController == null) return;

        // Change FOV based on running state
        targetFOV = playerController.IsRunning ? runningFOV : normalFOV;

        // Smoothly transition FOV
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV,
            fovTransitionSpeed * Time.deltaTime);
    }

    void ApplyCameraPosition()
    {
        // This method can be used for additional position adjustments
        // Currently handled in HandleCameraEffects, but kept for extensibility
    }

    // Public methods for external control
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    public void SetFOV(float fov)
    {
        targetFOV = fov;
    }

    public void AddCameraShake(float intensity)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
    }

    public void AddCameraShake(float intensity, float duration)
    {
        StartCoroutine(ShakeCoroutine(intensity, duration));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        shakeIntensity = intensity;
        yield return new WaitForSeconds(duration);
        shakeIntensity = 0f;
    }

    // Toggle camera effects
    public void ToggleHeadBob(bool enabled)
    {
        enableHeadBob = enabled;
        if (!enabled)
        {
            bobTimer = 0f;
        }
    }

    public void ToggleCameraSway(bool enabled)
    {
        enableCameraSway = enabled;
        if (!enabled)
        {
            swayTimer = 0f;
        }
    }

    // Cursor control methods
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    // Reset camera to default state
    public void ResetCamera()
    {
        xRotation = 0f;
        transform.localRotation = Quaternion.identity;
        transform.localPosition = originalCameraPosition;
        playerCamera.fieldOfView = normalFOV;
        shakeIntensity = 0f;
        bobTimer = 0f;
        swayTimer = 0f;
    }

    // Get camera information
    public Vector3 GetCameraDirection()
    {
        return transform.forward;
    }

    public Ray GetCameraRay()
    {
        return new Ray(transform.position, transform.forward);
    }

    public Vector3 GetCameraPosition()
    {
        return transform.position;
    }

    void OnValidate()
    {
        // Ensure min/max look angles are valid
        if (minLookAngle > maxLookAngle)
        {
            minLookAngle = maxLookAngle;
        }
    }
}