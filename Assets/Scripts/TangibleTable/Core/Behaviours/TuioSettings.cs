using UnityEngine;
using UTool.TabSystem;

namespace TangibleTable.Core.Behaviours
{
    /// <summary>
    /// Settings for TUIO stabilization, smoothing, and offsets.
    /// This component stores configuration settings for TUIO tracking.
    /// </summary>
    [HasTabField]
    public class TuioSettings : MonoBehaviour
    {
        #region Common Settings
        
        [Header("Position Smoothing")]
        [TabField]
        [Tooltip("Speed of position interpolation (lower = smoother but slower)")]
        [SerializeField, Range(0.01f, 1f)] private float _positionSmoothSpeed = 0.25f;
        
        [Header("Position Offset")]
        [TabField]
        [Tooltip("Position offset applied to TUIO elements")]
        [SerializeField] private Vector2 _positionOffset = Vector2.zero;
        
        #endregion
        
        #region Object-Specific Settings (Physical Markers)
        
        [Header("Object Rotation Stabilization")]
        [TabField]
        [Tooltip("Minimum angle change in degrees required to update rotation")]
        [SerializeField, Range(0.1f, 10f)] private float _objectRotationThreshold = 2.5f;
        
        [TabField]
        [Tooltip("Speed of rotation interpolation (lower = smoother but slower)")]
        [SerializeField, Range(0.01f, 1f)] private float _objectRotationSmoothSpeed = 0.15f;
        
        #endregion
        
        #region Cursor-Specific Settings
        
        [Header("Cursor Offset Settings")]
        [TabField]
        [Tooltip("Enable to apply offset in pixel space, disable to apply in normalized 0-1 space")]
        [SerializeField] private bool _cursorUsePixelOffset = true;
        
        #endregion
        
        /// <summary>
        /// Get effective cursor offset in screen pixels
        /// </summary>
        public Vector2 GetEffectiveCursorOffset()
        {
            if (_cursorUsePixelOffset)
            {
                return _positionOffset;
            }
            else
            {
                // Convert from normalized 0-1 space to pixel space
                return new Vector2(
                    _positionOffset.x * Screen.width,
                    _positionOffset.y * Screen.height
                );
            }
        }
        
        /// <summary>
        /// Configure a TuioStabilizer for an object
        /// </summary>
        public void ConfigureObjectStabilizer(TuioVisualizer visualizer)
        {
            if (visualizer == null) return;
            
            visualizer.ConfigureStabilizer(
                _objectRotationThreshold,
                _objectRotationSmoothSpeed,
                _positionSmoothSpeed
            );
            
            visualizer.SetStabilizerEnabled(true); 
            visualizer.SetPositionOffset(_positionOffset);
        }
        
        /// <summary>
        /// Configure a TuioStabilizer for a cursor
        /// </summary>
        public void ConfigureCursorStabilizer(TuioVisualizer visualizer)
        {
            if (visualizer == null) return;
            
            // For cursors, we only use position smoothing, not rotation
            visualizer.ConfigureStabilizer(
                0f,    // No threshold needed
                0f,    // No rotation smoothing
                _positionSmoothSpeed
            );
            
            visualizer.SetStabilizerEnabled(true); 
            visualizer.SetPositionOffset(GetEffectiveCursorOffset());
        }
    }
} 