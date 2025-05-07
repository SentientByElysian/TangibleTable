using UnityEngine;

namespace TangibleTable.Core.Behaviours.Pucks
{
    /// <summary>
    /// A TuioPuck subclass that controls rotation based on delta rotation.
    /// This allows physical markers to rotate objects relative to their initial placement angle.
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
        
        // Track delta rotation from initial placement
        private float _currentDeltaRotation = 0f;
        private float _referenceAngle = 0f;
        private bool _isReferenceSet = false;
        
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
            
            // Initialize reference angle
            _referenceAngle = rotation;
            _isReferenceSet = true;
            _currentDeltaRotation = 0f;
            
            // Apply initial rotation with offset
            if (targetTransform != null)
            {
                targetTransform.rotation = Quaternion.Euler(0, 0, rotationOffset);
            }
        }
        
        /// <summary>
        /// Called every time the puck is updated with new TUIO data
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            if (targetTransform == null || !_isReferenceSet) return;
            
            // Calculate delta rotation from reference angle
            _currentDeltaRotation = Mathf.DeltaAngle(_referenceAngle, rotation);
            
            // Make sure delta is in 0-360 range
            _currentDeltaRotation = (_currentDeltaRotation + 360f) % 360f;
            
            // Apply the rotation multiplier and inversion
            float rotationValue = _currentDeltaRotation * rotationMultiplier;
            if (invertRotation)
            {
                rotationValue = -rotationValue;
            }
            
            // Add the offset
            rotationValue += rotationOffset;
            
            // Apply rotation to the target transform (Z-axis rotation in 2D)
            targetTransform.rotation = Quaternion.Euler(0, 0, rotationValue);
            
            // Update state display in TuioVisualizer (useful for debugging)
            TuioBehaviour?.SetPuckState($"Delta: {_currentDeltaRotation:F0}°");
        }
        
        /// <summary>
        /// Called when the puck is removed
        /// </summary>
        protected override void OnRemove()
        {
            base.OnRemove();
        }
    }
} 