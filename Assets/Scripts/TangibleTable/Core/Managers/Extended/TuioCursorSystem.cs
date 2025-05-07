using UnityEngine;

namespace TangibleTable.Core.Managers.Extended
{
    /// <summary>
    /// Enhanced TUIO cursor system that provides smoothing and UI interaction for touch input.
    /// Inherits from TuioCursorManager and adds TabField support for runtime configuration.
    /// </summary>
    public class TuioCursorSystem : TuioCursorManager
    {
        [Header("Debug Settings")]
        [SerializeField] private bool verboseDebugging = true;
        
        protected override void Start()
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log($"[TuioCursorSystem] Starting with session: {(tuioSessionBehaviour != null ? "Connected" : "NULL")}");
                UnityEngine.Debug.Log($"[TuioCursorSystem] Cursor prefab: {(cursorPrefab != null ? cursorPrefab.name : "NULL")}");
            }
            
            base.Start();
            
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioCursorSystem] Initialization complete - waiting for TUIO cursors");
            }
        }
        
        protected override void RegisterTuioEvents()
        {
            base.RegisterTuioEvents();
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioCursorSystem] TUIO events registered successfully");
            }
        }
        
        /// <summary>
        /// Update the stabilization settings on all tracked cursors
        /// </summary>
        protected override void UpdateAllCursorStabilizers()
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioCursorSystem] Updating cursor smoothing settings");
            }
            
            // Use base class to update all cursors with settings from TuioSettings
            base.UpdateAllCursorStabilizers();
        }
        
        /// <summary>
        /// Handle a new cursor (touch down)
        /// </summary>
        protected override void AddTuioCursor(object sender, TuioNet.Tuio11.Tuio11Cursor tuioCursor)
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log($"[TuioCursorSystem] TUIO cursor detected! ID: {tuioCursor.SessionId}");
            }
            
            base.AddTuioCursor(sender, tuioCursor);
        }
    }
} 