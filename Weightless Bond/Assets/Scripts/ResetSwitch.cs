using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class ResetSwitch : MonoBehaviour
{
    [Header("Target")]
    public ResettableObject targetObject; // assign in Inspector

    [Header("Switch Animation")]
    public float pressDepth = 0.05f;   // how far down to move (negative Y)
    public float pressDuration = 0.1f; // how long to stay pressed before returning

    [Header("Sound Settings")]
    [Tooltip("Enable/disable button sound effect")]
    public bool playSfx = true;

    [Tooltip("Sound played when button is pressed")]
    public AudioClip buttonSfx;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private Vector3 originalPosition;
    private bool isPressed = false;
    private AudioSource audioSource;

#if UNITY_EDITOR
    // Auto-assign in Editor if missing (no dragging needed)
    private const string EditorClipPath = "Assets/Audio/Button.mp3";
    void OnValidate()
    {
        if (buttonSfx == null)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EditorClipPath);
            if (clip != null)
            {
                buttonSfx = clip;
                EditorUtility.SetDirty(this);
            }
        }
    }
#endif

    void Start()
    {
        originalPosition = transform.localPosition;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void ActivateSwitch()
    {
        if (isPressed) return; // prevent spamming
        StartCoroutine(PressAnimation());

        if (targetObject != null)
        {
            targetObject.ResetObject();
            Debug.Log("Switch activated: Object reset!");
        }

        if (playSfx && buttonSfx != null)
        {
            audioSource.volume = sfxVolume;
            audioSource.PlayOneShot(buttonSfx, sfxVolume);
        }
    }

    private IEnumerator PressAnimation()
    {
        isPressed = true;
        transform.localPosition = originalPosition + Vector3.down * pressDepth;

        yield return new WaitForSeconds(pressDuration);

        transform.localPosition = originalPosition;
        isPressed = false;
    }
}
