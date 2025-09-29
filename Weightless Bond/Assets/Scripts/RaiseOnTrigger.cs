using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RaiseOnTrigger : MonoBehaviour
{
    [Header("Target Object to Move")]
    public Transform targetObject;

    [Header("Raise Settings")]
    public float raiseAmount = 5f;
    public float raiseSpeed = 2f; // units per second

    [Header("Audio Settings")]
    public bool playDoorSfx = true;

    [Tooltip("First sound when door starts opening")]
    public AudioClip doorOpenSfx;
    [Range(0f, 1f)] public float doorOpenVolume = 1f;

    [Tooltip("Second sound after first finishes")]
    public AudioClip secondSfx;
    [Range(0f, 2f)] public float secondSfxVolume = 1f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isOpening = false;
    private bool hasOpened = false;

    private AudioSource audioSource;

    private void Start()
    {
        if (targetObject != null)
        {
            startPos = targetObject.position;
            targetPos = startPos + Vector3.up * raiseAmount;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (isOpening && targetObject != null)
        {
            targetObject.position = Vector3.MoveTowards(
                targetObject.position,
                targetPos,
                raiseSpeed * Time.deltaTime
            );

            if (Vector3.Distance(targetObject.position, targetPos) < 0.01f)
            {
                targetObject.position = targetPos;
                isOpening = false;
                hasOpened = true;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetObject != null && !hasOpened)
        {
            isOpening = true;

            Debug.Log($"{targetObject.name} is opening smoothly!");

            if (playDoorSfx)
            {
                // Play first clip and then queue the second
                if (doorOpenSfx != null)
                {
                    audioSource.volume = doorOpenVolume;
                    audioSource.PlayOneShot(doorOpenSfx, doorOpenVolume);

                    if (secondSfx != null)
                        StartCoroutine(PlaySecondSfxAfterDelay(doorOpenSfx.length));
                }
                else if (secondSfx != null) // if only second assigned
                {
                    audioSource.volume = secondSfxVolume;
                    audioSource.PlayOneShot(secondSfx, secondSfxVolume);
                }
            }
        }
    }

    private System.Collections.IEnumerator PlaySecondSfxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        audioSource.volume = secondSfxVolume;
        audioSource.PlayOneShot(secondSfx, secondSfxVolume);
    }
}
