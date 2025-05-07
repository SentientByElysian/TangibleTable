using System;
using System.Collections.Generic;
using TuioNet.Tuio11;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.Events;
using TangibleTable.Core.Behaviours;
using TangibleTable.Core.Behaviours.Visualization;
using TangibleTable.Core.Behaviours.Settings;
using TangibleTable.Core.Debugging;
using UTool.TabSystem;

namespace TangibleTable.Core.Managers
{
    /// <summary>
    /// Manages TUIO cursors (touch/click input) on the table.
    /// </summary>
    [RequireComponent(typeof(TuioSettings))]
    public class TuioCursorManager : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] protected TuioSessionBehaviour tuioSessionBehaviour;
        [SerializeField] protected TuioVisualizer cursorPrefab;
        
        [Header("UI Interaction")]
        [SerializeField] protected bool enableUIInteraction = true;
        [SerializeField] protected bool showCursorVisual = true;
        
        // Reference to the settings component
        protected TuioSettings _settings;
        
        [Header("Events")]
        public UnityEvent<Vector2> OnCursorDown = new();
        public UnityEvent<Vector2> OnCursorMove = new();
        public UnityEvent<Vector2> OnCursorUp = new();
        
        // Event for new cursor creation - useful for debugging/visualization
        public event Action<TuioVisualizer> OnCursorCreated;
        
        // Dictionary to track active cursors
        protected Dictionary<uint, TuioVisualizer> activeCursors = new();
        
        // Get dispatcher from session
        protected Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)tuioSessionBehaviour.TuioDispatcher;
        
        /// <summary>
        /// Initialize the cursor manager with required dependencies
        /// </summary>
        /// <param name="session">The TUIO session to use</param>
        /// <param name="prefab">The prefab to use for cursor visualization</param>
        public virtual void Initialize(TuioSessionBehaviour session, TuioVisualizer prefab)
        {
            this.tuioSessionBehaviour = session;
            this.cursorPrefab = prefab;
            
            if (isActiveAndEnabled && session != null)
            {
                RegisterTuioEvents();
            }
        }
        
        protected virtual void Awake()
        {
            // Get the settings component directly
            _settings = GetComponent<TuioSettings>();
            if (_settings == null)
            {
                _settings = gameObject.AddComponent<TuioSettings>();
                UnityEngine.Debug.Log("[TuioCursorManager] TuioSettings component added automatically");
            }
        }
        
        protected virtual void Start()
        {
            if (tuioSessionBehaviour == null)
            {
                UnityEngine.Debug.LogWarning("[TuioCursorManager] No TUIO session assigned! Cursors will not work.");
            }
            
            if (tuioSessionBehaviour != null)
            {
                RegisterTuioEvents();
            }
            
            // Subscribe to TabField settings applied event to update stabilizers
            TabContent.SettingsApplied += UpdateAllCursorStabilizers;
        }
        
        protected virtual void OnDestroy()
        {
            if (tuioSessionBehaviour != null)
            {
                try
                {
                    Dispatcher.OnCursorAdd -= AddTuioCursor;
                    Dispatcher.OnCursorUpdate -= UpdateTuioCursor;
                    Dispatcher.OnCursorRemove -= RemoveTuioCursor;
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[TuioCursorManager] Error removing TUIO event handlers: {exception.Message}");
                }
            }
            
            // Unsubscribe from TabField events
            TabContent.SettingsApplied -= UpdateAllCursorStabilizers;
        }
        
        protected virtual void RegisterTuioEvents()
        {
            try
            {
                Dispatcher.OnCursorAdd += AddTuioCursor;
                Dispatcher.OnCursorUpdate += UpdateTuioCursor;
                Dispatcher.OnCursorRemove += RemoveTuioCursor;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[TuioCursorManager] Error setting up TUIO event handlers: {exception.Message}");
            }
        }
        
        /// <summary>
        /// Update the stabilization settings on all tracked cursors
        /// </summary>
        protected virtual void UpdateAllCursorStabilizers()
        {
            foreach (var cursor in activeCursors.Values)
            {
                ConfigureCursorStabilizer(cursor);
            }
        }
        
        /// <summary>
        /// Configure stabilizer settings for a cursor visualizer
        /// </summary>
        protected virtual void ConfigureCursorStabilizer(TuioVisualizer visualizer)
        {
            if (visualizer == null) return;
            
            // Use the settings component to configure the cursor
            _settings.ConfigureCursorStabilizer(visualizer);
        }
        
        #region TUIO Cursor Handlers
        
        /// <summary>
        /// Handle a new cursor (touch down)
        /// </summary>
        protected virtual void AddTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            if (showCursorVisual && cursorPrefab != null)
            {
                Transform parent = null;
                
                if (GlobalTuioDebugger.Instance != null && GlobalTuioDebugger.Instance.DebugCanvas != null)
                {
                    parent = GlobalTuioDebugger.Instance.DebugCanvas.transform;
                }
                
                TuioVisualizer cursorBehaviour = null;
                if (parent != null)
                {
                    cursorBehaviour = Instantiate(cursorPrefab, parent);
                }
                else
                {
                    cursorBehaviour = Instantiate(cursorPrefab);
                }
                
                cursorBehaviour.Initialize(tuioCursor);
                
                // Configure cursor stabilizer settings
                ConfigureCursorStabilizer(cursorBehaviour);
                
                activeCursors[tuioCursor.SessionId] = cursorBehaviour;
                OnCursorCreated?.Invoke(cursorBehaviour);
            }
            
            Vector2 screenPos = GetCursorScreenPosition(tuioCursor);
            
            // Send pointer event for UI interaction
            if (enableUIInteraction)
            {
                CursorPointerManager.SimulatePointerEvent(
                    screenPos, 
                    CursorPointerManager.PointerEventType.Down, 
                    (int)tuioCursor.SessionId);
            }
            
            OnCursorDown.Invoke(screenPos);
        }
        
        /// <summary>
        /// Handle cursor movement (touch move)
        /// </summary>
        protected virtual void UpdateTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            Vector2 screenPos = GetCursorScreenPosition(tuioCursor);
            
            // Update cursor visual if it exists
            if (activeCursors.TryGetValue(tuioCursor.SessionId, out var cursorBehaviour))
            {
                // Update the TUIO behavior with new cursor data
                cursorBehaviour.UpdateCursor(tuioCursor);
            }
            
            // Send pointer event for UI interaction
            if (enableUIInteraction)
            {
                CursorPointerManager.SimulatePointerEvent(
                    screenPos, 
                    CursorPointerManager.PointerEventType.Move, 
                    (int)tuioCursor.SessionId);
            }
            
            OnCursorMove.Invoke(screenPos);
        }
        
        /// <summary>
        /// Handle cursor removal (touch up)
        /// </summary>
        protected virtual void RemoveTuioCursor(object sender, Tuio11Cursor tuioCursor)
        {
            Vector2 screenPos = GetCursorScreenPosition(tuioCursor);
            
            if (activeCursors.TryGetValue(tuioCursor.SessionId, out var cursorBehaviour))
            {
                activeCursors.Remove(tuioCursor.SessionId);
                
                cursorBehaviour?.Destroy();
            }
            
            // Send pointer event for UI interaction
            if (enableUIInteraction)
            {
                CursorPointerManager.SimulatePointerEvent(
                    screenPos, 
                    CursorPointerManager.PointerEventType.Up, 
                    (int)tuioCursor.SessionId);
            }
            
            OnCursorUp.Invoke(screenPos);
        }
        
        /// <summary>
        /// Convert TUIO cursor position to screen position
        /// </summary>
        protected virtual Vector2 GetCursorScreenPosition(Tuio11Cursor cursor)
        {
            // Apply the position offset from settings
            Vector2 offsetApplied = _settings.GetEffectiveCursorOffset();
            
            float screenX = cursor.Position.X * Screen.width + offsetApplied.x;
            float screenY = (1 - cursor.Position.Y) * Screen.height + offsetApplied.y;
            
            return new Vector2(screenX, screenY);
        }
        
        #endregion
    }
} 