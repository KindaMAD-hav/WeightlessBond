using UnityEngine;

public class BGMTrigger : MonoBehaviour
{
    public AudioClip musicClip;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && BGMHandler.Instance != null)
        {
            BGMHandler.Instance.PlayMusic(musicClip);
        }
    }
}
