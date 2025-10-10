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

    [Header("Enable Script On Open")]
    [Tooltip("The script/component you want to enable when the door opens.")]
    public MonoBehaviour scriptToEnable;

    [Tooltip("If true, enable after the door has fully opened. If false, enable as soon as the door begins opening.")]
    public bool enableOnDoorComplete = true;

    [Tooltip("Optional delay (seconds) before enabling the script.")]
    public float enableDelay = 0f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool isOpening = false;
    private bool hasOpened = false;

    private bool secondQueued = false;
    private bool scriptEnableQueuedOrDone = false;

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

        // Ensure target script starts disabled
        if (scriptToEnable != null)
            scriptToEnable.enabled = false;
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

                // Enable script when door completes, if that's the chosen timing
                if (enableOnDoorComplete)
                    ScheduleEnableTargetScript();

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

        // Enable script when door starts, if that's the chosen timing
        if (!enableOnDoorComplete)
            ScheduleEnableTargetScript();

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

    // === Script enabling helpers ===
    private void ScheduleEnableTargetScript()
    {
        if (scriptEnableQueuedOrDone) return;

        if (enableDelay <= 0f)
        {
            EnableTargetScriptNow();
        }
        else
        {
            // Queue once
            scriptEnableQueuedOrDone = true;
            Invoke(nameof(EnableTargetScriptNow), enableDelay);
        }
    }

    private void EnableTargetScriptNow()
    {
        // If not queued via Schedule, mark it done here to avoid double-enabling attempts
        if (!scriptEnableQueuedOrDone) scriptEnableQueuedOrDone = true;

        if (scriptToEnable != null)
        {
            scriptToEnable.enabled = true;
            if (debugLogs) Debug.Log($"Enabled script: {scriptToEnable.GetType().Name}");
        }
        else if (debugLogs)
        {
            Debug.LogWarning("No script assigned to 'scriptToEnable'.");
        }
    }
}
