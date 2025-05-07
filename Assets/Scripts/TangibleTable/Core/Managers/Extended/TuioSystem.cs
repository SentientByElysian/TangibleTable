using UnityEngine;
using TangibleTable.Core.Behaviours.Pucks;
using TangibleTable.Core.Behaviours.Visualization;

namespace TangibleTable.Core.Managers.Extended
{
    /// <summary>
    /// TUIO system that inherits from TuioPuckManager to handle physical objects directly.
    /// 
    /// This system focuses exclusively on tracking physical objects (pucks)
    /// on the table surface. For cursor/touch input or debugging, add those
    /// components to your scene separately.
    /// </summary>
    public class TuioSystem : TuioPuckManager
    {
        [Header("Debug Settings")]
        [SerializeField] private bool verboseDebugging = true;
        
        protected override void Start()
        {
            // Add detailed debugging
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log($"[TuioSystem] Starting with session: {(tuioSession != null ? "Connected" : "NULL")}");
                UnityEngine.Debug.Log($"[TuioSystem] Puck prefab: {(puckPrefab != null ? puckPrefab.name : "NULL")}");
                UnityEngine.Debug.Log($"[TuioSystem] Canvas: {(targetCanvas != null ? targetCanvas.name : "NULL")}");
            }
            
            base.Start();
            
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioSystem] Initialization complete - waiting for TUIO objects");
            }
        }
        
        protected override void RegisterTuioEvents()
        {
            base.RegisterTuioEvents();
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioSystem] TUIO events registered successfully");
            }
        }
        
        /// <summary>
        /// Update the stabilization settings on all tracked pucks
        /// </summary>
        protected override void UpdateAllStabilizers()
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log("[TuioSystem] Updating stabilizer settings on all pucks");
            }
            
            // Update all pucks with settings from the TuioSettings component
            base.UpdateAllStabilizers();
        }
        
        /// <summary>
        /// Override puck creation
        /// </summary>
        protected override TuioPuck CreatePuck(TuioNet.Tuio11.Tuio11Object tuioObject, TuioVisualizer behaviour)
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log($"[TuioSystem] Creating puck with ID: {tuioObject.SessionId}, Symbol: {tuioObject.SymbolId}");
            }
            
            TuioPuck puck = base.CreatePuck(tuioObject, behaviour);
            
            return puck;
        }
        
        // Debug for when objects are added
        protected override void AddTuioObject(object sender, TuioNet.Tuio11.Tuio11Object tuioObject)
        {
            if (verboseDebugging)
            {
                UnityEngine.Debug.Log($"[TuioSystem] TUIO object detected! ID: {tuioObject.SessionId}, Symbol: {tuioObject.SymbolId}");
            }
            
            base.AddTuioObject(sender, tuioObject);
        }
    }
} 