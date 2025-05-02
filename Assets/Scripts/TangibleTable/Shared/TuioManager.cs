using UnityEngine;
using TangibleTable.SingleDisplay;
using TangibleTable.DualDisplay;
using UTool.TabSystem;
using System.Collections.Generic;

namespace TangibleTable.Shared
{
    /// <summary>
    /// Central manager for TUIO object behavior and settings.
    /// Handles smoothing, stabilization, cursor settings, and more.
    /// Attach this to any game object to manage TUIO objects globally.
    /// </summary>
    [HasTabField]
    public class TuioManager : MonoBehaviour
    {
        [Header("Auto-Apply Settings")]
        [SerializeField] private bool _addToAllTuioObjects = true;
        
        [SerializeField] private DualDisplayTuio11Visualizer _dualVisualizer;
        
        [SerializeField] private SingleDisplayTuio11Visualizer _singleVisualizer;
        
        [Header("Stabilization Settings")]
        [SerializeField] private StabilizationPreset _preset = StabilizationPreset.Medium;
        
        [Header("Custom Settings")]
        [TabField]
        [SerializeField] private bool _useRotationStabilizer = true;
        
        [TabField]
        [SerializeField, Range(0.1f, 10f)]
        private float _rotationStabilityThreshold = 2.5f;
        
        [TabField]
        [SerializeField, Range(1, 10)]
        private int _stableFramesRequired = 3;
        
        [Header("Visual Smoothing")]
        [TabField]
        [SerializeField] private bool _useVisualSmoothing = true;
        
        [TabField]
        [SerializeField, Range(0.01f, 1f)]
        private float _rotationSmoothSpeed = 0.15f;
        
        [TabField]
        [SerializeField, Range(0.01f, 1f)]
        private float _positionSmoothSpeed = 0.25f;
        
        [Header("Cursor Offset")]
        [TabField]
        [SerializeField] private Vector2 _cursorOffset = Vector2.zero;
        
        [SerializeField] private bool _applyOffsetToExistingCursors = true;
        
        // Static property to be accessed by all TUIO objects
        public static Vector2 GlobalCursorOffset { get; private set; } = Vector2.zero;
        
        // List of all active TUIO cursors for cursor offset updates
        private List<CustomTuio11Behaviour> _activeTuioCursors = 
            new List<CustomTuio11Behaviour>();
        
        public enum StabilizationPreset
        {
            Light,
            Medium,
            Strong,
            Extreme,
            Custom
        }
        
        private void OnEnable()
        {
            if (_addToAllTuioObjects)
            {
                // Hook into object creation events from the visualizers
                if (_dualVisualizer != null)
                {
                    _dualVisualizer.OnObjectCreated += ConfigureStabilization;
                    _dualVisualizer.OnCursorCreated += ConfigureCursor;
                }
                
                if (_singleVisualizer != null)
                {
                    _singleVisualizer.OnObjectCreated += ConfigureStabilization;
                    _singleVisualizer.OnCursorCreated += ConfigureCursor;
                }
            }
            
            // Initialize global cursor offset
            GlobalCursorOffset = _cursorOffset;
        }
        
        private void OnDisable()
        {
            // Unhook from object creation events
            if (_dualVisualizer != null)
            {
                _dualVisualizer.OnObjectCreated -= ConfigureStabilization;
                _dualVisualizer.OnCursorCreated -= ConfigureCursor;
            }
            
            if (_singleVisualizer != null)
            {
                _singleVisualizer.OnObjectCreated -= ConfigureStabilization;
                _singleVisualizer.OnCursorCreated -= ConfigureCursor;
            }
        }
        
        private void Update()
        {
            // Check if cursor offset has changed
            if (GlobalCursorOffset != _cursorOffset)
            {
                GlobalCursorOffset = _cursorOffset;
                
                // Apply to existing cursors if enabled
                if (_applyOffsetToExistingCursors)
                {
                    UpdateAllCursorOffsets();
                }
            }
        }
        
        /// <summary>
        /// Apply cursor offset to all tracked TUIO cursors
        /// </summary>
        private void UpdateAllCursorOffsets()
        {
            // Remove any null references
            _activeTuioCursors.RemoveAll(obj => obj == null);
            
            // Apply cursor offset to all active cursors
            foreach (var cursor in _activeTuioCursors)
            {
                if (cursor != null)
                {
                    cursor.SetCursorOffset(GlobalCursorOffset);
                }
            }
        }
        
        /// <summary>
        /// Configures offset settings for a new TUIO cursor
        /// </summary>
        private void ConfigureCursor(GameObject cursorObject)
        {
            if (cursorObject == null) return;
            
            // Get the CustomTuio11Behaviour component
            CustomTuio11Behaviour behaviour = cursorObject.GetComponent<CustomTuio11Behaviour>();
            
            if (behaviour == null) return;
            
            // Add to tracked cursors for cursor offset updates
            if (!_activeTuioCursors.Contains(behaviour))
            {
                _activeTuioCursors.Add(behaviour);
            }
            
            // Set the cursor offset
            behaviour.SetCursorOffset(GlobalCursorOffset);
            
            // Set cursor visibility based on TuioDebugger settings
            bool showCursors = TuioDebugger.Instance != null ? TuioDebugger.Instance.ShowUIPanel : true;
            behaviour.SetCursorVisibility(showCursors);
        }
        
        /// <summary>
        /// Configures stabilization settings for a new TUIO object
        /// </summary>
        private void ConfigureStabilization(GameObject tuioObject)
        {
            if (tuioObject == null) return;
            
            // Get the CustomTuio11Behaviour component
            CustomTuio11Behaviour behaviour = tuioObject.GetComponent<CustomTuio11Behaviour>();
            
            if (behaviour == null) return;
            
            // Get current settings based on preset
            bool useStabilizer = _useRotationStabilizer;
            float threshold = _rotationStabilityThreshold;
            int frames = _stableFramesRequired;
            bool useSmoothing = _useVisualSmoothing;
            float rotSmoothSpeed = _rotationSmoothSpeed;
            float posSmoothSpeed = _positionSmoothSpeed;
            
            // Apply preset settings (except for Custom)
            switch (_preset)
            {
                case StabilizationPreset.Light:
                    useStabilizer = TuioSmoothingPresets.Light.UseStabilizer;
                    threshold = TuioSmoothingPresets.Light.StabilityThreshold;
                    frames = TuioSmoothingPresets.Light.StableFramesRequired;
                    useSmoothing = TuioSmoothingPresets.Light.UseVisualSmoothing;
                    rotSmoothSpeed = TuioSmoothingPresets.Light.RotationSmoothSpeed;
                    posSmoothSpeed = TuioSmoothingPresets.Light.PositionSmoothSpeed;
                    break;
                    
                case StabilizationPreset.Medium:
                    useStabilizer = TuioSmoothingPresets.Medium.UseStabilizer;
                    threshold = TuioSmoothingPresets.Medium.StabilityThreshold;
                    frames = TuioSmoothingPresets.Medium.StableFramesRequired;
                    useSmoothing = TuioSmoothingPresets.Medium.UseVisualSmoothing;
                    rotSmoothSpeed = TuioSmoothingPresets.Medium.RotationSmoothSpeed;
                    posSmoothSpeed = TuioSmoothingPresets.Medium.PositionSmoothSpeed;
                    break;
                    
                case StabilizationPreset.Strong:
                    useStabilizer = TuioSmoothingPresets.Strong.UseStabilizer;
                    threshold = TuioSmoothingPresets.Strong.StabilityThreshold;
                    frames = TuioSmoothingPresets.Strong.StableFramesRequired;
                    useSmoothing = TuioSmoothingPresets.Strong.UseVisualSmoothing;
                    rotSmoothSpeed = TuioSmoothingPresets.Strong.RotationSmoothSpeed;
                    posSmoothSpeed = TuioSmoothingPresets.Strong.PositionSmoothSpeed;
                    break;
                    
                case StabilizationPreset.Extreme:
                    useStabilizer = TuioSmoothingPresets.Extreme.UseStabilizer;
                    threshold = TuioSmoothingPresets.Extreme.StabilityThreshold;
                    frames = TuioSmoothingPresets.Extreme.StableFramesRequired;
                    useSmoothing = TuioSmoothingPresets.Extreme.UseVisualSmoothing;
                    rotSmoothSpeed = TuioSmoothingPresets.Extreme.RotationSmoothSpeed;
                    posSmoothSpeed = TuioSmoothingPresets.Extreme.PositionSmoothSpeed;
                    break;
                    
                case StabilizationPreset.Custom:
                    // Use the values set in the inspector
                    break;
            }
            
            // Set stabilization values using reflection
            var useStabilizerField = behaviour.GetType().GetField("_useRotationStabilizer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var thresholdField = behaviour.GetType().GetField("_rotationStabilityThreshold", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var framesField = behaviour.GetType().GetField("_stableFramesRequired", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var useVisualSmoothingField = behaviour.GetType().GetField("_useVisualSmoothing", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var rotationSmoothSpeedField = behaviour.GetType().GetField("_rotationSmoothSpeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            var positionSmoothSpeedField = behaviour.GetType().GetField("_positionSmoothSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (useStabilizerField != null)
                useStabilizerField.SetValue(behaviour, useStabilizer);
                
            if (thresholdField != null)
                thresholdField.SetValue(behaviour, threshold);
                
            if (framesField != null)
                framesField.SetValue(behaviour, frames);
                
            if (useVisualSmoothingField != null)
                useVisualSmoothingField.SetValue(behaviour, useSmoothing);
                
            if (rotationSmoothSpeedField != null)
                rotationSmoothSpeedField.SetValue(behaviour, rotSmoothSpeed);
                
            if (positionSmoothSpeedField != null)
                positionSmoothSpeedField.SetValue(behaviour, posSmoothSpeed);
        }
    }
} 