using TouchScript.InputSources;
using UnityEngine;

public class PointerRemapper : MonoBehaviour, ICoordinatesRemapper
{
    public TuioInput tuioInput;
    
    // Start is called before the first frame update
    void Start()
    {
        tuioInput.CoordinatesRemapper = this;
    }

    public Vector2 Remap(Vector2 input)
    {
        Debug.Log($"<color=cyan></color>");
        return new Vector2((input.x / 3)+1920, input.y);
    }
}
