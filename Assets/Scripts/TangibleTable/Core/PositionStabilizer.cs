using UnityEngine;

public class PositionStabilizer : BaseModifier
{
    private PositionStabilizerSettings settings;
    private RectTransform              rectTransform;
    private Vector2                    target, current, last;
    private float                      velocity;
    private bool                       initialized;
    
    public override void Initialize(BaseModifierSettings modifierSettings)
    {
        settings = (PositionStabilizerSettings)modifierSettings;
        rectTransform = GetComponent<RectTransform>();
    }
    
    public Vector2 SetTarget(Vector2 position)
    {
        if (!settings?.enabled ?? true)
        {
            Vector2 finalPos = position + settings.offset;
            rectTransform.anchoredPosition = finalPos;
            return finalPos;
        }
        
        target = position + settings.offset;
        
        if (initialized)
            velocity = Vector2.Distance(position, last) / Time.deltaTime;
        
        last = position;
        
        if (!initialized)
        {
            current = target;
            rectTransform.anchoredPosition = current;
            initialized = true;
            return current;
        }
        
        return UpdatePosition();
    }
    
    public Vector2 GetCurrent() => current;
    
    private Vector2 UpdatePosition()
    {
        float speed = settings.smoothSpeed;
        if (velocity > settings.velocityThreshold)
            speed *= settings.fastMovementMultiplier;
        
        current = Vector2.Lerp(current, target, Mathf.Clamp01(speed) * Time.deltaTime * 60f);
        rectTransform.anchoredPosition = current;
        return current;
    }
    
    private void Update()
    {
        if (initialized && settings?.enabled == true)
            UpdatePosition();
    }
}