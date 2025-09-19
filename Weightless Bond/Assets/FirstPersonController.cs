using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float groundDistance = 0.4f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public FirstPersonCameraController cameraController;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask;

    [Header("Interaction")]
    public float interactionRange = 3f;
    public LayerMask interactionMask;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode punchKey = KeyCode.Mouse0;
    public KeyCode inspectKey = KeyCode.F;

    // Private variables
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private bool isMoving;

    // Input variables
    private float horizontal;
    private float vertical;
    private bool jumpPressed;
    private bool runPressed;
    private bool interactPressed;
    private bool punchPressed;
    private bool inspectPressed;

    // Events for animation system
    public System.Action<bool> OnMovingChanged;
    public System.Action<bool> OnRunningChanged;
    public System.Action OnJump;
    public System.Action OnInteract;
    public System.Action OnPunch;
    public System.Action OnInspect;

    // Properties for animation system
    public bool IsMoving => isMoving;
    public bool IsRunning => isRunning;
    public bool IsGrounded => isGrounded;
    public float MovementSpeed => controller.velocity.magnitude;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;

        // If no camera assigned, try to find one
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // Get camera controller
        if (cameraController == null && playerCamera != null)
            cameraController = playerCamera.GetComponent<FirstPersonCameraController>();

        // If no ground check transform, create one
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleGroundCheck();
        HandleInteraction();
    }

    void HandleInput()
    {
        // Movement input
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Jump input
        jumpPressed = Input.GetButtonDown("Jump");

        // Run input
        runPressed = Input.GetKey(KeyCode.LeftShift);

        // Action inputs
        interactPressed = Input.GetKeyDown(interactKey);
        punchPressed = Input.GetKeyDown(punchKey);
        inspectPressed = Input.GetKeyDown(inspectKey);
    }

    void HandleMouseLook()
    {
        // Mouse look is now handled by FirstPersonCameraController
        // This method is kept for backward compatibility but does nothing
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to keep grounded
        }

        // Get movement direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;

        // Determine if moving
        bool wasMoving = isMoving;
        isMoving = move.magnitude > 0.1f;

        if (wasMoving != isMoving)
            OnMovingChanged?.Invoke(isMoving);

        // Determine movement speed
        bool wasRunning = isRunning;
        isRunning = isMoving && runPressed;

        if (wasRunning != isRunning)
            OnRunningChanged?.Invoke(isRunning);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Apply movement
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Handle jumping
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            OnJump?.Invoke();
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    void HandleInteraction()
    {
        // Handle interact
        if (interactPressed)
        {
            PerformInteraction();
        }

        // Handle punch
        if (punchPressed)
        {
            OnPunch?.Invoke();
        }

        // Handle inspect
        if (inspectPressed)
        {
            OnInspect?.Invoke();
        }
    }

    void PerformInteraction()
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, interactionRange, interactionMask))
        {
            // Remove gravity from the hit object
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearDamping = 2f; // Optional: add some drag to make it float more naturally
                OnInteract?.Invoke(); // Trigger interact animation

                Debug.Log($"Removed gravity from {hit.collider.name}");
            }
        }
    }

    // Method to enable/disable cursor lock (delegates to camera controller)
    public void SetCursorLock(bool locked)
    {
        if (cameraController != null)
        {
            if (locked) cameraController.LockCursor();
            else cameraController.UnlockCursor();
        }
        else
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw ground check sphere
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }

        // Draw interaction range
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionRange);
        }
    }
}

// You can add additional power-related methods here if needed