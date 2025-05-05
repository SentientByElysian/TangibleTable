using System;
using System.Collections.Generic;
using TangibleTable.Pucks;
using TangibleTable.Shared;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using TuioUnity.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TangibleTable.DualDisplay
{
    /// <summary>
    /// Custom TUIO 1.1 visualizer for dual/single display setup.
    /// This visualizer handles TUIO objects and cursor input.
    /// </summary>
    public class DualDisplayTuio11Visualizer : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] private TuioSessionBehaviour _tuioSessionBehaviour;
        [SerializeField] private Tuio11ObjectTransform _objectPrefab;
        [SerializeField] private Tuio11CursorTransform _cursorPrefab;
        
        [Header("Display Settings")]
        [SerializeField] private bool _isFirstDisplay = true;
        
        [Header("References")]
        [SerializeField] private PuckManager _puckManager;
        
        [Header("Cursor Events")]
        [SerializeField] private bool _enableCursorEvents = true;
        public UnityEvent<Vector2> OnCursorDown;
        public UnityEvent<Vector2> OnCursorMove;
        public UnityEvent<Vector2> OnCursorUp;
        
        // Delegate for object creation events
        public delegate void ObjectCreatedHandler(GameObject tuioObject);
        
        // Event fired when a new TUIO object is created
        public event ObjectCreatedHandler OnObjectCreated;
        
        // Define the delegate for cursor creation events
        public delegate void CursorCreatedHandler(GameObject cursorObject);
        
        // Event triggered when a new cursor is created
        public event CursorCreatedHandler OnCursorCreated;
        
        // Structure to store cursor information
        private struct CursorInfo
        {
            public GameObject CursorObject;
            public bool IsFirstDisplay;
        }
        
        private readonly Dictionary<uint, CustomTuioBehaviour> _tuioBehaviours = new Dictionary<uint, CustomTuioBehaviour>();
        private readonly Dictionary<uint, CursorInfo> _tuioCursors = new Dictionary<uint, CursorInfo>();

        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSessionBehaviour.TuioDispatcher;

        private void OnEnable()
        {
            try
            {
                // Register for TUIO object events
                Dispatcher.OnObjectAdd += AddTuioObject;
                Dispatcher.OnObjectRemove += RemoveTuioObject;
                
                // Register for TUIO cursor events if enabled
                if (_enableCursorEvents)
                {
                    Dispatcher.OnCursorAdd += AddTuioCursor;
                    Dispatcher.OnCursorUpdate += UpdateTuioCursor;
                    Dispatcher.OnCursorRemove += RemoveTuioCursor;
                }
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[Custom Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                // Unregister from TUIO events
                Dispatcher.OnObjectAdd -= AddTuioObject;
                Dispatcher.OnObjectRemove -= RemoveTuioObject;
                
                // Unregister from TUIO cursor events if they were enabled
                if (_enableCursorEvents)
                {
                    Dispatcher.OnCursorAdd -= AddTuioCursor;
                    Dispatcher.OnCursorUpdate -= UpdateTuioCursor;
                    Dispatcher.OnCursorRemove -= RemoveTuioCursor;
                }
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[Custom Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }
        
        /// <summary>
        /// Determine if a TUIO position should use the first display based on coordinates
        /// </summary>
        private bool ShouldUseFirstDisplay(float x, float y)
        {
            // In dual display mode, we can divide the TUIO space (0-1) between the two displays
            // A common approach is to split horizontally at x=0.5
            
            // Check if we are actually in dual display mode
            if (DualDisplayTuioTransform.DualScreenMode)
            {
                // In true dual display mode, use x-coordinate to determine display
                // Left half (0-0.5) goes to first display, right half (0.5-1) goes to second
                return x < 0.5f;
            }
            else
            {
                // If not in dual display mode, use the inspector setting
                return _isFirstDisplay;
            }
        }
        
        #region TUIO Object Handling
        
        private void AddTuioObject(object sender, Tuio11Object tuioObject)
        {
            var objectBehaviour = Instantiate(_objectPrefab, transform);
            
            var customBehaviour = objectBehaviour.gameObject.AddComponent<CustomTuioBehaviour>();
            customBehaviour.Initialize(tuioObject, _isFirstDisplay);
            _tuioBehaviours.Add(tuioObject.SessionId, customBehaviour);
            
            // Notify the puck manager if available
            if (_puckManager != null)
            {
                _puckManager.OnPuckAdded(tuioObject.SessionId, (int)tuioObject.SymbolId, customBehaviour);
            }
            
            // Fire the object created event
            OnObjectCreated?.Invoke(customBehaviour.gameObject);
        }
        
        private void RemoveTuioObject(object sender, Tuio11Object tuioObject)
        {
            if (_tuioBehaviours.TryGetValue(tuioObject.SessionId, out var objectBehaviour))
            {
                // Notify the puck manager if available
                if (_puckManager != null)
                {
                    _puckManager.OnPuckRemoved(tuioObject.SessionId);
                }
                
                _tuioBehaviours.Remove(tuioObject.SessionId);
                objectBehaviour.Destroy();
            }
        }
        
        #endregion
        
        #region TUIO Cursor Handling
        
        private void AddTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            if (!_enableCursorEvents) return;
            
            GameObject cursorObj = null;
            bool isFirstDisplay = ShouldUseFirstDisplay(tuioCursor.Position.X, tuioCursor.Position.Y);
            
            // Create a cursor visual if prefab is assigned (optional)
            if (_cursorPrefab != null)
            {
                // Instantiate the cursor object
                cursorObj = Instantiate(_cursorPrefab.gameObject, transform);
                
                // Destroy the default Tuio11CursorTransform component
                var defaultBehavior = cursorObj.GetComponent<Tuio11CursorTransform>();
                if (defaultBehavior != null)
                {
                    Destroy(defaultBehavior);
                }
                
                // Add our custom behaviour instead
                var customBehaviour = cursorObj.AddComponent<CustomTuioBehaviour>();
                customBehaviour.Initialize(tuioCursor, isFirstDisplay);
                
                // Store in dictionary
                _tuioCursors[tuioCursor.SessionId] = new CursorInfo
                {
                    CursorObject = cursorObj,
                    IsFirstDisplay = isFirstDisplay
                };
                
                // Fire the cursor created event
                OnCursorCreated?.Invoke(cursorObj);
            }
            
            // Get cursor position and transform to screen space
            Vector2 cursorPos = new Vector2(tuioCursor.Position.X, tuioCursor.Position.Y);
            Vector2 screenPos = GetCursorScreenPosition(cursorPos);
            
            // Send pointer down event to Unity's input system for proper button interaction
            TangibleTable.Shared.CursorPointerManager.SimulatePointerEvent(screenPos, TangibleTable.Shared.CursorPointerManager.PointerEventType.Down, (int)tuioCursor.SessionId);
            
            // Invoke the cursor down event
            OnCursorDown?.Invoke(screenPos);
        }
        
        private void UpdateTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            if (!_enableCursorEvents) return;
            
            // Get cursor position and transform to screen space
            Vector2 cursorPos = new Vector2(tuioCursor.Position.X, tuioCursor.Position.Y);
            Vector2 screenPos = GetCursorScreenPosition(cursorPos);
            
            // Send pointer move event to Unity's input system for proper drag handling
            TangibleTable.Shared.CursorPointerManager.SimulatePointerEvent(screenPos, TangibleTable.Shared.CursorPointerManager.PointerEventType.Move, (int)tuioCursor.SessionId);
            
            // Invoke the cursor move event
            OnCursorMove?.Invoke(screenPos);
        }
        
        private void RemoveTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            if (!_enableCursorEvents) return;
            
            // Get cursor position and transform to screen space
            Vector2 cursorPos = new Vector2(tuioCursor.Position.X, tuioCursor.Position.Y);
            Vector2 screenPos = GetCursorScreenPosition(cursorPos);
            
            // Send pointer up event to Unity's input system
            TangibleTable.Shared.CursorPointerManager.SimulatePointerEvent(screenPos, TangibleTable.Shared.CursorPointerManager.PointerEventType.Up, (int)tuioCursor.SessionId);
            
            // Remove cursor visual if it exists
            if (_tuioCursors.TryGetValue(tuioCursor.SessionId, out var cursorInfo))
            {
                Destroy(cursorInfo.CursorObject);
                _tuioCursors.Remove(tuioCursor.SessionId);
            }
            
            // Invoke the cursor up event
            OnCursorUp?.Invoke(screenPos);
        }
        
        private Vector2 GetCursorScreenPosition(Vector2 tuioPosition)
        {
            // Transform TUIO position (0-1) to screen position based on display
            if (_isFirstDisplay)
            {
                Vector3 screenPos = DualDisplayTuioTransform.GetFirstDisplayPosition(tuioPosition);
                return new Vector2(screenPos.x, screenPos.y);
            }
            else
            {
                Vector3 screenPos = DualDisplayTuioTransform.GetSecondDisplayPosition(tuioPosition);
                return new Vector2(screenPos.x, screenPos.y);
            }
        }
        
        #endregion
    }
} 