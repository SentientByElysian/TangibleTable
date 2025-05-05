using System;
using System.Collections.Generic;
using TangibleTable.Shared;
using TuioNet.Tuio11;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TangibleTable.SingleDisplay
{
    /// <summary>
    /// Custom TUIO 1.1 visualizer for single display setup.
    /// This visualizer handles TUIO objects and cursor input.
    /// </summary>
    public class TUIOCursorManager : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] private TuioSessionBehaviour _tuioSessionBehaviour;
        [SerializeField] private CustomTuioBehaviour _cursorPrefab;
        
        [Header("Cursor Events")]
        public UnityEvent<Vector2> OnCursorDown;
        public UnityEvent<Vector2> OnCursorMove;
        public UnityEvent<Vector2> OnCursorUp;


        // Event triggered when a new cursor is created
        public event Action<CustomTuioBehaviour> OnCursorCreated;
        
        private readonly Dictionary<uint, CustomTuioBehaviour> _tuioCursors = new Dictionary<uint, CustomTuioBehaviour>();
        
        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSessionBehaviour.TuioDispatcher;
        
        private void Start()
        {
            try
            {
                   Dispatcher.OnCursorAdd += AddTuioCursor;
                    Dispatcher.OnCursorUpdate += UpdateTuioCursor;
                    Dispatcher.OnCursorRemove += RemoveTuioCursor;
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
                 Dispatcher.OnCursorAdd -= AddTuioCursor;
                    Dispatcher.OnCursorUpdate -= UpdateTuioCursor;
                    Dispatcher.OnCursorRemove -= RemoveTuioCursor;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[Single Display Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }
        
        #region TUIO Cursor Handling
        
        private void AddTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            // Create a cursor visual if prefab is assigned (optional)
            if (_cursorPrefab != null)
            {
                // Instantiate the cursor object
                var cursorBehaviour = Instantiate(_cursorPrefab, TuioDebugger.Instance.DebugCanvas.transform);
                cursorBehaviour.Initialize(tuioCursor);
                
                // Store in dictionary
                _tuioCursors[tuioCursor.SessionId] = cursorBehaviour;
                
                // Fire the cursor created event
                OnCursorCreated?.Invoke(cursorBehaviour);
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