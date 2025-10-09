using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockSummoner : MonoBehaviour
{
    [Header("Summon Settings")]
    public KeyCode summonKey = KeyCode.Q;
    public float summonDistance = 8f;
    public float blockMoveSpeed = 10f;
    public float blockStopThreshold = 0.1f;
    public float gridSpacing = 1.5f;

    [Header("References")]
    public Camera playerCamera;

    private List<Transform> blocks = new List<Transform>();
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private bool platformActive = false;
    private bool isMoving = false;
    private Vector3[] gridPositions;

    void Start()
    {
        // Find all blocks with tag "SummonBlock"
        GameObject[] foundBlocks = GameObject.FindGameObjectsWithTag("SummonBlock");
        foreach (var obj in foundBlocks)
        {
            Transform block = obj.transform;
            blocks.Add(block);
            originalPositions[block] = block.position; // Store original position
        }

        Debug.Log($"[BlockSummoner] Found {blocks.Count} summonable blocks.");
    }

    void Update()
    {
        if (Input.GetKeyDown(summonKey) && !isMoving)
        {
            if (!platformActive)
                StartCoroutine(SummonBlocks());
            else
                StartCoroutine(ReturnBlocks());
        }
    }

    private IEnumerator SummonBlocks()
    {
        isMoving = true;

        // Raycast forward from camera to determine target point
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPoint = playerCamera.transform.position + playerCamera.transform.forward * summonDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, summonDistance))
        {
            targetPoint = hit.point;
        }

        Debug.Log("[BlockSummoner] Summoning platform at " + targetPoint);

        // Create grid offsets (3x3)
        List<Vector3> gridOffsets = new List<Vector3>();
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                gridOffsets.Add(new Vector3(x * gridSpacing, 0, z * gridSpacing));
            }
        }

        // Assign grid positions
        gridPositions = new Vector3[gridOffsets.Count];
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            gridPositions[i] = targetPoint + gridOffsets[i];
        }

        // Assign blocks to grid positions
        int count = Mathf.Min(blocks.Count, gridPositions.Length);
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(MoveBlockTo(blocks[i], gridPositions[i]));
        }

        yield return new WaitForSeconds(1.5f);
        platformActive = true;
        isMoving = false;
    }

    private IEnumerator ReturnBlocks()
    {
        isMoving = true;
        Debug.Log("[BlockSummoner] Returning blocks to original positions.");

        foreach (var block in blocks)
        {
            if (originalPositions.ContainsKey(block))
                StartCoroutine(MoveBlockTo(block, originalPositions[block]));
        }

        yield return new WaitForSeconds(1.5f);
        platformActive = false;
        isMoving = false;
    }

    private IEnumerator MoveBlockTo(Transform block, Vector3 target)
    {
        while (Vector3.Distance(block.position, target) > blockStopThreshold)
        {
            block.position = Vector3.Lerp(block.position, target, Time.deltaTime * blockMoveSpeed);
            yield return null;
        }

        block.position = target;
    }
}
