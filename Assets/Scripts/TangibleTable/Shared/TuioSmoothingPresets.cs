using UnityEngine;

namespace TangibleTable.Shared
{
    /// <summary>
    /// Static class containing predefined smoothing settings for TUIO objects.
    /// These presets define different levels of rotation stabilization and smoothing.
    /// </summary>
    public static class TuioSmoothingPresets
    {
        /// <summary>
        /// Structure defining a smoothing preset with all related settings
        /// </summary>
        public struct SmoothingPreset
        {
            public bool UseStabilizer;
            public float StabilityThreshold;
            public int StableFramesRequired;
            public bool UseVisualSmoothing;
            public float RotationSmoothSpeed;
            public float PositionSmoothSpeed;
        }
        
        /// <summary>
        /// Light smoothing - minimal stabilization for responsive applications
        /// </summary>
        public static SmoothingPreset Light => new SmoothingPreset
        {
            UseStabilizer = true,
            StabilityThreshold = 1.5f,
            StableFramesRequired = 2,
            UseVisualSmoothing = true,
            RotationSmoothSpeed = 0.25f,
            PositionSmoothSpeed = 0.35f
        };
        
        /// <summary>
        /// Medium smoothing - balanced stabilization between responsiveness and smoothness
        /// </summary>
        public static SmoothingPreset Medium => new SmoothingPreset
        {
            UseStabilizer = true,
            StabilityThreshold = 2.5f,
            StableFramesRequired = 3,
            UseVisualSmoothing = true,
            RotationSmoothSpeed = 0.15f,
            PositionSmoothSpeed = 0.25f
        };
        
        /// <summary>
        /// Strong smoothing - focused on stability over responsiveness
        /// </summary>
        public static SmoothingPreset Strong => new SmoothingPreset
        {
            UseStabilizer = true,
            StabilityThreshold = 4.0f,
            StableFramesRequired = 4,
            UseVisualSmoothing = true,
            RotationSmoothSpeed = 0.1f,
            PositionSmoothSpeed = 0.15f
        };
        
        /// <summary>
        /// Extreme smoothing - maximum stability, less responsive
        /// </summary>
        public static SmoothingPreset Extreme => new SmoothingPreset
        {
            UseStabilizer = true,
            StabilityThreshold = 6.0f,
            StableFramesRequired = 5,
            UseVisualSmoothing = true,
            RotationSmoothSpeed = 0.05f,
            PositionSmoothSpeed = 0.1f
        };
    }
}
