using UnityEngine;
using UTool.TabSystem;

[HasTabField]
public class PositionStabilizerSettings : BaseModifierSettings
{
    [TabField]                    public bool    enabled                = true;
    [TabField]                    public Vector2 offset                 = Vector2.zero;
    [TabField] [Range(0.01f, 1f)] public float   smoothSpeed            = 0.3f;
    [TabField] [Range(0.1f, 5f)]  public float   velocityThreshold      = 1.5f;
    [TabField] [Range(0.1f, 3f)]  public float   fastMovementMultiplier = 2f;
}