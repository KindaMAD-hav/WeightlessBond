using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockSummonerPreview : MonoBehaviour
{
    [Header("Summon Settings")]
    public KeyCode summonKey = KeyCode.Q;
    public float summonDistance = 8f;
    public float blockMoveSpeed = 10f;
    public float blockStopThreshold = 0.1f;
    public float gridSpacing = 1.5f;

    [Header("Return Scatter Settings")]
    public float scatterRadius = 2f;        // how far they scatter when returning
    public float scatterDelayRange = 0.5f;  // max random delay before returning

    [Header("References")]
    public Camera playerCamera;
    public GameObject ghostBlockPrefab;
    public ParticleSystem summonEffectPrefab;

    private List<Transform> blocks = new List<Transform>();
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private List<GameObject> ghostBlocks = new List<GameObject>();

    private bool isPreviewing = false;
    private bool isPlaced = false;
    private bool isMoving = false;

    private Vector3[] gridPositions;
    private Vector3 targetPoint;

    void Start()
    {
        GameObject[] foundBlocks = GameObject.FindGameObjectsWithTag("SummonBlock");
        foreach (var obj in foundBlocks)
        {
            Transform block = obj.transform;
            blocks.Add(block);
            originalPositions[block] = block.position;
        }

        Debug.Log($"[BlockSummoner] Found {blocks.Count} summonable blocks.");
    }

    void Update()
    {
        if (Input.GetKeyDown(summonKey) && !isMoving)
        {
            if (!isPreviewing && !isPlaced)
            {
                ShowPreview();
            }
            else if (isPreviewing)
            {
                StartCoroutine(SummonBlocks());
            }
            else if (isPlaced)
            {
                StartCoroutine(ReturnBlocksWithScatter());
            }
        }

        if (isPreviewing)
        {
            UpdatePreviewPosition();
        }
    }

    private void ShowPreview()
    {
        isPreviewing = true;
        Debug.Log("[BlockSummoner] Showing placement preview.");

        if (ghostBlocks.Count == 0)
        {
            for (int i = 0; i < 9; i++)
            {
                GameObject ghost = Instantiate(ghostBlockPrefab);
                ghostBlocks.Add(ghost);
            }
        }

        foreach (var ghost in ghostBlocks)
            ghost.SetActive(true);

        UpdatePreviewPosition();
    }

    private void UpdatePreviewPosition()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        targetPoint = playerCamera.transform.position + playerCamera.transform.forward * summonDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, summonDistance))
            targetPoint = hit.point;

        List<Vector3> gridOffsets = new List<Vector3>();
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                gridOffsets.Add(new Vector3(x * gridSpacing, 0, z * gridSpacing));

        gridPositions = new Vector3[gridOffsets.Count];
        for (int i = 0; i < gridOffsets.Count; i++)
        {
            gridPositions[i] = targetPoint + gridOffsets[i];
            ghostBlocks[i].transform.position = gridPositions[i];
        }
    }

    private IEnumerator SummonBlocks()
    {
        isMoving = true;
        isPreviewing = false;
        isPlaced = true;

        Debug.Log("[BlockSummoner] Summoning platform at " + targetPoint);

        foreach (var ghost in ghostBlocks)
            ghost.SetActive(false);

        if (summonEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(summonEffectPrefab, targetPoint, Quaternion.identity);
            effect.Play();
            StartCoroutine(DestroyWhenDone(effect));
        }

        int count = Mathf.Min(blocks.Count, gridPositions.Length);
        for (int i = 0; i < count; i++)
            StartCoroutine(MoveBlockTo(blocks[i], gridPositions[i]));

        yield return new WaitForSeconds(1.5f);
        isMoving = false;
    }

    /// <summary>
    /// Returns blocks with a random scatter movement.
    /// </summary>
    private IEnumerator ReturnBlocksWithScatter()
    {
        isMoving = true;
        Debug.Log("[BlockSummoner] Returning blocks with scatter effect.");

        foreach (var block in blocks)
        {
            if (originalPositions.ContainsKey(block))
            {
                // Random scatter direction
                Vector3 scatterOffset = Random.insideUnitSphere * scatterRadius;
                scatterOffset.y = Mathf.Abs(scatterOffset.y); // make sure they go slightly upward visually

                // Random delay before starting return
                float randomDelay = Random.Range(0f, scatterDelayRange);

                StartCoroutine(ScatterAndReturn(block, scatterOffset, randomDelay));
            }
        }

        yield return new WaitForSeconds(1.5f + scatterDelayRange);
        isPlaced = false;
        isMoving = false;
    }

    /// <summary>
    /// Makes the block move to a random offset and then back to its original position.
    /// </summary>
    private IEnumerator ScatterAndReturn(Transform block, Vector3 scatterOffset, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 midTarget = block.position + scatterOffset;

        // Move to scattered position
        yield return MoveBlockTo(block, midTarget);

        // Small pause before returning
        yield return new WaitForSeconds(0.2f);

        // Move back to original
        if (originalPositions.ContainsKey(block))
            yield return MoveBlockTo(block, originalPositions[block]);
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

    private IEnumerator DestroyWhenDone(ParticleSystem ps)
    {
        yield return new WaitUntil(() => !ps.IsAlive(true));
        Destroy(ps.gameObject);
    }
}
