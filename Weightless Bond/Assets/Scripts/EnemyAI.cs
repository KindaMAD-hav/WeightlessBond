using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float loseTargetRange = 15f;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    public float attackDamage = 20f;

    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6f;

    [Header("References")]
    public Transform player;
    public LayerMask playerLayer = 1;

    // Private variables
    private NavMeshAgent agent;
    private Animator animator;
    private float lastAttackTime;
    private bool hasTarget = false;
    private bool isDead = false;

    // Animator parameter names (matching your controller)
    private const string IS_RUNNING = "IsRunning";
    private const string ATTACK = "Attack";
    private const string GET_HIT = "GetHit";
    private const string DEATH = "Death";

    void Start()
    {
        // Get components
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Set initial agent speed
        agent.speed = walkSpeed;
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is in detection range
        if (!hasTarget && distanceToPlayer <= detectionRange)
        {
            // Check line of sight
            if (CanSeePlayer())
            {
                hasTarget = true;
            }
        }

        // Lose target if player gets too far
        if (hasTarget && distanceToPlayer > loseTargetRange)
        {
            hasTarget = false;
            agent.ResetPath();
            SetAnimatorState(false, false);
            return;
        }

        if (hasTarget)
        {
            HandleCombat(distanceToPlayer);
        }
        else
        {
            // Idle state
            SetAnimatorState(false, false);
        }
    }

    void HandleCombat(float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange)
        {
            // Stop moving and attack
            agent.ResetPath();
            LookAtPlayer();

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                AttackPlayer();
            }

            SetAnimatorState(false, false);
        }
        else
        {
            // Chase the player
            agent.SetDestination(player.position);
            agent.speed = runSpeed;
            SetAnimatorState(true, false);
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger(ATTACK);

        // Deal damage to player (you can modify this based on your player health system)
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
    }

    void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep enemy upright

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out RaycastHit hit, detectionRange))
        {
            return hit.transform == player;
        }

        return false;
    }

    void SetAnimatorState(bool isRunning, bool isAttacking)
    {
        animator.SetBool(IS_RUNNING, isRunning);
    }

    // Call this method when the enemy takes damage
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        // Trigger hit animation
        animator.SetTrigger(GET_HIT);

        // You can add health system here
        // For now, let's say enemy dies after being hit
        Die();
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        hasTarget = false;

        // Stop movement
        agent.ResetPath();
        agent.enabled = false;

        // Trigger death animation
        animator.SetTrigger(DEATH);

        // Disable collider so player can walk through
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Optional: Destroy after animation
        Destroy(gameObject, 3f);
    }

    // Gizmos for debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Lose target range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}
