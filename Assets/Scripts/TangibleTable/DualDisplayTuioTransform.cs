using UnityEngine;
using TuioUnity.Common;

namespace TangibleTable
{
    /// <summary>
    /// Custom TUIO transform utility to handle specific dual/single display configuration
    /// </summary>
    public static class DualDisplayTuioTransform
    {
        // Configuration options
        public static bool DualScreenMode = true;  // Set to false for single screen mode
        public static bool DebugMode = false;      // Enable debug logging of coordinates
        
        // Screen size used for calculations (can be overridden for testing)
        private static int _screenWidth = Screen.width;
        private static int _screenHeight = Screen.height;
        
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
        /// Transforms TUIO position from the first display source.
        /// In dual-screen mode: Maps to left half of screen.
        /// In single-screen mode: Maps to entire screen.
        /// </summary>
        public static Vector3 GetFirstDisplayPosition(Vector2 position)
        {
            if (DebugMode)
            {
                Debug.Log($"First Display Raw Input: ({position.x}, {position.y})");
            }
            
            float remappedX;
            
            if (DualScreenMode)
            {
                // Map to left half of screen in dual screen mode
                remappedX = position.x * 0.5f;
            }
            else
            {
                // Map to entire screen in single screen mode
                remappedX = position.x;
            }
            
            // Y coordinate is already correct (1 at bottom, 0 at top)
            float remappedY = position.y;
            
            // Convert to screen coordinates
            float screenX = remappedX * _screenWidth;
            float screenY = (1 - remappedY) * _screenHeight;
            
            if (DebugMode)
            {
                Debug.Log($"First Display Remapped: ({remappedX}, {remappedY}) -> Screen: ({screenX}, {screenY})");
            }
            
            return new Vector3(screenX, screenY, 0);
        }
        
        /// <summary>
        /// Transforms TUIO position from the second display source.
        /// In dual-screen mode: Maps to right half of screen.
        /// In single-screen mode: Maps to entire screen (same as first display).
        /// </summary>
        public static Vector3 GetSecondDisplayPosition(Vector2 position)
        {
            if (DebugMode)
            {
                Debug.Log($"Second Display Raw Input: ({position.x}, {position.y})");
            }
            
            float remappedX;
            
            if (DualScreenMode)
            {
                // Display 2 has coordinates from (0.5,0) to (1,1)
                // Map this 0.5-1 range to 0.5-1 screen range
                float normalizedX = (position.x - 0.5f) / 0.5f; // Convert 0.5-1.0 to 0-1.0
                remappedX = 0.5f + (normalizedX * 0.5f);  // Convert 0-1.0 to 0.5-1.0
            }
            else
            {
                // In single screen mode, map directly to screen
                remappedX = position.x;
            }
            
            // Y coordinate is already correct (1 at bottom, 0 at top)
            float remappedY = position.y;
            
            // Convert to screen coordinates
            float screenX = remappedX * _screenWidth;
            float screenY = (1 - remappedY) * _screenHeight;
            
            if (DebugMode)
            {
                Debug.Log($"Second Display Remapped: ({remappedX}, {remappedY}) -> Screen: ({screenX}, {screenY})");
            }
            
            return new Vector3(screenX, screenY, 0);
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