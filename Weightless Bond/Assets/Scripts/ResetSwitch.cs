using UnityEngine;

public class ResetSwitch : MonoBehaviour
{
    public ResettableObject targetObject; // assign in Inspector

    public void ActivateSwitch()
    {
        if (targetObject != null)
        {
            targetObject.ResetObject();
            Debug.Log("Switch activated: Object reset!");
        }
    }
}
