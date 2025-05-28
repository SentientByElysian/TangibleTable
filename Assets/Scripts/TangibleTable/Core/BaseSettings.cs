using UnityEngine;
using UTool.TabSystem;

[HasTabField]
public abstract class BaseModifierSettings : MonoBehaviour
{
    public string ModifierId => modifierId;
    [SerializeField] protected string modifierId;
}