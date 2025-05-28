using UnityEngine;

public abstract class BaseModifier : MonoBehaviour
{
    public                     string ModifierId => modifierId;
    [SerializeField] protected string modifierId;

    public abstract void Initialize(BaseModifierSettings modifierSettings);
}