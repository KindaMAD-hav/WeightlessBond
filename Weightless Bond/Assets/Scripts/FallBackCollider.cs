using UnityEngine;

public class FallBackCollider : MonoBehaviour
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        // Store the original player position and rotation when the scene loads
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            originalPosition = player.transform.position;
            originalRotation = player.transform.rotation;
        }
        else
        {
            Debug.LogError("FallBackCollider: No GameObject with tag 'Player' found!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Get the PlayerHealth component
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                // Reset position and rotation
                other.transform.position = originalPosition;
                other.transform.rotation = originalRotation;

                // Reset health
                playerHealth.SetHealth(playerHealth.maxHealth);

                Debug.Log("FallBackCollider: Player reset to original position and health restored.");
            }
        }
    }
}
