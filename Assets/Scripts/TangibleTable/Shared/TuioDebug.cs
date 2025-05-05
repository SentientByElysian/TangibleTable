using TMPro;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TangibleTable.Shared
{
    /// <summary>
    /// Simple component to display properties of tuio objects in the scene and set a random color for easier
    /// distinction between objects or touches.
    /// </summary>
    [RequireComponent(typeof(CustomTuioBehaviour))]
    public class TuioDebug : MonoBehaviour
    {
        public Color tuioColor = Color.white;
        
        [SerializeField] private TMP_Text        debugText;
        [SerializeField] private MaskableGraphic background;
        [SerializeField] private bool            isCursor = false;

        private CustomTuioBehaviour _customBehaviour;
        private bool _wasVisible = true;
        private bool _startComplete = false;

        private void Start()
        {
            _customBehaviour = GetComponent<CustomTuioBehaviour>();
            
            // Set initial color
            if (background != null)
                background.color = tuioColor;
                
            if (debugText != null)
                debugText.color = tuioColor;
            
            // Check if we should be visible based on settings
            UpdateVisibility();
            _startComplete = true;
        }

        private void Update()
        {
            // Update debug text
            if (_customBehaviour != null && debugText != null)
            {
                debugText.text = _customBehaviour.DebugText();
            }
            
            // Check visibility based on settings
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            bool shouldBeVisible = true;
            
            // For cursors, check cursor visibility setting
            if (isCursor)
            {
                // Check if cursor visuals should be hidden based on TuioManager
                shouldBeVisible = IsCursorVisualEnabled();
            }
            
            // For all objects (cursors and markers), check debug panel visibility
            if (TuioDebugger.Instance != null)
            {
                // If TuioDebugger has UI hidden, hide this debug visual too
                if (!TuioDebugger.Instance.ShowUIPanel)
                {
                    shouldBeVisible = false;
                }
            }
            
            // Apply visibility changes if needed
            if (_wasVisible != shouldBeVisible || !_startComplete)
            {
                SetVisibility(shouldBeVisible);
                _wasVisible = shouldBeVisible;
            }
        }
        
        private bool IsCursorVisualEnabled()
        {
            // Try to get cursor visual setting from TuioManager if it exists
            bool showCursor = true;
            
            if (_customBehaviour != null)
            {
                // Check the _showCursorVisual field directly from CustomTuio11Behaviour
                var showCursorField = _customBehaviour.GetType().GetField("_showCursorVisual", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (showCursorField != null)
                {
                    showCursor = (bool)showCursorField.GetValue(_customBehaviour);
                }
            }
            
            return showCursor;
        }
        
        private void SetVisibility(bool visible)
        {
            // Hide/show debug components
            if (debugText != null)
                debugText.enabled = visible;
                
            if (background != null)
                background.enabled = visible;
            
            // For CURSOR-type TUIO objects only - hide the cursor visuals completely
            if (isCursor && !visible)
            {
                HideAllRenderers();
            }
            
            // Special case: If we're a regular object (not cursor), we only hide debug text, not the object itself
            if (!isCursor)
            {
                // Ensure object's renderers stay visible even when debug text is hidden
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = true;  // Always keep real objects visible
                }
                
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image != background)
                        image.enabled = true;  // Keep object visuals visible
                }
            }
        }
        
        private void HideAllRenderers()
        {
            // This is for cursors only - hide absolutely everything
            // Hide meshes
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
            
            // Hide UI graphics (except our debug components already handled)
            foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic != debugText && graphic != background)
                    graphic.enabled = false;
            }
            
            // Hide particles
            foreach (var particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop();
                var renderer = particles.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
            
            // Hide lines
            foreach (var line in GetComponentsInChildren<LineRenderer>(true))
            {
                line.enabled = false;
            }
        }
        
        private void OnEnable()
        {
            if (_startComplete)
            {
                UpdateVisibility();
            }
        }
    }
}
