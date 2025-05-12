using UnityEngine;

namespace TuioUnity.Common
{
    /// <summary>
    /// Modified TuioTransform class with built-in dual screen support.
    /// This is a direct replacement for the original TuioTransform class.
    /// Includes all original methods like GetScreenSpaceSize.
    /// </summary>
    public static class TuioTransform
    {
        // Dual screen configuration
        private static bool _dualScreenEnabled = true;
        private static bool _screensAreSideBySide = true;
        private static float _screen1Proportion = 0.5f;
        private static float _horizontalDivision = 0.5f;
        private static float _verticalDivision = 0.5f;

        /// <summary>
        /// Configure the dual screen setup.
        /// Call this at application startup before any TUIO processing begins.
        /// </summary>
        public static void ConfigureDualScreen(bool enabled, bool sideBySide, float screen1Proportion,
            float horizontalDivision, float verticalDivision)
        {
            _dualScreenEnabled = enabled;
            _screensAreSideBySide = sideBySide;
            _screen1Proportion = Mathf.Clamp01(screen1Proportion);
            _horizontalDivision = Mathf.Clamp01(horizontalDivision);
            _verticalDivision = Mathf.Clamp01(verticalDivision);

            Debug.Log($"TuioTransform: Dual screen {(enabled ? "enabled" : "disabled")}, " +
                      $"Layout: {(sideBySide ? "Side by Side" : "Stacked")}, " +
                      $"Screen1 Proportion: {screen1Proportion}, " +
                      $"Divisions: H={horizontalDivision}, V={verticalDivision}");
        }

        /// <summary>
        /// Transforms a normalized TUIO position (0-1) to screen position in Unity's coordinate system.
        /// If dual screen is enabled, applies the appropriate transformation.
        /// </summary>
        public static Vector3 GetScreenPosition(Vector2 position)
        {
            if (!_dualScreenEnabled)
            {
                // Original single-screen behavior
                return new Vector3(
                    position.x * Screen.width,
                    (1 - position.y) * Screen.height,
                    0);
            }

            // Apply dual screen transformation
            return GetDualScreenPosition(position);
        }

        /// <summary>
        /// Calculates screen position for dual screen setup
        /// </summary>
        private static Vector3 GetDualScreenPosition(Vector2 position)
        {
            float screenX, screenY;

            if (_screensAreSideBySide)
            {
                // Side by side screens
                if (position.x < _horizontalDivision)
                {
                    // Map to first screen
                    screenX = (position.x / _horizontalDivision) * (Screen.width * _screen1Proportion);
                }
                else
                {
                    // Map to second screen
                    float normalizedX = (position.x - _horizontalDivision) / (1f - _horizontalDivision);
                    screenX = (normalizedX * (Screen.width * (1f - _screen1Proportion))) + (Screen.width * _screen1Proportion);
                }

                // Y mapping is straightforward
                screenY = (1f - position.y) * Screen.height;
            }
            else
            {
                // Stacked screens
                // X mapping is straightforward
                screenX = position.x * Screen.width;

                if (position.y < _verticalDivision)
                {
                    // Map to first screen (top)
                    screenY = (1f - (position.y / _verticalDivision)) * (Screen.height * _screen1Proportion);
                }
                else
                {
                    // Map to second screen (bottom)
                    float normalizedY = (position.y - _verticalDivision) / (1f - _verticalDivision);
                    screenY = (1f - normalizedY) * (Screen.height * (1f - _screen1Proportion))
                              + (Screen.height * _screen1Proportion);
                }
            }

            return new Vector3(screenX, screenY, 0f);
        }

        /// <summary>
        /// Transforms a normalized TUIO size to screen space size in Unity's coordinate system.
        /// </summary>
        /// <param name="size">Normalized TUIO size (0-1)</param>
        /// <returns>Size in screen space</returns>
        public static Vector2 GetScreenSpaceSize(Vector2 size)
        {
            if (!_dualScreenEnabled)
            {
                // Original single-screen behavior
                return new Vector2(
                    size.x * Screen.width,
                    size.y * Screen.height);
            }

            // For dual screen, we need to adjust the size according to the screen it's displayed on
            return GetDualScreenSize(size);
        }

        /// <summary>
        /// Calculates scaled size for dual screen setup
        /// </summary>
        private static Vector2 GetDualScreenSize(Vector2 size)
        {
            float scaleX, scaleY;

            if (_screensAreSideBySide)
            {
                // For side-by-side layout, X scale depends on which screen region we're in
                // We'll scale based on the respective screen's proportion of the total width
                // For simplicity, we'll use a weighted average of the two screens' scaling factors
                float screen1ScaleFactor = (Screen.width * _screen1Proportion) / _horizontalDivision;
                float screen2ScaleFactor = (Screen.width * (1 - _screen1Proportion)) / (1 - _horizontalDivision);

                // Use a weighted average based on screen proportions
                scaleX = (screen1ScaleFactor * _screen1Proportion) + (screen2ScaleFactor * (1 - _screen1Proportion));

                // Y scaling is straightforward
                scaleY = Screen.height;
            }
            else
            {
                // For stacked layout, similar approach but for Y axis
                // X scaling is straightforward
                scaleX = Screen.width;

                float screen1ScaleFactor = (Screen.height * _screen1Proportion) / _verticalDivision;
                float screen2ScaleFactor = (Screen.height * (1 - _screen1Proportion)) / (1 - _verticalDivision);

                // Use a weighted average based on screen proportions
                scaleY = (screen1ScaleFactor * _screen1Proportion) + (screen2ScaleFactor * (1 - _screen1Proportion));
            }

            return new Vector2(size.x * scaleX, size.y * scaleY);
        }
    }
}