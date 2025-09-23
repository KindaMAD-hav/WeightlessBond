using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Physics / Momentum")]
    public float playerMass = 80f;     // effective mass for impulses
    public float airDrag = 0.05f;      // 0..1 per second, tiny
    public float groundFriction = 6f;  // how fast we bleed horizontal speed when grounded

    private Vector3 worldVel;          // full velocity, includes impulses & gravity

    [Header("Ground Accel")]
    public float groundAcceleration = 50f;

    [Header("Air Accel / Strafe")]
    public float airAcceleration = 12f;          // forward/back while airborne
    public float airStrafeAcceleration = 50f;    // pure A/D strafing in air
    public float airControl = 0.30f;             // how well you can "turn" your velocity mid-air (0..1)
    public float airMaxSpeed = 7f;               // cap for forward/back air acceleration
    public float airStrafeMaxSpeed = 30f;        // higher cap for pure strafe

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float groundCheckDistance = 0.3f;

    [Header("Movement States")]
    public float walkThreshold = 0.1f; // Minimum input to start walking

    [Header("Input Settings")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode punchKey = KeyCode.Mouse0;
    public KeyCode inspectKey = KeyCode.F;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundMask = 1;

    [Header("Combat Settings")]
    public float punchRange = 2f;
    public float punchDamage = 25f;
    public LayerMask enemyLayerMask = 1 << 6; // Assuming enemies are on layer 6
    public float punchCooldown = 0.5f;

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public float footstepInterval = 0.5f;
    public AudioClip punchSound;

    // Components
    private CharacterController controller;
    private AudioSource audioSource;
    private PlayerAnimationController animationController;
    private Camera playerCamera;

    // Movement variables
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private bool isWalking;
    private float currentSpeed;
    private float inputMagnitude;

    // Combat variables
    private float lastPunchTime;

    // Audio variables
    private float footstepTimer;

    // Input variables
    private float horizontal;
    private float vertical;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponentInChildren<PlayerAnimationController>();

        // Get the camera (assuming it's a child of the player or tagged as MainCamera)
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSpeed = walkSpeed;
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleActions();
        HandleAudio();

        // Send movement data to animation controller
        if (animationController != null)
        {
            animationController.SetMovementData(inputMagnitude, isWalking, isRunning, isGrounded);
        }
    }

    public void AddImpulse(Vector3 impulseWorld)
    {
        // Δv = J / m
        worldVel += impulseWorld / Mathf.Max(0.01f, playerMass);
    }

    void HandleInput()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Calculate input magnitude for movement states
        Vector2 inputVector = new Vector2(horizontal, vertical);
        inputMagnitude = Mathf.Clamp01(inputVector.magnitude);

        // Determine movement states
        isRunning = Input.GetKey(runKey) && inputMagnitude > walkThreshold;
        isWalking = !isRunning && inputMagnitude > walkThreshold;

        // Set current speed based on state
        if (isRunning)
            currentSpeed = runSpeed;
        else if (isWalking)
            currentSpeed = walkSpeed;
        else
            currentSpeed = 0f;
    }

    void HandleMovement()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundMask);

        // Jump
        if (Input.GetKeyDown(jumpKey) && isGrounded)
            worldVel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Stick to ground when grounded & falling
        if (isGrounded && worldVel.y < 0f)
            worldVel.y = -2f;

        // Build input wish direction (camera-relative, horizontal plane)
        Vector3 wishdir = Vector3.zero;
        if (inputMagnitude > walkThreshold)
            wishdir = (transform.right * horizontal + transform.forward * vertical).normalized;

        // Split velocity
        Vector3 horiz = new Vector3(worldVel.x, 0f, worldVel.z);

        if (isGrounded)
        {
            // Ground friction first
            horiz = Vector3.MoveTowards(horiz, Vector3.zero, groundFriction * Time.deltaTime);

            // Accelerate toward target ground speed
            float wishspeed = currentSpeed;                 // walk/run speed from your state
            Accelerate(ref horiz, wishdir, wishspeed, groundAcceleration);
        }
        else
        {
            // Light air drag on existing momentum
            horiz *= Mathf.Clamp01(1f - airDrag * Time.deltaTime);

            // Decide if the player is doing a pure strafe (A/D only) vs forward/back
            bool pureStrafe = Mathf.Abs(horizontal) > 0f && Mathf.Abs(vertical) <= 0.0001f;

            float wishspeed = currentSpeed; // use your run/walk value as intent speed
            float cap = pureStrafe ? airStrafeMaxSpeed : airMaxSpeed;
            if (wishspeed > cap) wishspeed = cap;

            float accel = pureStrafe ? airStrafeAcceleration : airAcceleration;

            // Air accelerate toward wishdir (adds speed in that direction up to the cap)
            Accelerate(ref horiz, wishdir, wishspeed, accel);

            // Allow bending the current horizontal velocity toward wishdir mid-air
            AirControlTurn(ref horiz, wishdir, wishspeed);
        }

        // Recombine with vertical & apply gravity
        worldVel = new Vector3(horiz.x, worldVel.y, horiz.z);
        worldVel.y += gravity * Time.deltaTime;

        // ONE move
        controller.Move(worldVel * Time.deltaTime);

        // Re-stick if grounded after move
        if (isGrounded && worldVel.y < 0f)
            worldVel.y = -2f;
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(interactKey))
        {
            PerformInteract();
        }

        if (Input.GetKeyDown(punchKey))
        {
            PerformPunch();
        }

        if (Input.GetKeyDown(inspectKey))
        {
            PerformInspect();
        }
    }

    void HandleAudio()
    {
        if (isGrounded && (isWalking || isRunning))
        {
            footstepTimer += Time.deltaTime;

            float currentFootstepInterval = isRunning ? footstepInterval * 0.6f : footstepInterval;

            if (footstepTimer >= currentFootstepInterval)
            {
                PlayFootstepSound();
                footstepTimer = 0f;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void PlayFootstepSound()
    {
        if (footstepSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }

    void PerformInteract()
    {
        if (animationController != null)
        {
            animationController.TriggerInteract();
        }

        // Add your interaction logic here
        Debug.Log("Interact performed");
    }

    void PerformPunch()
    {
        // Check cooldown
        if (Time.time - lastPunchTime < punchCooldown)
            return;

        lastPunchTime = Time.time;

        if (animationController != null)
        {
            animationController.TriggerPunch();
        }

        // Play punch sound
        if (punchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(punchSound, 0.8f);
        }

        // Perform raycast from camera center
        Ray punchRay = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (Physics.Raycast(punchRay, out RaycastHit hit, punchRange, enemyLayerMask))
        {
            // Check if we hit an enemy
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null)
            {
                // Deal damage to the enemy
                enemy.TakeDamage(punchDamage);

                // Optional: Add punch force/impulse to the enemy
                Rigidbody enemyRb = hit.collider.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 punchForce = punchRay.direction * 5f; // Adjust force as needed
                    enemyRb.AddForce(punchForce, ForceMode.Impulse);
                }

                Debug.Log($"Punched {enemy.name} for {punchDamage} damage!");
            }
        }
        else
        {
            Debug.Log("Punch missed - no enemy in range");
        }
    }

    void PerformInspect()
    {
        if (animationController != null)
        {
            animationController.TriggerInspect();
        }

        // Add your inspect logic here
        Debug.Log("Inspect performed");
    }

    // Public methods for external access
    public bool IsGrounded() => isGrounded;
    public bool IsRunning() => isRunning;
    public bool IsWalking() => isWalking;
    public float GetInputMagnitude() => inputMagnitude;
    public float GetMovementSpeed() => currentSpeed;
    public Vector3 GetVelocity() => controller.velocity;

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }

        // Draw punch range
        if (playerCamera != null)
        {
            Gizmos.color = Color.blue;
            Vector3 punchDirection = playerCamera.transform.forward;
            Gizmos.DrawRay(playerCamera.transform.position, punchDirection * punchRange);
        }
    }

    // Quake-like accelerate: pushes horizontal velocity toward wishdir at a rate (accel),
    // capped by how much speed we're missing toward that direction.
    void Accelerate(ref Vector3 horizVel, Vector3 wishdir, float wishspeed, float accel)
    {
        if (wishspeed <= 0f) return;
        float currentspeed = Vector3.Dot(horizVel, wishdir);
        float addspeed = wishspeed - currentspeed;
        if (addspeed <= 0f) return;

        float accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed) accelspeed = addspeed;

        horizVel += wishdir * accelspeed;
    }

    // Optional "air control": lets you bend your current horizontal velocity toward wishdir while airborne.
    void AirControlTurn(ref Vector3 horizVel, Vector3 wishdir, float wishspeed)
    {
        if (airControl <= 0f || wishspeed <= 0f) return;
        // Only when moving somewhat forward relative to wishdir
        float proj = Vector3.Dot(horizVel.normalized, wishdir);
        if (proj <= 0f) return;

        // Nudge direction toward wishdir, roughly preserving magnitude
        Vector3 newDir = Vector3.Slerp(horizVel.normalized, wishdir, airControl * Time.deltaTime);
        float speed = horizVel.magnitude;
        horizVel = newDir * speed;
    }
}
