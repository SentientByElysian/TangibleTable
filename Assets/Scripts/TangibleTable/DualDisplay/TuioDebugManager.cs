using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UTool.TabSystem;
using TangibleTable.DualDisplay;

namespace TangibleTable
{
    /// <summary>
    /// Debug manager for TUIO system that handles configuration
    /// for dual/single screen modes and debug options.
    /// </summary>
    [HasTabField]
    public class TuioDebugManager : MonoBehaviour
    {
        [Tooltip("When true, input is mapped to separate halves of the screen")]
        [TabField][SerializeField] private bool _dualScreenMode = true;
        
        [Tooltip("Debug mode shows raw coordinate logs in console")]
        [TabField][SerializeField] private bool _debugMode = false;
        
        [Tooltip("Screen resolution override for testing (0 to use actual screen)")]
        [SerializeField] private Vector2Int _screenResolution = new Vector2Int(3840, 1080);
        
        [Tooltip("All TUIO visualizers in the scene")]
        [SerializeField] private List<DualDisplayTuio11Visualizer> _tuioVisualizers = new List<DualDisplayTuio11Visualizer>();
        
        private void Start()
        {
            // Initialize screen resolution if specified
            if (_screenResolution.x > 0 && _screenResolution.y > 0)
            {
                DualDisplayTuioTransform.SetScreenDimensions(_screenResolution.x, _screenResolution.y);
            }
            
            // Initialize settings
            ApplySettings();
        }

        // Handle value changes from TabField system through OnValidate
        [TabButton]
        public void Refresh()
        {
            // Apply settings
            ApplySettings();
            
            // Apply screen resolution
            if (_screenResolution.x > 0 && _screenResolution.y > 0)
            {
                DualDisplayTuioTransform.SetScreenDimensions(_screenResolution.x, _screenResolution.y);
            }
        }
        
        private void ApplySettings()
        {
            // Apply settings to the static transform class
            DualDisplayTuioTransform.DualScreenMode = _dualScreenMode;
            DualDisplayTuioTransform.DebugMode = _debugMode;
        }
    }
} 