using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Tooltip("Second sound to play after (optionally when door finishes)")]
    public AudioClip secondSfx;
    [Range(0f, 2f)] public float secondSfxVolume = 1f;

    [Header("Performance")]
    public bool preloadClipsOnStart = true;
    public bool secondSfxOnDoorComplete = true;
    public bool debugLogs = true;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isOpening = false;
    private bool hasOpened = false;
    private bool secondQueued = false;

    private AudioSource audioSource;

    // === Static latches per scene ===
    private static int s_lastSceneIndex = -1;
    private static bool s_firstPlayedThisScene = false;
    private static bool s_secondPlayedThisScene = false;

    private void Awake()
    {
        int idx = SceneManager.GetActiveScene().buildIndex;
        if (idx != s_lastSceneIndex)
        {
            s_lastSceneIndex = idx;
            s_firstPlayedThisScene = false;
            s_secondPlayedThisScene = false;
        }
    }

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

        if (preloadClipsOnStart)
        {
            PreloadClip(doorOpenSfx);
            PreloadClip(secondSfx);
        }
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

            bool reached = Vector3.Distance(targetObject.position, targetPos) < 0.01f;
            if (reached)
            {
                targetObject.position = targetPos;
                isOpening = false;
                hasOpened = true;

                if (playDoorSfx && !secondQueued && secondSfxOnDoorComplete && !s_secondPlayedThisScene)
                {
                    PlaySecondSfx();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || targetObject == null || hasOpened) return;

        isOpening = true;
        if (debugLogs) Debug.Log(targetObject.name + " is opening smoothly!");

        if (playDoorSfx && !s_firstPlayedThisScene)
        {
            s_firstPlayedThisScene = true;

            if (doorOpenSfx != null)
            {
                audioSource.volume = doorOpenVolume;
                audioSource.PlayOneShot(doorOpenSfx, doorOpenVolume);

                if (secondSfx != null && !secondSfxOnDoorComplete && !s_secondPlayedThisScene)
                {
                    Invoke(nameof(PlaySecondSfx), doorOpenSfx.length);
                    secondQueued = true;
                }
            }
            else if (secondSfx != null && !secondSfxOnDoorComplete && !s_secondPlayedThisScene)
            {
                PlaySecondSfx();
            }
        }
    }

    private void PlaySecondSfx()
    {
        if (secondQueued) return;
        if (secondSfx == null) return;

        audioSource.volume = secondSfxVolume;
        audioSource.PlayOneShot(secondSfx, secondSfxVolume);

        s_secondPlayedThisScene = true;
        secondQueued = true;
    }

    private static void PreloadClip(AudioClip clip)
    {
        if (clip == null) return;
        if (clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();
    }
}
