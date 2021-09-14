using UnityEngine;

public class LookGetter : MonoBehaviour
{

    public PlayerInputs inputs;

    public Vector2 GetLook()
    {
        return inputs.GetLook();
    }
}
