using TMPro;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using UnityEngine;
using UnityEngine.UI;
using TangibleTable.Shared;
using TangibleTable.DualDisplay;

namespace TangibleTable
{
    /// <summary>
    /// Custom behavior for TUIO 1.1 objects that uses our dual-display coordinate transformation.
    /// Can also be used for single display mode.
    /// </summary>
    public class CustomTuio11Behaviour : MonoBehaviour
    {
        private RectTransform _rectTransform;
        
        // Use the base object type instead
        private object _component;
        private uint _sessionId;
        private bool _isFirstDisplay;
        
        // Flag to use single display mode instead of dual display
        [SerializeField] private bool _useSingleDisplayMode = false;
        
        // Cursor offset (only applies to Cursor type, not objects)
        [SerializeField] private Vector2 _cursorOffset = Vector2.zero;
        private bool _isCursor = false;
        
        // Rotation stability settings
        [Header("Rotation Stability")]
        [SerializeField] private bool _useRotationStabilizer = true;
        
        [SerializeField, Range(0.1f, 10f)] 
        private float _rotationStabilityThreshold = 2.5f;
        
        [SerializeField, Range(1, 10)] 
        private int _stableFramesRequired = 3;
        
        // Cursor display option
        [Header("Cursor Display")]
        [SerializeField] private bool _showCursorVisual = true;
        
        // Visual smoothing settings
        [Header("Visual Smoothing")]
        [SerializeField] private bool _useVisualSmoothing = true;
        
        [SerializeField, Range(0.01f, 1f)]
        private float _rotationSmoothSpeed = 0.15f;
        
        [SerializeField, Range(0.01f, 1f)]
        private float _positionSmoothSpeed = 0.25f;
        
        // Debug settings
        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugPanel = false;
        
        // Position and rotation data
        private Vector2 _tuioPosition = Vector2.zero;
        private Vector2 _displayPosition = Vector2.zero; // Visually smoothed position for display
        private float _angle; // Stabilized angle
        private float _displayAngle; // Visually smoothed angle for display
        
        // For rotation stabilization at the source level
        private float _lastStableAngle = 0f;
        private bool _angleInitialized = false;
        public static float ANGLE_STABILITY_THRESHOLD = 2.5f; // For debugger access
        public static int STABLE_FRAMES_REQUIRED = 3; // For debugger access
        private int _stableFrameCount = 0;
        private float _pendingAngle = 0f;
        
        // Text component for displaying info
        private TextMeshProUGUI _tmpText;
        private Text _legacyText;
        private bool _textInitialized = false;
        
        // For puck state display
        private string _puckStateName = "";
        
        // Symbol ID (for TUIO objects)
        public int _symbolId = -1;
        
        // Debugger reference
        private TuioDebugger _debugger;

        public void Initialize(Tuio11Cursor cursor, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = cursor;
            _sessionId = cursor.SessionId;
            _isFirstDisplay = isFirstDisplay;
            _isCursor = true;
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
            
            // Ensure cursor visibility is handled right away
            if (!_showCursorVisual)
            {
                HideCursorVisuals();
            }
        }

        public void Initialize(Tuio11Cursor cursor)
        {
            // Overload for single display mode
            _useSingleDisplayMode = true;
            _rectTransform = GetComponent<RectTransform>();
            _component = cursor;
            _sessionId = cursor.SessionId;
            _isCursor = true;
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
            
            // Ensure cursor visibility is handled right away
            if (!_showCursorVisual)
            {
                HideCursorVisuals();
            }
        }

        public void Initialize(Tuio11Object obj, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = obj;
            _sessionId = obj.SessionId;
            _isFirstDisplay = isFirstDisplay;
            _symbolId = (int)obj.SymbolId;
            _isCursor = false;
            
            // Find and initialize text components
            FindTextComponents();
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
            
            // Initialize visual position and angle
            _displayPosition = _rectTransform.position;
            _displayAngle = _angle;
        }

        public void Initialize(Tuio11Object obj)
        {
            // Overload for single display mode
            _useSingleDisplayMode = true;
            _rectTransform = GetComponent<RectTransform>();
            _component = obj;
            _sessionId = obj.SessionId;
            _symbolId = (int)obj.SymbolId;
            _isCursor = false;
            
            // Find and initialize text components
            FindTextComponents();
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
            
            // Initialize visual position and angle
            _displayPosition = _rectTransform.position;
            _displayAngle = _angle;
        }

        public void Initialize(Tuio11Blob blob, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = blob;
            _sessionId = blob.SessionId;
            _isFirstDisplay = isFirstDisplay;
            _isCursor = false;
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
        }

        public void Initialize(Tuio11Blob blob)
        {
            // Overload for single display mode
            _useSingleDisplayMode = true;
            _rectTransform = GetComponent<RectTransform>();
            _component = blob;
            _sessionId = blob.SessionId;
            _isCursor = false;
            
            // Initialize debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            UpdateComponent();
        }
        
        private void FindTextComponents()
        {
            if (_textInitialized) return;
            
            // Try to find TextMeshPro component
            _tmpText = GetComponentInChildren<TextMeshProUGUI>();
            
            // Try to find legacy Text component
            if (_tmpText == null)
            {
                _legacyText = GetComponentInChildren<Text>();
            }
            
            _textInitialized = true;
            
            // Initially hide text components until puck state is set
            if (_tmpText != null)
                _tmpText.gameObject.SetActive(false);
                
            if (_legacyText != null)
                _legacyText.gameObject.SetActive(false);
        }
        
        private void Awake()
        {
            // Setup debugger if needed
            if (_showDebugPanel)
            {
                SetupDebugger();
            }
            
            // Initialize display position
            _displayPosition = _rectTransform != null ? 
                (Vector2)_rectTransform.position : 
                Vector2.zero;
        }
        
        private void SetupDebugger()
        {
            // Only create one debugger
            if (TuioDebugger.Instance == null)
            {
                GameObject debugObj = new GameObject("TUIO Debugger");
                _debugger = debugObj.AddComponent<TuioDebugger>();
                _debugger.SetAlignment(TuioDebugger.PanelAlignment.Right); // Set alignment to right side
            }
        }
        
        private void OnEnable()
        {
            // Update debugger when show debug panel changes
            if (_showDebugPanel && TuioDebugger.Instance == null)
            {
                SetupDebugger();
            }
            else if (!_showDebugPanel && TuioDebugger.Instance != null)
            {
                TuioDebugger.Instance.RemoveTrackedObject(this);
            }
            
            // Make sure the debugger is tracking this object
            if (_showDebugPanel && TuioDebugger.Instance != null)
            {
                TuioDebugger.Instance.AddTrackedObject(this);
            }
            
            // Update cursor offset if this is a cursor
            if (_isCursor)
            {
                _cursorOffset = TuioManager.GlobalCursorOffset;
            }
        }
        
        private void OnDisable()
        {
            // Remove from debugger
            if (TuioDebugger.Instance != null)
            {
                TuioDebugger.Instance.RemoveTrackedObject(this);
            }
        }
        
        private void Update()
        {
            // Update cursor offset if this is a cursor
            if (_isCursor && _cursorOffset != TuioManager.GlobalCursorOffset)
            {
                _cursorOffset = TuioManager.GlobalCursorOffset;
            }
            
            UpdateComponent();
            
            // Update debugger status based on TabField value
            if (_showDebugPanel && TuioDebugger.Instance == null)
            {
                SetupDebugger();
            }
            else if (_showDebugPanel && TuioDebugger.Instance != null)
            {
                TuioDebugger.Instance.AddTrackedObject(this);
            }
            else if (!_showDebugPanel && TuioDebugger.Instance != null)
            {
                TuioDebugger.Instance.RemoveTrackedObject(this);
            }
            
            // Keep cursor visibility in sync with TuioDebugger settings
            if (_isCursor && TuioDebugger.Instance != null)
            {
                bool shouldShowCursor = TuioDebugger.Instance.ShowUIPanel;
                if (_showCursorVisual != shouldShowCursor)
                {
                    SetCursorVisibility(shouldShowCursor);
                }
            }
            
            // Apply rotation visual smoothing
            ApplyVisualSmoothing();
        }
        
        /// <summary>
        /// Smoothly interpolate the visual position and rotation to match the target values
        /// </summary>
        private void ApplyVisualSmoothing()
        {
            // Skip if visual smoothing is disabled or if it's a cursor
            if (!_useVisualSmoothing || _isCursor)
            {
                return;
            }
            
            // Apply position smoothing
            if (_rectTransform != null)
            {
                // Get current target position based on latest TUIO data
                Vector3 targetPosition;
                if (_useSingleDisplayMode)
                {
                    targetPosition = TuioTransform.GetScreenPosition(_tuioPosition);
                }
                else if (_isFirstDisplay)
                {
                    targetPosition = DualDisplayTuioTransform.GetFirstDisplayPosition(_tuioPosition);
                }
                else
                {
                    targetPosition = DualDisplayTuioTransform.GetSecondDisplayPosition(_tuioPosition);
                }
                
                // Smoothly move towards target position
                _displayPosition = Vector2.Lerp(_displayPosition, targetPosition, _positionSmoothSpeed);
                _rectTransform.position = _displayPosition;
            }
            
            // Apply rotation smoothing
            if (!_useVisualSmoothing)
            {
                _displayAngle = _angle;
                return;
            }
            
            // Handle 360-degree wrapping when lerping between angles
            float shortestAngle = Mathf.DeltaAngle(_displayAngle, _angle);
            _displayAngle = Mathf.Lerp(0, shortestAngle, _rotationSmoothSpeed) + _displayAngle;
            
            // Keep angle in 0-360 range
            _displayAngle = (_displayAngle + 360f) % 360f;
            
            // Apply the smoothed rotation
            if (_rectTransform != null)
            {
                _rectTransform.rotation = Quaternion.Euler(0, 0, _displayAngle);
            }
        }

        private void UpdateComponent()
        {
            if (_component == null || _rectTransform == null)
                return;
                
            // Hide the visual if it's a cursor and showCursorVisual is false
            if (_isCursor && !_showCursorVisual)
            {
                // Hide the visual but maintain the transform
                if (gameObject.activeInHierarchy)
                {
                    HideCursorVisuals();
                }
            }
            
            // Get position from component based on its type
            if (_component is Tuio11Cursor cursor)
            {
                _tuioPosition.x = cursor.Position.X;
                _tuioPosition.y = cursor.Position.Y;
                _angle = 0f;
            }
            else if (_component is Tuio11Object obj)
            {
                _tuioPosition.x = obj.Position.X;
                _tuioPosition.y = obj.Position.Y;
                
                // Get raw angle and apply direct stabilization
                float rawAngle = Mathf.Rad2Deg * obj.Angle;
                _angle = StabilizeAngle(rawAngle);
                
                _symbolId = (int)obj.SymbolId;
            }
            else if (_component is Tuio11Blob blob)
            {
                _tuioPosition.x = blob.Position.X;
                _tuioPosition.y = blob.Position.Y;
                _angle = 0f;
            }

            // Apply coordinate transformation based on mode
            Vector3 screenPosition;
            if (_useSingleDisplayMode)
            {
                // Use standard TUIO transformation for single display
                screenPosition = TuioTransform.GetScreenPosition(_tuioPosition);
            }
            else if (_isFirstDisplay)
            {
                // Use dual display transformation
                screenPosition = DualDisplayTuioTransform.GetFirstDisplayPosition(_tuioPosition);
            }
            else
            {
                // Use dual display transformation
                screenPosition = DualDisplayTuioTransform.GetSecondDisplayPosition(_tuioPosition);
            }
            
            // Apply cursor offset if this is a cursor
            if (_isCursor)
            {
                screenPosition += new Vector3(_cursorOffset.x, _cursorOffset.y, 0);
            }

            // Store the target position (will be applied with smoothing in ApplyVisualSmoothing)
            Vector3 targetPosition = screenPosition;
            
            // If visual smoothing is disabled or it's a cursor, update position directly
            if (!_useVisualSmoothing || _isCursor)
            {
                _rectTransform.position = targetPosition;
                _displayPosition = targetPosition;
            }
            
            // If visual smoothing is disabled, update rotation directly
            if (!_useVisualSmoothing)
            {
                _rectTransform.rotation = Quaternion.Euler(0, 0, _angle);
                _displayAngle = _angle;
            }
            
            // Update text display
            UpdateTextDisplay();
            
            // Ensure cursor stays invisible if needed (in case of component reset)
            if (_isCursor && !_showCursorVisual)
            {
                HideCursorVisuals();
            }
        }
        
        private void HideCursorVisuals()
        {
            // Hide all visual components but keep the GameObject active
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
            foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                graphic.enabled = false;
            }
            
            // If there's an Image component, disable it
            foreach (var image in GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                image.enabled = false;
            }
            
            // For 3D objects
            foreach (var mesh in GetComponentsInChildren<MeshRenderer>())
            {
                mesh.enabled = false;
            }
        }
        
        /// <summary>
        /// Set cursor offset for this object (only applies if it's a cursor)
        /// </summary>
        public void SetCursorOffset(Vector2 offset)
        {
            if (_isCursor)
            {
                _cursorOffset = offset;
            }
        }
        
        /// <summary>
        /// Stabilizes the raw angle by ignoring small fluctuations directly at the data source
        /// </summary>
        private float StabilizeAngle(float rawAngle)
        {
            // Skip stabilization if disabled
            if (!_useRotationStabilizer)
                return rawAngle;
            
            // Update the static values for debugger to access
            ANGLE_STABILITY_THRESHOLD = _rotationStabilityThreshold;
            STABLE_FRAMES_REQUIRED = _stableFramesRequired;
            
            // Initialize on first call
            if (!_angleInitialized)
            {
                _lastStableAngle = rawAngle;
                _angleInitialized = true;
                return rawAngle;
            }
            
            // Check if the change is larger than our threshold
            float angleDifference = Mathf.Abs(Mathf.DeltaAngle(rawAngle, _lastStableAngle));
            
            if (angleDifference < _rotationStabilityThreshold)
            {
                // Small change - ignore it completely and use the last stable angle
                return _lastStableAngle;
            }
            else
            {
                // Significant change - implement a stability period before accepting it
                // This helps prevent one-frame glitches
                
                if (_pendingAngle != rawAngle)
                {
                    // New potential angle, start counting stable frames
                    _pendingAngle = rawAngle;
                    _stableFrameCount = 1;
                    return _lastStableAngle; // Keep using old value
                }
                else
                {
                    // Same pending angle, increment counter
                    _stableFrameCount++;
                    
                    if (_stableFrameCount >= _stableFramesRequired)
                    {
                        // Angle has been stable long enough, accept it
                        _lastStableAngle = _pendingAngle;
                        return _lastStableAngle;
                    }
                    else
                    {
                        // Not stable enough yet
                        return _lastStableAngle;
                    }
                }
            }
        }
        
        private void UpdateTextDisplay()
        {
            // Only update text if we have a puck state to show
            if (string.IsNullOrEmpty(_puckStateName)) return;
            
            string displayText = GetDisplayText();
            
            // Update TextMeshPro if available
            if (_tmpText != null)
            {
                _tmpText.text = displayText;
            }
            // Update legacy Text if available
            else if (_legacyText != null)
            {
                _legacyText.text = displayText;
            }
        }
        
        private string GetDisplayText()
        {
            string displaySide;
            if (_useSingleDisplayMode)
                displaySide = "S";
            else
                displaySide = _isFirstDisplay ? "1" : "2";
                
            string position = $"({_tuioPosition.x:F2}, {_tuioPosition.y:F2})";
            string angleText = $"{_angle:F0}°";
            string stateText = string.IsNullOrEmpty(_puckStateName) ? "" : $"\nState: {_puckStateName}";
            
            return $"ID: {_symbolId}\nPos: {position}\nAngle: {angleText}\nDisp: {displaySide}{stateText}";
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
        
        public string DebugText()
        {
            string displayType;
            if (_useSingleDisplayMode)
                displayType = "Single";
            else
                displayType = _isFirstDisplay ? "First" : "Second";
                
            return $"{displayType} Display | ID: {_sessionId} | Position: {_tuioPosition}";
        }

        /// <summary>
        /// Shows or hides the debug text based on whether this puck has a state
        /// </summary>
        public void SetPuckState(string stateName)
        {
            _puckStateName = stateName;
            
            // Make sure text components are initialized
            if (!_textInitialized)
                FindTextComponents();
                
            // Show debug text if we have a state name
            bool showDebug = !string.IsNullOrEmpty(_puckStateName);
            
            // Enable/disable text components based on whether we have state info
            if (_tmpText != null)
                _tmpText.gameObject.SetActive(showDebug);
                
            if (_legacyText != null)
                _legacyText.gameObject.SetActive(showDebug);
        }

        /// <summary>
        /// Set cursor visibility (only applies if it's a cursor)
        /// </summary>
        public void SetCursorVisibility(bool visible)
        {
            if (_isCursor)
            {
                _showCursorVisual = visible;
                
                // Apply immediately
                if (!_showCursorVisual)
                {
                    HideCursorVisuals();
                    
                    // Also update any attached TuioDebug component
                    var debug = GetComponent<TuioDebug>();
                    if (debug != null)
                    {
                        // Use reflection to access the private UpdateVisibility method
                        var method = debug.GetType().GetMethod("UpdateVisibility", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(debug, null);
                        }
                    }
                }
            }
        }
    }
} 