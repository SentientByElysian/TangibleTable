using UnityEngine;

namespace TangibleTable.Core.Behaviours.Pucks.Extended
{
    /// <summary>
    /// A TuioPuck subclass that controls rotation based on the physical marker's orientation.
    /// The object will immediately adopt the exact orientation of the physical marker.
    /// </summary>
    public class DeltaRotateablePuck : TuioPuck
    {
        [Header("Rotation Settings")]
        [Tooltip("The transform to rotate. If null, will use this puck's transform")]
        [SerializeField] private Transform targetTransform;
        
        [Tooltip("Clockwise (traditional) or counter-clockwise rotation")]
        [SerializeField] private bool invertRotation = false;
        
        [Tooltip("Multiply the rotation by this value (1.0 = normal, 2.0 = double, 0.5 = half)")]
        [SerializeField] private float rotationMultiplier = 1.0f;
        
        [Tooltip("Rotation offset to apply (added to the final rotation)")]
        [SerializeField] private float rotationOffset = 0f;
        
        /// <summary>
        /// Called after initialization
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // If no target transform specified, use this transform
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
            
            // Apply initial marker orientation directly
            ApplyRotation();
        }
        
        /// <summary>
        /// Called every time the puck is updated with new TUIO data
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            if (targetTransform == null) return;
            
            // Apply rotation from the physical marker
            ApplyRotation();
        }
        
        /// <summary>
        /// Called when the puck is removed
        /// </summary>
        protected override void OnRemove()
        {
            base.OnRemove();
        }
        
        /// <summary>
        /// Apply rotation from the physical marker to the target transform
        /// </summary>
        private void ApplyRotation()
        {
            float rotationValue = rotation * rotationMultiplier;
            if (invertRotation)
            {
                rotationValue = -rotationValue;
            }
            
            // Add the offset and normalize to 0-360 range
            rotationValue = (rotationValue + rotationOffset) % 360f;
            
            // Apply rotation to the target transform (Z-axis rotation in 2D)
            targetTransform.rotation = Quaternion.Euler(0, 0, rotationValue);
            
            // Update state display in TuioVisualizer (useful for debugging)
            TuioBehaviour?.SetPuckState($"Angle: {rotation:F0}°");
        }
    }
} 