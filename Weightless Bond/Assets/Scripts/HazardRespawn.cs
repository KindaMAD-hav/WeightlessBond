using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class HazardRespawn : MonoBehaviour
{
    [Header("Hazard Visual Settings")]
    [Tooltip("Material to apply to the generated cube.")]
    public Material cubeMaterial;

    private void Start()
    {
        // Disable all mesh renderers on this object
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in meshRenderers)
        {
            mr.enabled = false;
        }

        // Get all BoxColliders on this object
        BoxCollider[] colliders = GetComponentsInChildren<BoxCollider>();

        foreach (BoxCollider box in colliders)
        {
            // Create a new cube to visualize the collider
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"{gameObject.name}_VisualCube";
            cube.transform.SetParent(box.transform, false);

            // Match collider's size and position
            cube.transform.localPosition = box.center;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = box.size;

            // Assign material if provided
            var cubeRenderer = cube.GetComponent<MeshRenderer>();
            if (cubeMaterial != null)
                cubeRenderer.material = cubeMaterial;

            // Remove collider from the primitive
            Destroy(cube.GetComponent<BoxCollider>());
        }
    }
}
