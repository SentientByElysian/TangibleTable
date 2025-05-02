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

namespace TangibleTable.SingleDisplay
{
    /// <summary>
    /// Custom TUIO 1.1 visualizer for single display setup.
    /// This visualizer handles TUIO objects and cursor input.
    /// </summary>
    public class SingleDisplayTuio11Visualizer : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] private TuioSessionBehaviour _tuioSessionBehaviour;
        [SerializeField] private Tuio11ObjectTransform _objectPrefab;
        [SerializeField] private Tuio11CursorTransform _cursorPrefab;
        
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
        
        private readonly Dictionary<uint, CustomTuio11Behaviour> _tuioBehaviours = new Dictionary<uint, CustomTuio11Behaviour>();
        private readonly Dictionary<uint, CustomTuio11Behaviour> _tuioCursors = new Dictionary<uint, CustomTuio11Behaviour>();
        
        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSessionBehaviour.TuioDispatcher;
        
        private void Start()
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
            catch (System.Exception exception)
            {
                Debug.LogError($"[Single Display Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }
        
        private void OnDestroy()
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
            catch (System.Exception exception)
            {
                Debug.LogError($"[Single Display Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }
        
        #region TUIO Object Handling
        
        private void AddTuioObject(object sender, Tuio11Object tuioObject)
        {
            var objectBehaviour = Instantiate(_objectPrefab, transform);
            
            // Destroy the default Tuio11ObjectTransform component as we'll use our custom one
            Destroy(objectBehaviour as Component);
            
            // Add our custom behaviour instead
            var customBehaviour = objectBehaviour.gameObject.AddComponent<CustomTuio11Behaviour>();
            customBehaviour.Initialize(tuioObject); // Using single display mode
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
                
                objectBehaviour.Destroy();
                _tuioBehaviours.Remove(tuioObject.SessionId);
            }
        }
        
        #endregion
        
        #region TUIO Cursor Handling
        
        private void AddTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            if (!_enableCursorEvents) return;
            
            GameObject cursorObj = null;
            
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
                var customBehaviour = cursorObj.AddComponent<CustomTuio11Behaviour>();
                customBehaviour.Initialize(tuioCursor); // Using single display mode
                
                // Store in dictionary
                _tuioCursors[tuioCursor.SessionId] = customBehaviour;
                
                // Fire the cursor created event
                OnCursorCreated?.Invoke(cursorObj);
            }
            
            // Get cursor position and transform to screen space
            Vector2 cursorPos = new Vector2(tuioCursor.Position.X, tuioCursor.Position.Y);
            Vector2 screenPos = GetCursorScreenPosition(cursorPos);
            
            // Send pointer down event to Unity's input system for proper button interaction
            CursorPointerManager.SimulatePointerEvent(screenPos, CursorPointerManager.PointerEventType.Down, (int)tuioCursor.SessionId);
            
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
            CursorPointerManager.SimulatePointerEvent(screenPos, CursorPointerManager.PointerEventType.Move, (int)tuioCursor.SessionId);
            
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
            CursorPointerManager.SimulatePointerEvent(screenPos, CursorPointerManager.PointerEventType.Up, (int)tuioCursor.SessionId);
            
            // Remove cursor visual if it exists
            if (_tuioCursors.TryGetValue(tuioCursor.SessionId, out var cursorBehaviour))
            {
                _tuioCursors.Remove(tuioCursor.SessionId);
                cursorBehaviour.Destroy();
            }
            
            // Invoke the cursor up event
            OnCursorUp?.Invoke(screenPos);
        }
        
        private Vector2 GetCursorScreenPosition(Vector2 tuioPosition)
        {
            // Transform TUIO position (0-1) to screen position 
            Vector3 screenPos = TuioTransform.GetScreenPosition(tuioPosition);
            return new Vector2(screenPos.x, screenPos.y);
        }
        
        #endregion
    }
} 