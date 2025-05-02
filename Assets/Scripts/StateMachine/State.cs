using UnityEngine;

public abstract class State : MonoBehaviour
{
    public abstract void Enter();
    public abstract float Exit();
}