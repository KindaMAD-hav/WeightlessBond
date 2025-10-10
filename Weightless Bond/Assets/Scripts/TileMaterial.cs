using UnityEngine;

public class TileMaterial : MonoBehaviour
{
    public Renderer targetRenderer;
    public Vector2 tiling = new Vector2(2, 2);

    void Start()
    {
        if (targetRenderer != null)
        {
            // For most GLTFUtility materials, "_BaseMap" is used instead of "_MainTex"
            if (targetRenderer.material.HasProperty("_BaseMap"))
                targetRenderer.material.SetTextureScale("_BaseMap", tiling);
            else if (targetRenderer.material.HasProperty("_MainTex"))
                targetRenderer.material.SetTextureScale("_MainTex", tiling);
        }
    }
}
