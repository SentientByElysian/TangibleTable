using UnityEngine;
using System.Collections.Generic;
using TangibleTable.Core.Behaviours.Visualization;

namespace TangibleTable.Core.Behaviours.Visualization
{
    /// <summary>
    /// Provides advanced rotation and position smoothing for TUIO objects to reduce jitter.
    /// Uses the proven approach from the original CustomTuioBehaviour with improvements
    /// for adaptive position smoothing and accumulating small rotation changes.
    /// </summary>
    public class TuioStabilizer : MonoBehaviour
    {
        /// <summary>
        /// The stabilization mode to use
        /// </summary>
        public enum StabilizationMode
        {
            /// <summary>For physical objects with both position and rotation</summary>
            Object,
            /// <summary>For cursors/touches with position only</summary>
            Cursor
        }
        
        [Header("Stabilization Mode")]
        [SerializeField] private StabilizationMode _mode = StabilizationMode.Object;
        
        [Header("Rotation Stabilization")]
        [SerializeField] [Range(0.1f, 10f)] private float _rotationStabilityThreshold = 2.5f;
        
        [Header("Visual Smoothing")]
        [SerializeField] [Range(0.01f, 1f)] private float _rotationSmoothSpeed = 0.15f;
        [SerializeField] [Range(0.01f, 1f)] private float _positionSmoothSpeed = 0.25f;
        
        [Header("Advanced Settings")]
        [SerializeField] [Range(0.01f, 1f)] private float _adaptiveSpeedMinFactor = 0.2f;
        [SerializeField] [Range(1f, 5f)] private float _adaptiveSpeedMaxFactor = 3f;
        
        // Reference to the TuioVisualizer
        private TuioVisualizer _visualizer;
        
        // Rotation stabilization fields
        private float _lastStableRotation = 0f;
        private float _accumulatedRotation = 0f;
        private float _lastRawRotation = 0f;
        private bool _rotationInitialized = false;
        
        // Visual position and rotation (smoothed)
        private Vector3 _targetPosition;
        private float _targetRotation;
        private Vector3 _currentPosition;
        private float _currentRotation;
        private bool _initialized = false;
        
        // For detecting significant changes
        private Vector3 _lastPosition;
        private float _positionChangeRate = 0f;
        
        /// <summary>
        /// Get or set the stabilization mode
        /// </summary>
        public StabilizationMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                ApplyModeSettings();
            }
        }
        
        private void Awake()
        {
            _visualizer = GetComponent<TuioVisualizer>();
            if (_visualizer == null)
            {
                UnityEngine.Debug.LogError("TuioStabilizer requires a TuioVisualizer component on the same GameObject.");
                enabled = false;
            }
            
            // Apply mode-specific settings
            ApplyModeSettings();
        }
        
        /// <summary>
        /// Apply settings based on the current mode
        /// </summary>
        private void ApplyModeSettings()
        {
            if (_mode == StabilizationMode.Cursor)
            {
                // Cursors only use position smoothing
                _rotationSmoothSpeed = 0f;
            }
        }
        
        /// <summary>
        /// Configure stabilization settings
        /// </summary>
        public void Configure(float rotationThreshold, float rotationSpeed, float positionSpeed)
        {
            _rotationStabilityThreshold = rotationThreshold;
            _rotationSmoothSpeed = rotationSpeed;
            _positionSmoothSpeed = positionSpeed;
            
            // Make sure settings respect the mode
            ApplyModeSettings();
        }
        
        /// <summary>
        /// Process raw rotation angle from TUIO to get a stabilized value
        /// </summary>
        public float StabilizeRotation(float rawAngle)
        {
            // For cursors, don't use rotation stabilization
            if (_mode == StabilizationMode.Cursor)
                return rawAngle;
                
            // First-time initialization
            if (!_rotationInitialized)
            {
                _lastStableRotation = rawAngle;
                _lastRawRotation = rawAngle;
                _accumulatedRotation = 0f;
                _rotationInitialized = true;
                return rawAngle;
            }
            
            // Calculate the angle difference using shortest path
            float angleDifference = Mathf.DeltaAngle(_lastRawRotation, rawAngle);
            
            // Update last raw angle for next frame
            _lastRawRotation = rawAngle;
            
            if (Mathf.Abs(angleDifference) > _rotationStabilityThreshold)
            {
                // Significant change - accept it directly
                _lastStableRotation = rawAngle;
                _accumulatedRotation = 0f;
                return rawAngle;
            }
            else
            {
                // Small change - accumulate it
                _accumulatedRotation += angleDifference;
                
                // If accumulated change exceeds threshold, apply it
                if (Mathf.Abs(_accumulatedRotation) > _rotationStabilityThreshold)
                {
                    // Calculate new stable rotation
                    float newRotation = _lastStableRotation + _accumulatedRotation;
                    // Normalize to 0-360 range
                    newRotation = (newRotation + 360f) % 360f;
                    
                    _lastStableRotation = newRotation;
                    _accumulatedRotation = 0f;
                    return newRotation;
                }
                
                // Otherwise, keep using the last stable rotation
                return _lastStableRotation;
            }
        }
        
        /// <summary>
        /// Sets the target position and rotation for smooth movement
        /// </summary>
        public void SetTargets(Vector3 position, float rotation)
        {
            // Calculate positional change rate
            if (_initialized)
            {
                _positionChangeRate = Vector3.Distance(position, _lastPosition) / Time.deltaTime;
            }
            
            _lastPosition = position;
            _targetPosition = position;
            _targetRotation = rotation;
            
            // Initialize current values if they haven't been set
            if (!_initialized)
            {
                _currentPosition = position;
                _currentRotation = rotation;
                
                transform.position = position;
                transform.rotation = Quaternion.Euler(0, 0, rotation);
                
                _initialized = true;
            }
        }
        
        private void Update()
        {
            if (!_initialized)
                return;
            
            // Calculate adaptive position speed based on movement rate
            float posSpeedFactor = CalculateAdaptiveSpeed(_positionChangeRate, 10f);
            float adaptivePositionSpeed = _positionSmoothSpeed * posSpeedFactor;
            
            // For zero speed, apply position directly without smoothing
            if (_positionSmoothSpeed <= 0.001f)
            {
                _currentPosition = _targetPosition;
            }
            else
            {
                // Smooth and apply position
                _currentPosition = Vector3.Lerp(_currentPosition, _targetPosition, adaptivePositionSpeed);
            }
            
            transform.position = _currentPosition;
            
            // Smooth and apply rotation (for objects only)
            if (_mode == StabilizationMode.Object)
            {
                // For zero speed, apply rotation directly without smoothing
                if (_rotationSmoothSpeed <= 0.001f)
                {
                    _currentRotation = _targetRotation;
                }
                else 
                {
                    // Handle 360-degree wrapping when lerping between angles
                    float shortestAngle = Mathf.DeltaAngle(_currentRotation, _targetRotation);
                    _currentRotation = Mathf.Lerp(0, shortestAngle, _rotationSmoothSpeed) + _currentRotation;
                    
                    // Keep angle in 0-360 range
                    _currentRotation = (_currentRotation + 360f) % 360f;
                }
                
                // Apply the rotation
                transform.rotation = Quaternion.Euler(0, 0, _currentRotation);
            }
        }
        
        /// <summary>
        /// Calculate adaptive smoothing speed based on change rate
        /// </summary>
        private float CalculateAdaptiveSpeed(float changeRate, float maxExpectedRate)
        {
            // Normalize change rate against expected maximum
            float normalizedRate = Mathf.Clamp01(changeRate / maxExpectedRate);
            
            // Map to range between min and max factors
            return Mathf.Lerp(_adaptiveSpeedMinFactor, _adaptiveSpeedMaxFactor, normalizedRate);
        }
    }
} 