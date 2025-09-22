using UnityEngine;
using System.Collections;

public class SphereSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject spherePrefab;
    public Transform spawnRegion; // Empty GameObject defining the spawn area
    public Vector2 spawnAreaSize = new Vector2(20f, 20f); // Width and Length of spawn area
    public float spawnHeight = 50f; // Height above the spawn region

    [Header("Timing")]
    public float spawnRate = 2f; // Spheres per second
    public float spawnDuration = 30f; // How long to spawn (0 = infinite)
    public float initialDelay = 0f; // Delay before first spawn

    [Header("Randomization")]
    public Vector2 damageRange = new Vector2(15f, 25f);
    public Vector2 fallSpeedRange = new Vector2(8f, 12f);
    public bool randomizeProperties = true;

    [Header("Control")]
    public bool startOnAwake = true;
    public bool canManualTrigger = true;

    private bool isSpawning = false;
    private float nextSpawnTime = 0f;
    private float spawnEndTime = 0f;

    void Start()
    {
        if (startOnAwake)
        {
            StartSpawning();
        }
    }

    void Update()
    {
        if (isSpawning && Time.time >= nextSpawnTime)
        {
            if (spawnDuration <= 0 || Time.time < spawnEndTime)
            {
                SpawnSphere();
                nextSpawnTime = Time.time + (1f / spawnRate);
            }
            else
            {
                StopSpawning();
            }
        }

        // Manual trigger for testing
        if (canManualTrigger && Input.GetKeyDown(KeyCode.R))
        {
            SpawnSphere();
        }
    }

    public void StartSpawning()
    {
        if (spherePrefab == null)
        {
            Debug.LogError("Sphere prefab not assigned!");
            return;
        }

        isSpawning = true;
        nextSpawnTime = Time.time + initialDelay;

        if (spawnDuration > 0)
        {
            spawnEndTime = Time.time + initialDelay + spawnDuration;
        }

        Debug.Log("Started spawning falling spheres!");
    }

    public void StopSpawning()
    {
        isSpawning = false;
        Debug.Log("Stopped spawning falling spheres!");
    }

    void SpawnSphere()
    {
        // Calculate spawn position
        Vector3 spawnPos = GetRandomSpawnPosition();

        // Create sphere
        GameObject sphere = Instantiate(spherePrefab, spawnPos, Quaternion.identity);

        // Randomize properties if enabled
        if (randomizeProperties)
        {
            FallingSphere fallingComp = sphere.GetComponent<FallingSphere>();
            if (fallingComp != null)
            {
                fallingComp.damage = Random.Range(damageRange.x, damageRange.y);
                fallingComp.fallSpeed = Random.Range(fallSpeedRange.x, fallSpeedRange.y);
            }
        }

        Debug.Log($"Spawned falling sphere at {spawnPos}");
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePos = spawnRegion != null ? spawnRegion.position : transform.position;

        float randomX = Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
        float randomZ = Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f);

        return new Vector3(basePos.x + randomX, basePos.y + spawnHeight, basePos.z + randomZ);
    }

    // Visualize spawn area in scene view
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

    // Public methods for external control
    public void TriggerSingleSphere()
    {
        SpawnSphere();
    }

    public void SetSpawnRate(float newRate)
    {
        spawnRate = newRate;
    }
}
