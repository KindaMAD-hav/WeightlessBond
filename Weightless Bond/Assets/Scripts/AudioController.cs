using UnityEngine;
using UnityEngine.Audio; // only needed if you assign a mixer
using System;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [Header("Master Control")]
    [Tooltip("Optional: Assign your AudioMixer. If null, falls back to AudioListener.volume.")]
    public AudioMixer masterMixer;

    [Tooltip("Exposed parameter on the mixer controlling master volume (in dB).")]
    public string mixerVolumeParam = "MasterVolume";

    [Tooltip("Default volume if no PlayerPrefs found.")]
    [Range(0f, 1f)] public float defaultVolume = 0.8f;

    private const string PREF_KEY = "MasterVolume";
    private float volume01 = 1f; // 0..1

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved volume (or default)
        float saved = PlayerPrefs.GetFloat(PREF_KEY, defaultVolume);
        SetVolume(saved, applyNow: true, save: false);
    }

    /// <summary>
    /// Set master volume in [0..1]. Saves to PlayerPrefs.
    /// </summary>
    public void SetVolume(float v) => SetVolume(v, applyNow: true, save: true);

    /// <summary>
    /// Current master volume in [0..1]
    /// </summary>
    public float GetVolume() => volume01;

    private void SetVolume(float v, bool applyNow, bool save)
    {
        volume01 = Mathf.Clamp01(v);

        if (applyNow)
        {
            if (masterMixer != null && !string.IsNullOrEmpty(mixerVolumeParam))
            {
                // Convert [0..1] to decibels smoothly (log scale). 0 -> -80 dB (mute), 1 -> 0 dB
                float dB = (volume01 <= 0.0001f) ? -80f : Mathf.Log10(volume01) * 20f;
                masterMixer.SetFloat(mixerVolumeParam, dB);
            }
            else
            {
                // Fallback: affects all AudioSources globally
                AudioListener.volume = volume01;
            }
        }

        if (save)
            PlayerPrefs.SetFloat(PREF_KEY, volume01);
    }
}
