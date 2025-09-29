using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;   // keep this alias

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

    [Range(0f, 1f)]
    [Tooltip("Maximum allowed volume for all background music.")]
    public float maxVolume = 0.25f;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

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
        if (defaultTrack != null)
        {
            activeSource.clip = defaultTrack;
            activeSource.loop = true;
            activeSource.volume = maxVolume;
            activeSource.Play();
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (activeSource.isPlaying && activeSource.clip == clip)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        inactiveSource.clip = clip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

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
            inactiveSource.volume = Mathf.Lerp(startVolInactive, maxVolume, t);

            yield return null;
        }

        activeSource.Stop();

        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        activeSource.volume = maxVolume;
        inactiveSource.volume = 0f;

        fadeCoroutine = null;
    }

    // === NEW: Fade out and stop all music ===
    public void FadeOutMusic(float duration = 2f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVol = activeSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            activeSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        activeSource.Stop();
        inactiveSource.Stop();
        activeSource.volume = 0f;
        inactiveSource.volume = 0f;

        fadeCoroutine = null;
    }
}
