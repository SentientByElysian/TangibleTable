using TMPro;
using TuioNet.Tuio11;
using UnityEngine;
using UnityEngine.UI;
using TangibleTable.Core.Debugging;
using UnityEngine.Events;

namespace TangibleTable.Core.Behaviours.Visualization
{
    /// <summary>
    /// Visualizes and manages TUIO elements (objects and cursors) by handling their visual representation.
    /// </summary>
    public class TuioVisualizer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _createDebugComponent = false;
        
        [Header("Delta Rotation")]
        [SerializeField] private bool _trackDeltaRotation = true;
        [Tooltip("Set to true to reset the reference angle when the puck is lifted and placed again")]
        [SerializeField] private bool _resetReferenceOnNewSession = true;
        
        private RectTransform _rectTransform;
        
        // TUIO object data
        private uint _sessionId;
        private int _symbolId = -1;
        
        // Position and rotation data
        private Vector2 _tuioPosition = Vector2.zero;
        protected float _angle = 0f;
        
        // Delta rotation tracking
        private float _referenceAngle = 0f;
        private float _deltaAngle = 0f;
        private float _lastDeltaAngle = 0f;
        private bool _referenceAngleInitialized = false;
        
        // Offset data
        private Vector2 _positionOffset = Vector2.zero;
        private bool _isCursor = false;
        
        // Debug component
        private TuioDebug _debugComponent;
        
        // For puck state display
        private string _puckStateName = "";
        
        // Stabilizer component for smoothing rotation
        private TuioStabilizer _stabilizer;
        
        // Raw data from TUIO
        private Vector2 _rawTuioPosition = Vector2.zero;
        private float _rawAngle = 0f;
        
        // Events
        public UnityEvent<float> OnDeltaRotationChanged = new UnityEvent<float>();
        
        private void Awake()
        {
            if (_createDebugComponent)
            {
                _debugComponent = GetComponentInChildren<TuioDebug>();
                if (_debugComponent == null)
                {
                    // Create a child GameObject for the debug component
                    GameObject debugObj = new("TuioDebug");
                    debugObj.transform.SetParent(transform);
                    debugObj.transform.localPosition = Vector3.zero;
                    
                    // Add TuioDebug component
                    _debugComponent = debugObj.AddComponent<TuioDebug>();
                    
                    // Add necessary UI components
                    RectTransform rectTransform = debugObj.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(200, 100);
                    
                    // Create background
                    GameObject background = new("Background");
                    background.transform.SetParent(debugObj.transform, false);
                    RectTransform bgRect = background.AddComponent<RectTransform>();
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.sizeDelta = Vector2.zero;
                    UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
                    bgImage.color = new Color(0, 0, 0, 0.7f);
                    
                    // Create text
                    GameObject textObj = new("DebugText");
                    textObj.transform.SetParent(debugObj.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = new Vector2(5, 5);
                    textRect.offsetMax = new Vector2(-5, -5);
                    TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                    tmpText.fontSize = 12;
                    tmpText.color = Color.white;
                    tmpText.alignment = TMPro.TextAlignmentOptions.TopLeft;
                    
                    // Initialize visibility based on the global debug setting
                    bool globalDebug = GlobalTuioDebugger.Instance?._displayDebug ?? false;
                    _debugComponent.UpdateVisibility(globalDebug || _showDebugInfo);
                }
            }
            
            // Initialize stabilizer if needed
            _stabilizer = GetComponent<TuioStabilizer>();
            if (_stabilizer == null)
                {
                _stabilizer = gameObject.AddComponent<TuioStabilizer>();
            }
            
            // Register with the global debugger if it exists
            GlobalTuioDebugger.Instance?.AddTrackedObject(this);
        }
        
        private void OnDestroy()
        {
            // Unregister from the global debugger
            GlobalTuioDebugger.Instance?.RemoveTrackedObject(this);
        }
        
        /// <summary>
        /// Try to register with GlobalTuioDebugger if it exists
        /// </summary>
        private void RegisterWithGlobalDebugger()
        {
            GlobalTuioDebugger.Instance?.AddTrackedObject(this);
        }
        
        /// <summary>
        /// Try to unregister from GlobalTuioDebugger if it exists
        /// </summary>
        private void UnregisterFromGlobalDebugger()
                {
            GlobalTuioDebugger.Instance?.RemoveTrackedObject(this);
        }
        
        /// <summary>
        /// Initialize with a TUIO object
        /// </summary>
        public virtual void Initialize(Tuio11Object obj)
        {
            EnsureRectTransform();
            
            uint previousSessionId = _sessionId;
            _sessionId = obj.SessionId;
            _symbolId = (int)obj.SymbolId;
            _isCursor = false;
            
            // Configure stabilizer mode for an object
            if (_stabilizer != null)
            {
                _stabilizer.Mode = TuioStabilizer.StabilizationMode.Object;
            }
            
            // Store the raw TUIO data
            _rawTuioPosition = new Vector2(obj.Position.X, obj.Position.Y);
            _rawAngle = Mathf.Rad2Deg * obj.Angle;
            
            // Process raw data into stabilized values
            ProcessRawTuioData();
            
            // Reset reference angle if needed (when a new puck is placed)
            if (_trackDeltaRotation && (_resetReferenceOnNewSession && previousSessionId != _sessionId || !_referenceAngleInitialized))
            {
                SetReferenceAngle(_angle);
            }
            
            // Update component visuals
            UpdateVisual();
            
            // Update debug info
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Initialize with a TUIO cursor
        /// </summary>
        public virtual void Initialize(Tuio11Cursor cursor)
        {
            EnsureRectTransform();
            
            _sessionId = cursor.SessionId;
            _symbolId = -1; // No symbol ID for cursors
            _isCursor = true;
            
            // Configure stabilizer mode for a cursor
            if (_stabilizer != null)
            {
                _stabilizer.Mode = TuioStabilizer.StabilizationMode.Cursor;
            }
            
            // Store the raw TUIO data
            _rawTuioPosition = new Vector2(cursor.Position.X, cursor.Position.Y);
            _rawAngle = 0f; // Cursors don't have rotation
            
            // Process raw data into stabilized values
            ProcessRawTuioData();
            
            // Update component visuals
            UpdateVisual();
            
            // Update debug info
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Process raw TUIO data into stabilized position and angle values
        /// Similar to CustomTuioBehaviour's approach
        /// </summary>
        private void ProcessRawTuioData()
        {
            // Store the raw position for further processing
            _tuioPosition = _rawTuioPosition;
            
            // For objects (not cursors), process angle with stabilization
            if (!_isCursor && _stabilizer != null)
            {
                // Process angle through stabilizer
                _angle = _stabilizer.StabilizeRotation(_rawAngle);
                
                // Calculate delta rotation if enabled
                if (_trackDeltaRotation && _referenceAngleInitialized)
                {
                    // Calculate the delta rotation (reversed direction)
                    float rawDelta = -Mathf.DeltaAngle(_referenceAngle, _angle);
                    
                    // Convert to 0-360 range
                    _deltaAngle = (rawDelta + 360f) % 360f;
                    
                    // Only trigger the event if the delta angle has actually changed
                    if (Mathf.Abs(_deltaAngle - _lastDeltaAngle) > 0.1f)
                    {
                        _lastDeltaAngle = _deltaAngle;
                        OnDeltaRotationChanged.Invoke(_deltaAngle);
                    }
                }
            }
            else
            {
                _angle = _rawAngle;
            }
        }
        
        /// <summary>
        /// Sets the current angle as the reference angle (0°) for delta rotation calculations
        /// </summary>
        public void SetReferenceAngle(float referenceAngle)
        {
            _referenceAngle = referenceAngle;
            _deltaAngle = 0f;
            _lastDeltaAngle = 0f;
            _referenceAngleInitialized = true;
            
            // Trigger initial event with zero delta
            OnDeltaRotationChanged.Invoke(0f);
        }
        
        /// <summary>
        /// Manually reset the reference angle to the current angle, making the current rotation the new "zero" reference
        /// </summary>
        public void ResetReferenceAngle()
        {
            SetReferenceAngle(_angle);
        }
        
        /// <summary>
        /// Get the current delta rotation in degrees (relative to initial reference angle)
        /// </summary>
        public float GetDeltaRotation()
        {
            if (_trackDeltaRotation && _referenceAngleInitialized)
            {
                return _deltaAngle;
            }
            return 0f;
        }
        
        /// <summary>
        /// Get the reference angle in degrees (the angle that's considered "zero" for delta calculations)
        /// </summary>
        public float GetReferenceAngle()
        {
            return _referenceAngle;
        }
        
        /// <summary>
        /// Get the absolute rotation in world space
        /// </summary>
        public float GetAbsoluteRotation()
        {
            return _angle;
        }
        
        private void EnsureRectTransform()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            // Set up the size and anchoring
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
        
        protected virtual void Update()
        {
            // Check for global debug setting from GlobalTuioDebugger
            bool globalDebug = false;
            if (GlobalTuioDebugger.Instance != null)
            {
                globalDebug = GlobalTuioDebugger.Instance._displayDebug;
            }
            
            // Update our local debug flag if it doesn't match the global setting
            if (_showDebugInfo != globalDebug)
            {
                _showDebugInfo = globalDebug;
            }
            
            _debugComponent?.UpdateVisibility(_showDebugInfo);
        }
        
        /// <summary>
        /// Update the visual presentation based on TUIO data
        /// </summary>
        public virtual void UpdateVisual()
        {
            if (_rectTransform == null) return;
            
            // Position the object on screen
            Vector3 screenPosition = GetScreenPosition(_tuioPosition);
            
            if (_stabilizer != null && _stabilizer.enabled)
            {
                // Set targets for smoothing in the stabilizer
                _stabilizer.SetTargets(screenPosition, _angle);
            }
            else
            {
                // Direct update without stabilization
            _rectTransform.position = screenPosition;
            _rectTransform.rotation = Quaternion.Euler(0, 0, _angle);
            }
            
            // Update debug info
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Update the object with new TUIO data 
        /// </summary>
        public virtual void UpdateObject(Tuio11Object obj)
        {
            // Update raw position and angle from TUIO object
            _rawTuioPosition = new Vector2(obj.Position.X, obj.Position.Y);
            _rawAngle = Mathf.Rad2Deg * obj.Angle;
            
            // Process raw data into stabilized values
            ProcessRawTuioData();
            
            // Update visuals
            UpdateVisual();
            
            // Update debug info
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Update the cursor with new TUIO data
        /// </summary>
        public virtual void UpdateCursor(Tuio11Cursor cursor)
        {
            // Update raw position from TUIO cursor
            _rawTuioPosition = new Vector2(cursor.Position.X, cursor.Position.Y);
            
            // Process raw data into stabilized values (for cursors, this just copies the position)
            ProcessRawTuioData();
            
            // Update visuals
            UpdateVisual();
            
            // Update debug info
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Update debug information display
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (_debugComponent != null)
            {
                // Get delta rotation for debug display
                float deltaRotation = _trackDeltaRotation && _referenceAngleInitialized ?
                    _deltaAngle : 0f;
                
                // Use the overload that includes delta rotation
                _debugComponent.SetData(
                    _sessionId.ToString(),
                    _symbolId.ToString(),
                    _tuioPosition,
                    _angle,
                    deltaRotation,
                    _puckStateName
                );
                
                // Visibility is now handled by the TuioDebug component itself
                // based on the global debug setting
            }
        }
        
        /// <summary>
        /// Convert TUIO position to screen position
        /// </summary>
        protected virtual Vector3 GetScreenPosition(Vector2 tuioPos)
        {
            // Apply position offset if this is a cursor
            Vector2 adjustedPos = tuioPos;
            
            // Apply offset - different scale for cursors vs physical objects
            if (_isCursor)
            {
                // Cursors: Apply offset in screen space (pixels)
                float screenX = adjustedPos.x * Screen.width;
                float screenY = (1 - adjustedPos.y) * Screen.height;
                return new Vector3(screenX + _positionOffset.x, screenY + _positionOffset.y, 0);
            }
            else
            {
                // Physical objects: Apply offset in normalized TUIO space (0-1)
                adjustedPos += _positionOffset;
                
                // Clamp to valid range
                adjustedPos.x = Mathf.Clamp01(adjustedPos.x);
                adjustedPos.y = Mathf.Clamp01(adjustedPos.y);
                
                // Convert to screen space
                float screenX = adjustedPos.x * Screen.width;
                float screenY = (1 - adjustedPos.y) * Screen.height;
            return new Vector3(screenX, screenY, 0);
        }
        }
        
        /// <summary>
        /// Set the cursor/object position offset
        /// </summary>
        public void SetPositionOffset(Vector2 offset)
        {
            _positionOffset = offset;
            UpdateVisual();
        }
        
        /// <summary>
        /// Get the symbol ID of this TUIO object
        /// </summary>
        public int GetSymbolId()
        {
            return _symbolId;
        }
        
        /// <summary>
        /// Get whether this visualizer represents a cursor
        /// </summary>
        public bool IsCursor => _isCursor;
        
        /// <summary>
        /// Set the state name for this puck
        /// </summary>
        public void SetPuckState(string stateName)
        {
            _puckStateName = stateName;
            UpdateDebugInfo();
        }
        
        /// <summary>
        /// Configure the stabilizer component with specific settings
        /// </summary>
        public void ConfigureStabilizer(float rotationThreshold, float rotationSpeed, float positionSpeed)
        {
            if (_stabilizer != null)
            {
                // Set the appropriate mode first
                _stabilizer.Mode = _isCursor 
                    ? TuioStabilizer.StabilizationMode.Cursor 
                    : TuioStabilizer.StabilizationMode.Object;
                    
                // Configure with provided settings
                _stabilizer.Configure(
                    rotationThreshold,
                    rotationSpeed,
                    positionSpeed
                );
            }
        }
        
        /// <summary>
        /// Enable or disable the stabilizer component
        /// </summary>
        public void SetStabilizerEnabled(bool enabled)
        {
            if (_stabilizer != null)
            {
                _stabilizer.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Destroy this object
        /// </summary>
        public virtual void Destroy()
        {
            GameObject.Destroy(gameObject);
        }
        
        /// <summary>
        /// Set debug panel visibility - kept for compatibility with GlobalTuioDebugger
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _showDebugInfo = enabled;
            _debugComponent?.UpdateVisibility(enabled);
        }
    }
} 