using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Parameters")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isGroundedParam = "IsGrounded";
    [SerializeField] private string isWalkingParam = "IsWalking";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string interactTrigger = "Interact";
    [SerializeField] private string punchTrigger = "Punch";
    [SerializeField] private string inspectTrigger = "Inspect";

    [Header("Animation Settings")]
    public float animationSmoothTime = 0.1f;

    [Header("Action Cooldowns")]
    public float interactCooldown = 1f;
    public float punchCooldown = 0.5f;
    public float inspectCooldown = 1.5f;

    // Components
    private Animator animator;

    // Animation state
    private float currentMoveSpeed;
    private bool currentIsGrounded;
    private bool currentIsWalking;
    private bool currentIsRunning;

    // Cooldown timers
    private float lastInteractTime;
    private float lastPunchTime;
    private float lastInspectTime;

    // Hash IDs for performance
    private int moveSpeedHash;
    private int isGroundedHash;
    private int isWalkingHash;
    private int isRunningHash;
    private int interactHash;
    private int punchHash;
    private int inspectHash;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Cache animator parameter hash IDs
        moveSpeedHash = Animator.StringToHash(moveSpeedParam);
        isGroundedHash = Animator.StringToHash(isGroundedParam);
        isWalkingHash = Animator.StringToHash(isWalkingParam);
        isRunningHash = Animator.StringToHash(isRunningParam);
        interactHash = Animator.StringToHash(interactTrigger);
        punchHash = Animator.StringToHash(punchTrigger);
        inspectHash = Animator.StringToHash(inspectTrigger);

        // Validate animator parameters
        ValidateAnimatorParameters();
    }

    void ValidateAnimatorParameters()
    {
        if (animator == null) return;

        // Check if all required parameters exist in the animator
        AnimatorControllerParameter[] parameters = animator.parameters;

        bool hasMove = false, hasGrounded = false, hasWalking = false, hasRunning = false;
        bool hasInteract = false, hasPunch = false, hasInspect = false;

        foreach (var param in parameters)
        {
            switch (param.name)
            {
                case var name when name == moveSpeedParam:
                    hasMove = true;
                    break;
                case var name when name == isGroundedParam:
                    hasGrounded = true;
                    break;
                case var name when name == isWalkingParam:
                    hasWalking = true;
                    break;
                case var name when name == isRunningParam:
                    hasRunning = true;
                    break;
                case var name when name == interactTrigger:
                    hasInteract = true;
                    break;
                case var name when name == punchTrigger:
                    hasPunch = true;
                    break;
                case var name when name == inspectTrigger:
                    hasInspect = true;
                    break;
            }
        }

        // Log warnings for missing parameters
        if (!hasMove) Debug.LogWarning($"Animator parameter '{moveSpeedParam}' not found!");
        if (!hasGrounded) Debug.LogWarning($"Animator parameter '{isGroundedParam}' not found!");
        if (!hasWalking) Debug.LogWarning($"Animator parameter '{isWalkingParam}' not found!");
        if (!hasRunning) Debug.LogWarning($"Animator parameter '{isRunningParam}' not found!");
        if (!hasInteract) Debug.LogWarning($"Animator trigger '{interactTrigger}' not found!");
        if (!hasPunch) Debug.LogWarning($"Animator trigger '{punchTrigger}' not found!");
        if (!hasInspect) Debug.LogWarning($"Animator trigger '{inspectTrigger}' not found!");
    }

    public void SetMovementData(float inputMagnitude, bool isWalking, bool isRunning, bool isGrounded)
    {
        if (animator == null) return;

        // Smooth movement speed changes
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, inputMagnitude,
            Time.deltaTime / animationSmoothTime);

        // Set animation parameters
        animator.SetFloat(moveSpeedHash, currentMoveSpeed);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isWalkingHash, isWalking);
        animator.SetBool(isRunningHash, isRunning);

        // Update current states
        currentIsGrounded = isGrounded;
        currentIsWalking = isWalking;
        currentIsRunning = isRunning;
    }

    public void TriggerInteract()
    {
        if (CanPerformAction(lastInteractTime, interactCooldown))
        {
            animator.SetTrigger(interactHash);
            lastInteractTime = Time.time;
            Debug.Log("Interact animation triggered");
        }
    }

    public void TriggerPunch()
    {
        if (CanPerformAction(lastPunchTime, punchCooldown))
        {
            animator.SetTrigger(punchHash);
            lastPunchTime = Time.time;
            Debug.Log("Punch animation triggered");
        }
    }

    public void TriggerInspect()
    {
        if (CanPerformAction(lastInspectTime, inspectCooldown))
        {
            animator.SetTrigger(inspectHash);
            lastInspectTime = Time.time;
            Debug.Log("Inspect animation triggered");
        }
    }

    private bool CanPerformAction(float lastActionTime, float cooldown)
    {
        return Time.time - lastActionTime >= cooldown;
    }

    // Public getters for external scripts
    public float GetCurrentMoveSpeed() => currentMoveSpeed;
    public bool IsCurrentlyGrounded() => currentIsGrounded;
    public bool IsCurrentlyWalking() => currentIsWalking;
    public bool IsCurrentlyRunning() => currentIsRunning;

    // Animation Events - Call these from animation events in the Animator
    public void OnInteractAnimationStart()
    {
        Debug.Log("Interact animation started");
        // Add logic for when interact animation starts
    }

    public void OnInteractAnimationEnd()
    {
        Debug.Log("Interact animation ended");
        // Add logic for when interact animation ends
    }

    public void OnPunchAnimationHit()
    {
        Debug.Log("Punch hit frame");
        // Add logic for punch hit detection
        // This is typically called at the frame where the punch should deal damage
    }

    public void OnPunchAnimationEnd()
    {
        Debug.Log("Punch animation ended");
        // Add logic for when punch animation ends
    }

    public void OnInspectAnimationStart()
    {
        Debug.Log("Inspect animation started");
        // Add logic for when inspect animation starts
    }

    public void OnInspectAnimationEnd()
    {
        Debug.Log("Inspect animation ended");
        // Add logic for when inspect animation ends
    }

    // Manual animation control methods
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }

    public void PauseAnimation()
    {
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    public void ResumeAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    // Get current animation state info
    public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
    {
        return animator != null ? animator.GetCurrentAnimatorStateInfo(layerIndex) : new AnimatorStateInfo();
    }

    public bool IsAnimationPlaying(string stateName, int layerIndex = 0)
    {
        if (animator == null) return false;
        return animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
    }

    void OnDrawGizmosSelected()
    {
        // Visual feedback in scene view
        if (Application.isPlaying)
        {
            Gizmos.color = currentIsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);

            if (currentMoveSpeed > 0.1f)
            {
                Gizmos.color = currentIsRunning ? Color.yellow : (currentIsWalking ? Color.blue : Color.gray);
                Gizmos.DrawLine(transform.position,
                    transform.position + transform.forward * currentMoveSpeed);
            }
        }
    }
}