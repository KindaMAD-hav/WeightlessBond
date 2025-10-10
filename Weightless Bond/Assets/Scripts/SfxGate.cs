using UnityEngine;
using System.Collections;

/// <summary>
/// Global SFX sequencer that ensures one-shot clips never overlap across scripts.
/// Any request made while another clip is reserved will wait, then play.
/// </summary>
public static class SfxGate
{
    // Wall-clock schedule (unscaled) for when the gate is free again
    private static float s_busyUntil = 0f;

    /// <summary>
    /// Queue a one-shot clip to play on an AudioSource as soon as the gate is free.
    /// This reserves a timeslot immediately so late callers line up behind it.
    /// </summary>
    public static IEnumerator PlayQueued(MonoBehaviour host, AudioSource src, AudioClip clip, float volume)
    {
        if (host == null || src == null || clip == null) yield break;

        float now = Time.unscaledTime;
        float delay = Mathf.Max(0f, s_busyUntil - now);

        // Reserve this slot immediately so subsequent callers queue behind it
        float clipLen = clip.length / Mathf.Max(0.01f, src.pitch); // pitch-aware
        s_busyUntil = now + delay + clipLen;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // Finally play
        src.PlayOneShot(clip, Mathf.Clamp01(src.volume) * volume);
    }

    /// <summary>Returns true while the gate is currently busy.</summary>
    public static bool IsBusy => Time.unscaledTime < s_busyUntil;
}