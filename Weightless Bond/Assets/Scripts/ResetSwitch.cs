using UnityEngine;
using System.Collections;

public class ResetSwitch : MonoBehaviour
{
    [Header("Target")]
    public ResettableObject targetObject; // assign in Inspector

    [Header("Switch Animation")]
    public float pressDepth = 0.05f;   // how far down to move (negative Y)
    public float pressDuration = 0.1f; // how long to stay pressed before returning

    private Vector3 originalPosition;
    private bool isPressed = false;

    void Start()
    {
        originalPosition = transform.localPosition;
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
    }

    private IEnumerator PressAnimation()
    {
        isPressed = true;

        // Move down
        transform.localPosition = originalPosition + Vector3.down * pressDepth;

        yield return new WaitForSeconds(pressDuration);

        // Return to original
        transform.localPosition = originalPosition;

        isPressed = false;
    }
}
