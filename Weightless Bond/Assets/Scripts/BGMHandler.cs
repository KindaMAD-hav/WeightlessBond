using UnityEngine;
using System.Collections;

public class BGMHandler : MonoBehaviour
{
    public static BGMHandler Instance;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource inactiveSource;

    [Header("Settings")]
    public float fadeDuration = 2f; // seconds for crossfade
    public AudioClip defaultTrack;  // starting track

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Get two audio sources (make sure they exist on this object)
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            Debug.LogError("BGMHandler needs 2 AudioSources on the same GameObject.");
            return;
        }

        sourceA = sources[0];
        sourceB = sources[1];
        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    void Start()
    {
        // Play default track if assigned
        if (defaultTrack != null)
        {
            activeSource.clip = defaultTrack;
            activeSource.loop = true;
            activeSource.volume = 1f;
            activeSource.Play();
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        // If the requested clip is already playing → do nothing
        if (activeSource.isPlaying && activeSource.clip == clip)
            return;

        // Stop any ongoing fade
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Setup inactive source with new clip
        inactiveSource.clip = clip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        // Start smooth crossfade
        fadeCoroutine = StartCoroutine(Crossfade());
    }

    private IEnumerator Crossfade()
    {
        float time = 0f;

        float startVolActive = activeSource.volume;
        float startVolInactive = inactiveSource.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            activeSource.volume = Mathf.Lerp(startVolActive, 0f, t);
            inactiveSource.volume = Mathf.Lerp(startVolInactive, 1f, t);

            yield return null;
        }

        // Finalize swap
        activeSource.Stop();

        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        activeSource.volume = 1f;
        inactiveSource.volume = 0f;

        fadeCoroutine = null;
    }
}
