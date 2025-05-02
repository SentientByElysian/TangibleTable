using UnityEngine;
using TuioUnity.Common;

namespace TangibleTable.DualDisplay
{
    /// <summary>
    /// Handles the transformation of TUIO coordinates (0-1) to screen positions for dual display setup.
    /// </summary>
    public static class DualDisplayTuioTransform
    {
        // Configuration options
        public static bool DualScreenMode
        {
            get => IsDualDisplayModeActive;
            set => IsDualDisplayModeActive = value;
        }
        public static bool DebugMode = false;      // Enable debug logging of coordinates
        
        // Screen size used for calculations (can be overridden for testing)
        private static int _screenWidth = Screen.width;
        private static int _screenHeight = Screen.height;
        
        // Screen sizes (set from a manager script on initialization)
        private static Vector2 _firstDisplaySize = new Vector2(1920, 1080);
        private static Vector2 _secondDisplaySize = new Vector2(1920, 1080);
        
        // Display offsets (how much the second display is offset from the first)
        private static Vector2 _secondDisplayOffset = new Vector2(1920, 0);
        
        // Whether the system is running in dual display mode
        public static bool IsDualDisplayModeActive { get; private set; } = false;
        
        /// <summary>
        /// Set the screen dimensions used for calculations
        /// This is useful for testing when the actual screen size isn't available
        /// </summary>
        public static void SetScreenDimensions(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
        }
        
        /// <summary>
        /// Set up the transform for one-display setup.
        /// </summary>
        public static void SetSingleDisplayTransform(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            IsDualDisplayModeActive = false;
        }
        
        /// <summary>
        /// Set up the dual display transformation parameters
        /// </summary>
        public static void SetDisplayParameters(Vector2 firstDisplay, Vector2 secondDisplay, Vector2 secondOffset, bool isDualMode = true)
        {
            _firstDisplaySize = firstDisplay;
            _secondDisplaySize = secondDisplay;
            _secondDisplayOffset = secondOffset;
            IsDualDisplayModeActive = isDualMode;
        }
        
        /// <summary>
        /// Get the screen position for a TUIO coordinate on the first display (main display)
        /// </summary>
        public static Vector3 GetFirstDisplayPosition(Vector2 tuioPosition)
        {
            if (DebugMode)
            {
                Debug.Log($"First Display Raw Input: ({tuioPosition.x}, {tuioPosition.y})");
            }
            
            Vector3 result;
            
            if (IsDualDisplayModeActive)
            {
                result = new Vector3(
                    tuioPosition.x * _firstDisplaySize.x,
                    (1 - tuioPosition.y) * _firstDisplaySize.y, // TUIO Y is inverted
                    0
                );
            }
            else
            {
                // Fall back to single display mode if dual mode isn't active
                result = new Vector3(
                    tuioPosition.x * _screenWidth,
                    (1 - tuioPosition.y) * _screenHeight,
                    0
                );
            }
            
            if (DebugMode)
            {
                Debug.Log($"First Display Result: Screen: ({result.x}, {result.y})");
            }
            
            return result;
        }
        
        /// <summary>
        /// Get the screen position for a TUIO coordinate on the second display
        /// </summary>
        public static Vector3 GetSecondDisplayPosition(Vector2 tuioPosition)
        {
            if (DebugMode)
            {
                Debug.Log($"Second Display Raw Input: ({tuioPosition.x}, {tuioPosition.y})");
            }
            
            Vector3 result;
            
            if (IsDualDisplayModeActive)
            {
                result = new Vector3(
                    tuioPosition.x * _secondDisplaySize.x + _secondDisplayOffset.x,
                    (1 - tuioPosition.y) * _secondDisplaySize.y + _secondDisplayOffset.y, // TUIO Y is inverted
                    0
                );
            }
            else
            {
                // If not in dual display mode, second display position is the same as first
                result = GetFirstDisplayPosition(tuioPosition);
            }
            
            if (DebugMode)
            {
                Debug.Log($"Second Display Result: Screen: ({result.x}, {result.y})");
            }
            
            return result;
        }
        
        /// <summary>
        /// Transforms a size value from normalized TUIO size to screen space size.
        /// </summary>
        public static Vector2 GetScreenSpaceSize(Vector2 size)
        {
            return new Vector2(
                size.x * _screenWidth,
                size.y * _screenHeight);
        }
    }
} 