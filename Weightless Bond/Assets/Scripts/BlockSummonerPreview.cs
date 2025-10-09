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

    [Header("References")]
    public Camera playerCamera;
    public GameObject ghostBlockPrefab;
    public ParticleSystem summonEffectPrefab;
    public Material summonMaterial;           // ✨ Material to apply temporarily
    public Renderer targetRenderer;           // 🎯 Optional: extra object whose material changes too

    private List<Transform> blocks = new List<Transform>();
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private List<GameObject> ghostBlocks = new List<GameObject>();
    private Dictionary<Transform, Material[]> originalBlockMats = new Dictionary<Transform, Material[]>();
    private Material[] originalTargetMats; // 🧩 for the assigned targetRenderer

    private bool isPreviewing = false;
    private bool isPlaced = false;
    private bool isMoving = false;

    private Vector3[] gridPositions;
    private Vector3 targetPoint;

    void Start()
    {
        // Cache blocks & their original positions/materials
        GameObject[] foundBlocks = GameObject.FindGameObjectsWithTag("SummonBlock");
        foreach (var obj in foundBlocks)
        {
            Transform block = obj.transform;
            blocks.Add(block);
            originalPositions[block] = block.position;

            MeshRenderer rend = block.GetComponent<MeshRenderer>();
            if (rend != null)
                originalBlockMats[block] = rend.materials;
        }

        // Cache original materials of targetRenderer
        if (targetRenderer != null)
            originalTargetMats = targetRenderer.materials;

        Debug.Log($"[BlockSummoner] Found {blocks.Count} summonable blocks.");
    }

    void Update()
    {
        if (Input.GetKeyDown(summonKey) && !isMoving)
        {
            if (!isPreviewing && !isPlaced)
                ShowPreview();
            else if (isPreviewing)
                StartCoroutine(SummonBlocks());
            else if (isPlaced)
                StartCoroutine(ReturnBlocks());
        }

        if (isPreviewing)
            UpdatePreviewPosition();
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

        // 🌟 Apply summon material to blocks + targetRenderer
        ApplySummonMaterial();

        // Play summon effect
        float effectDuration = 0f;
        if (summonEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(summonEffectPrefab, targetPoint, Quaternion.identity);
            effect.Play();
            effectDuration = effect.main.duration + effect.main.startLifetime.constantMax;
            StartCoroutine(DestroyWhenDone(effect));
        }

        // Move blocks to grid
        int count = Mathf.Min(blocks.Count, gridPositions.Length);
        for (int i = 0; i < count; i++)
            StartCoroutine(MoveBlockTo(blocks[i], gridPositions[i]));

        // ⏳ Wait for particle duration, then revert
        if (effectDuration > 0)
            yield return new WaitForSeconds(effectDuration);
        else
            yield return new WaitForSeconds(1.5f);

        RestoreOriginalMaterials();
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
        isPlaced = false;
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

    private IEnumerator DestroyWhenDone(ParticleSystem ps)
    {
        yield return new WaitUntil(() => !ps.IsAlive(true));
        Destroy(ps.gameObject);
    }

    // 🧩 Apply glowing summon material to blocks and target renderer
    private void ApplySummonMaterial()
    {
        if (summonMaterial == null) return;

        // Change all summon blocks
        foreach (var block in blocks)
        {
            var rend = block.GetComponent<MeshRenderer>();
            if (rend == null) continue;

            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = summonMaterial;
            rend.materials = mats;
        }

        // Change target renderer
        if (targetRenderer != null)
        {
            Material[] mats = new Material[targetRenderer.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = summonMaterial;
            targetRenderer.materials = mats;
        }

        Debug.Log("[BlockSummoner] Applied summon material to blocks + target renderer.");
    }

    // 🧩 Restore original materials
    private void RestoreOriginalMaterials()
    {
        // Restore blocks
        foreach (var block in blocks)
        {
            var rend = block.GetComponent<MeshRenderer>();
            if (rend != null && originalBlockMats.ContainsKey(block))
                rend.materials = originalBlockMats[block];
        }

        // Restore target renderer
        if (targetRenderer != null && originalTargetMats != null)
            targetRenderer.materials = originalTargetMats;

        Debug.Log("[BlockSummoner] Restored original materials.");
    }
}
