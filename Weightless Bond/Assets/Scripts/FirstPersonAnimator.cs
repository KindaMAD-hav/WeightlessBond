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
    [SerializeField] private string equipTrigger = "Equip"; // 🆕 Added Equip Trigger

    [Header("Animation Settings")]
    public float animationSmoothTime = 0.1f;

    [Header("Action Cooldowns")]
    public float interactCooldown = 1f;
    public float punchCooldown = 0.5f;
    public float inspectCooldown = 1.5f;
    public float equipCooldown = 1f; // 🆕 Optional cooldown for Equip

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
    private float lastEquipTime; // 🆕

    // Hash IDs for performance
    private int moveSpeedHash;
    private int isGroundedHash;
    private int isWalkingHash;
    private int isRunningHash;
    private int interactHash;
    private int punchHash;
    private int inspectHash;
    private int equipHash; // 🆕

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
        equipHash = Animator.StringToHash(equipTrigger); // 🆕

        ValidateAnimatorParameters();
    }

    void Update()
    {
        // 🆕 Press Q to trigger Equip
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TriggerEquip();
        }
    }

    void ValidateAnimatorParameters()
    {
        if (animator == null) return;

        AnimatorControllerParameter[] parameters = animator.parameters;

        bool hasMove = false, hasGrounded = false, hasWalking = false, hasRunning = false;
        bool hasInteract = false, hasPunch = false, hasInspect = false, hasEquip = false;

        foreach (var param in parameters)
        {
            switch (param.name)
            {
                case var name when name == moveSpeedParam: hasMove = true; break;
                case var name when name == isGroundedParam: hasGrounded = true; break;
                case var name when name == isWalkingParam: hasWalking = true; break;
                case var name when name == isRunningParam: hasRunning = true; break;
                case var name when name == interactTrigger: hasInteract = true; break;
                case var name when name == punchTrigger: hasPunch = true; break;
                case var name when name == inspectTrigger: hasInspect = true; break;
                case var name when name == equipTrigger: hasEquip = true; break; // 🆕
            }
        }

        if (!hasMove) Debug.LogWarning($"Animator parameter '{moveSpeedParam}' not found!");
        if (!hasGrounded) Debug.LogWarning($"Animator parameter '{isGroundedParam}' not found!");
        if (!hasWalking) Debug.LogWarning($"Animator parameter '{isWalkingParam}' not found!");
        if (!hasRunning) Debug.LogWarning($"Animator parameter '{isRunningParam}' not found!");
        if (!hasInteract) Debug.LogWarning($"Animator trigger '{interactTrigger}' not found!");
        if (!hasPunch) Debug.LogWarning($"Animator trigger '{punchTrigger}' not found!");
        if (!hasInspect) Debug.LogWarning($"Animator trigger '{inspectTrigger}' not found!");
        if (!hasEquip) Debug.LogWarning($"Animator trigger '{equipTrigger}' not found!"); // 🆕
    }

    public void SetMovementData(float inputMagnitude, bool isWalking, bool isRunning, bool isGrounded)
    {
        if (animator == null) return;

        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, inputMagnitude,
            Time.deltaTime / animationSmoothTime);

        animator.SetFloat(moveSpeedHash, currentMoveSpeed);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isWalkingHash, isWalking);
        animator.SetBool(isRunningHash, isRunning);

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

    // 🆕 EQUIP Trigger
    public void TriggerEquip()
    {
        if (CanPerformAction(lastEquipTime, equipCooldown))
        {
            animator.SetTrigger(equipHash);
            lastEquipTime = Time.time;
            Debug.Log("Equip animation triggered");
        }
    }

    private bool CanPerformAction(float lastActionTime, float cooldown)
    {
        return Time.time - lastActionTime >= cooldown;
    }

    // Public getters
    public float GetCurrentMoveSpeed() => currentMoveSpeed;
    public bool IsCurrentlyGrounded() => currentIsGrounded;
    public bool IsCurrentlyWalking() => currentIsWalking;
    public bool IsCurrentlyRunning() => currentIsRunning;

    // Animation Events
    public void OnEquipAnimationStart()
    {
        Debug.Log("Equip animation started");
        // Add logic for when equip animation starts (e.g., hide old weapon)
    }

    public void OnEquipAnimationEnd()
    {
        Debug.Log("Equip animation ended");
        // Add logic for when equip animation ends (e.g., show new weapon)
    }

    // Other animation event hooks (unchanged)
    public void OnInteractAnimationStart() { Debug.Log("Interact animation started"); }
    public void OnInteractAnimationEnd() { Debug.Log("Interact animation ended"); }
    public void OnPunchAnimationHit() { Debug.Log("Punch hit frame"); }
    public void OnPunchAnimationEnd() { Debug.Log("Punch animation ended"); }
    public void OnInspectAnimationStart() { Debug.Log("Inspect animation started"); }
    public void OnInspectAnimationEnd() { Debug.Log("Inspect animation ended"); }

    // Manual animation control
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null) animator.speed = speed;
    }

    public void PauseAnimation()
    {
        if (animator != null) animator.speed = 0f;
    }

    public void ResumeAnimation()
    {
        if (animator != null) animator.speed = 1f;
    }

    // Info helpers
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
