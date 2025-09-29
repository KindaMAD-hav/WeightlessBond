using UnityEngine;
using System.Collections;

public class SphereSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject spherePrefab;
    public Transform spawnRegion; // Empty GameObject defining the spawn area
    public Vector2 spawnAreaSize = new Vector2(20f, 20f); // Width and Length of spawn area
    public float spawnHeight = 50f; // Height above the spawn region

    [Header("Player Detection")]
    public float activationRange = 30f; // Distance to start spawning
    public float deactivationRange = 40f; // Distance to stop spawning (should be > activationRange)
    public LayerMask playerLayerMask = 1; // What layer is the player on
    public string playerTag = "Player"; // Player tag

    [Header("Timing")]
    public float spawnRate = 2f; // Spheres per second
    public float initialDelay = 0f; // Delay before first spawn

    [Header("Performance")]
    public float playerCheckInterval = 0.5f; // How often to check for player (seconds)
    public int maxActiveSpheres = 10; // Limit concurrent spheres for performance

    [Header("Randomization")]
    public Vector2 damageRange = new Vector2(15f, 25f);
    public Vector2 fallSpeedRange = new Vector2(8f, 12f);
    public bool randomizeProperties = true;

    [Header("Control")]
    public bool canManualTrigger = true;

    // Private variables
    private bool isSpawning = false;
    private bool playerInRange = false;
    private float nextSpawnTime = 0f;
    private float nextPlayerCheck = 0f;
    private Transform playerTransform;
    private int currentActiveSpheres = 0;
    private Vector3 cachedPosition; // Cache our position for distance calculations

    // Optimization: Cache frequently used values
    private float spawnRateReciprocal; // Store 1/spawnRate to avoid division

    [Header("Audio")]
    public bool enableSpawnSFX = true; // toggle on/off
    public AudioClip spawnSound;
    [Range(0.5f, 2f)] public float pitchMin = 0.9f;
    [Range(0.5f, 2f)] public float pitchMax = 1.1f;
    [Tooltip("Play the spawn sound once per this many spawned spheres (e.g. 10 = play once every 10 spawns).")]
    public int playEveryNSpawns = 1;  // default = play every spawn

    private AudioSource audioSource;
    private int spawnCountSinceLastSound = 0;

    void Start()
    {
        // Cache our position
        cachedPosition = transform.position;

        // Cache spawn rate reciprocal for optimization
        UpdateSpawnRateCache();

        // Find player once at start
        FindPlayer();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Check for player at intervals instead of every frame
        if (Time.time >= nextPlayerCheck)
        {
            CheckPlayerDistance();
            nextPlayerCheck = Time.time + playerCheckInterval;
        }

        // Handle spawning - run continuously while player is in range
        if (isSpawning && Time.time >= nextSpawnTime && currentActiveSpheres < maxActiveSpheres)
        {
            SpawnSphere();
            nextSpawnTime = Time.time + spawnRateReciprocal;
        }

        // Manual trigger for testing (only in editor)
#if UNITY_EDITOR
        if (canManualTrigger && Input.GetKeyDown(KeyCode.R))
        {
            SpawnSphere();
        }
#endif
    }

    void FindPlayer()
    {
        // Try to find player by tag first (fastest)
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }

        // Fallback: find by component (slower but more reliable)
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }
    }

    void CheckPlayerDistance()
    {
        // If no player found, try to find again
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // Use sqrMagnitude for performance (avoids expensive square root calculation)
        float sqrDistance = (playerTransform.position - cachedPosition).sqrMagnitude;
        float sqrActivationRange = activationRange * activationRange;
        float sqrDeactivationRange = deactivationRange * deactivationRange;

        bool wasInRange = playerInRange;

        if (!playerInRange && sqrDistance <= sqrActivationRange)
        {
            // Player entered range
            playerInRange = true;
            StartSpawning();
            Debug.Log($"Player entered spawn range. Distance: {Mathf.Sqrt(sqrDistance):F1}");
        }
        else if (playerInRange && sqrDistance >= sqrDeactivationRange)
        {
            // Player left range
            playerInRange = false;
            StopSpawning();
            Debug.Log($"Player left spawn range. Distance: {Mathf.Sqrt(sqrDistance):F1}");
        }
    }

    public void StartSpawning()
    {
        if (spherePrefab == null)
        {
            Debug.LogError("Sphere prefab not assigned!");
            return;
        }

        if (isSpawning) return; // Already spawning

        isSpawning = true;
        nextSpawnTime = Time.time + initialDelay;

        Debug.Log("Started spawning falling spheres - will continue while player is in range!");
    }

    public void StopSpawning()
    {
        if (!isSpawning) return; // Already stopped

        isSpawning = false;
        Debug.Log("Stopped spawning falling spheres - player left range!");
    }

    void SpawnSphere()
    {
        // Calculate spawn position
        Vector3 spawnPos = GetRandomSpawnPosition();

        // Create sphere
        GameObject sphere = Instantiate(spherePrefab, spawnPos, Quaternion.identity);

        // Track active spheres for performance
        currentActiveSpheres++;

        // Set up sphere destruction callback to update counter
        FallingSphere fallingComp = sphere.GetComponent<FallingSphere>();
        if (fallingComp != null)
        {
            // Randomize properties if enabled
            if (randomizeProperties)
            {
                fallingComp.damage = Random.Range(damageRange.x, damageRange.y);
                fallingComp.fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);
            }

            // Set up destruction callback
            StartCoroutine(TrackSphereLifetime(sphere));
        }
        else
        {
            // Fallback: track by lifetime if no FallingSphere component
            StartCoroutine(TrackSphereLifetime(sphere, 10f));
        }

        // Increment spawn counter
        spawnCountSinceLastSound++;

        if (enableSpawnSFX && spawnSound != null && audioSource != null && playEveryNSpawns > 0)
        {
            spawnCountSinceLastSound++;

            if (spawnCountSinceLastSound >= playEveryNSpawns)
            {
                audioSource.pitch = Random.Range(pitchMin, pitchMax);
                audioSource.PlayOneShot(spawnSound);
                spawnCountSinceLastSound = 0;
            }
        }

        Debug.Log($"Spawned falling sphere at {spawnPos} (Active: {currentActiveSpheres}/{maxActiveSpheres})");
    }

    // Coroutine to track when spheres are destroyed
    IEnumerator TrackSphereLifetime(GameObject sphere, float maxLifetime = -1f)
    {
        // Wait until sphere is destroyed or max lifetime reached
        if (maxLifetime > 0)
        {
            float endTime = Time.time + maxLifetime;
            while (sphere != null && Time.time < endTime)
            {
                yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            }
        }
        else
        {
            while (sphere != null)
            {
                yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            }
        }

        // Decrease counter when sphere is destroyed
        currentActiveSpheres = Mathf.Max(0, currentActiveSpheres - 1);
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePos = spawnRegion != null ? spawnRegion.position : cachedPosition;

        // Use cached random values for better performance
        float randomX = Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
        float randomZ = Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f);

        return new Vector3(basePos.x + randomX, basePos.y + spawnHeight, basePos.z + randomZ);
    }

    // Update spawn rate and cache reciprocal
    public void SetSpawnRate(float newRate)
    {
        spawnRate = Mathf.Max(0.1f, newRate); // Minimum rate
        UpdateSpawnRateCache();
    }

    void UpdateSpawnRateCache()
    {
        spawnRateReciprocal = 1f / spawnRate;
    }

    // Visualize spawn area and detection ranges in scene view
    void OnDrawGizmos()
    {
        Vector3 center = spawnRegion != null ? spawnRegion.position : transform.position;

        // Draw spawn area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

        // Draw spawn height
        Gizmos.color = Color.green;
        Vector3 spawnCenter = center + Vector3.up * spawnHeight;
        Gizmos.DrawWireCube(spawnCenter, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

        // Draw player detection ranges
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, deactivationRange);

        // Draw connection lines
        Gizmos.color = Color.blue;
        Vector3[] corners = {
            center + new Vector3(-spawnAreaSize.x/2, 0, -spawnAreaSize.y/2),
            center + new Vector3(spawnAreaSize.x/2, 0, -spawnAreaSize.y/2),
            center + new Vector3(spawnAreaSize.x/2, 0, spawnAreaSize.y/2),
            center + new Vector3(-spawnAreaSize.x/2, 0, spawnAreaSize.y/2)
        };

        foreach (var corner in corners)
        {
            Gizmos.DrawLine(corner, corner + Vector3.up * spawnHeight);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Show more detailed info when selected
        Vector3 pos = transform.position;

        // Draw activation range with label
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, activationRange);

        // Draw deactivation range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, deactivationRange);

#if UNITY_EDITOR
        // Add text labels in scene view
        UnityEditor.Handles.Label(pos + Vector3.up * 2f, 
            $"Activation: {activationRange}m\nDeactivation: {deactivationRange}m\nActive Spheres: {currentActiveSpheres}/{maxActiveSpheres}");
#endif
    }

    // Public methods for external control
    public void TriggerSingleSphere()
    {
        if (currentActiveSpheres < maxActiveSpheres)
        {
            SpawnSphere();
        }
    }

    public void ForceStart()
    {
        playerInRange = true;
        StartSpawning();
    }

    public void ForceStop()
    {
        playerInRange = false;
        StopSpawning();
    }

    // Public getters for monitoring
    public bool IsPlayerInRange() => playerInRange;
    public bool IsSpawning() => isSpawning;
    public int GetActiveSphereCount() => currentActiveSpheres;
}