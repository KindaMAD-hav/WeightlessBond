using UnityEngine;

public class BGMTrigger : MonoBehaviour
{
    [Header("Music Settings")]
    public AudioClip musicClip;

    [Header("Script Control")]
    [Tooltip("The same script that was enabled by the door. It will be disabled when the player enters this trigger.")]
    public MonoBehaviour scriptToDisable;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Play music if the handler exists
            if (BGMHandler.Instance != null && musicClip != null)
            {
                BGMHandler.Instance.PlayMusic(musicClip);
            }

            // Disable the target script
            if (scriptToDisable != null && scriptToDisable.enabled)
            {
                scriptToDisable.enabled = false;
                Debug.Log($"Disabled script: {scriptToDisable.GetType().Name}");
            }
        }
    }
}
