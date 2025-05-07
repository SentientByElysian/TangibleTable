using System.Collections.Generic;
using UnityEngine;
using TuioNet.Tuio11;
using TuioUnity.Common;
using UnityEngine.Events;
using TangibleTable.Core.Behaviours;
using TangibleTable.Core.Behaviours.Pucks;
using TangibleTable.Core.Behaviours.Visualization;
using TangibleTable.Core.Behaviours.Settings;
using UTool.TabSystem;

namespace TangibleTable.Core.Managers
{
    /// <summary>
    /// Base manager class for handling TUIO pucks.
    /// Provides only the core functionality for tracking TUIO objects
    /// and creating/updating/removing pucks.
    /// </summary>
    [RequireComponent(typeof(TuioSettings))]
    public class TuioPuckManager : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] protected TuioSessionBehaviour tuioSession;
        [SerializeField] protected GameObject puckPrefab;
        
        [Header("Canvas Settings")]
        [SerializeField] protected Canvas targetCanvas;
        
        // Reference to the settings component
        protected TuioSettings _settings;
        
        // Events
        public UnityEvent<TuioPuck> OnPuckAdded = new();
        public UnityEvent<TuioPuck> OnPuckUpdated = new();
        public UnityEvent<TuioPuck> OnPuckRemoved = new();
        
        // Dictionary to track active pucks by session ID
        protected Dictionary<uint, TuioPuck> activePucks = new();
        
        // Get the TUIO dispatcher from the session behaviour
        protected Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)tuioSession.TuioDispatcher;
        
        /// <summary>
        /// Initialize the manager with required dependencies
        /// </summary>
        /// <param name="session">The TUIO session to use</param>
        /// <param name="prefab">The prefab to use for pucks</param>
        public virtual void Initialize(TuioSessionBehaviour session, GameObject prefab)
        {
            this.tuioSession = session;
            this.puckPrefab = prefab;
            
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
                UnityEngine.Debug.Log("[TuioPuckManager] TuioSettings component added automatically");
            }
        }
        
        protected virtual void Start()
        {
            if (tuioSession == null)
            {
                UnityEngine.Debug.LogWarning("[TuioPuckManager] No TUIO session assigned! Pucks will not work.");
            }
            
            if (targetCanvas == null)
            {
                UnityEngine.Debug.LogWarning("[TuioPuckManager] No Canvas assigned! UI elements may not be visible.");
            }
            
            if (tuioSession != null)
            {
                RegisterTuioEvents();
            }
            
            // Subscribe to TabField settings applied event to update stabilizers
            TabContent.SettingsApplied += UpdateAllStabilizers;
        }
        
        protected virtual void OnDestroy()
        {
            if (tuioSession != null)
            {
                try
                {
                    Dispatcher.OnObjectAdd -= AddTuioObject;
                    Dispatcher.OnObjectUpdate -= UpdateTuioObject;
                    Dispatcher.OnObjectRemove -= RemoveTuioObject;
                }
                catch (System.Exception exception)
                {
                    UnityEngine.Debug.LogError($"[TuioPuckManager] Error removing TUIO event handlers: {exception.Message}");
                }
            }
            
            // Unsubscribe from TabField events
            TabContent.SettingsApplied -= UpdateAllStabilizers;
        }
        
        protected virtual void RegisterTuioEvents()
        {
            try
            {
                Dispatcher.OnObjectAdd += AddTuioObject;
                Dispatcher.OnObjectUpdate += UpdateTuioObject;
                Dispatcher.OnObjectRemove += RemoveTuioObject;
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogError($"[TuioPuckManager] Error setting up TUIO event handlers: {exception.Message}");
            }
        }
        
        /// <summary>
        /// Update the stabilization settings on all tracked pucks
        /// </summary>
        protected virtual void UpdateAllStabilizers()
        {
            foreach (var puck in activePucks.Values)
            {
                if (puck?.TuioBehaviour != null)
                {
                    ConfigureStabilizer(puck.TuioBehaviour);
                }
            }
        }
        
        /// <summary>
        /// Configure stabilizer settings for a TuioVisualizer
        /// </summary>
        protected virtual void ConfigureStabilizer(TuioVisualizer visualizer)
        {
            if (visualizer == null) return;
            
            // Use the settings component to configure the visualizer
            _settings.ConfigureObjectStabilizer(visualizer);
        }
        
        #region TUIO Object Handlers
        
        /// <summary>
        /// Handler for when a new TUIO object is detected
        /// </summary>
        protected virtual void AddTuioObject(object sender, Tuio11Object tuioObject)
        {
            if (puckPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[TuioPuckManager] No puck prefab assigned.");
                return;
            }
            
            TuioVisualizer behaviour = CreateVisual(tuioObject);
            if (behaviour == null) return;
            
            // Configure stabilizer with current settings
            ConfigureStabilizer(behaviour);
            
            TuioPuck puck = CreatePuck(tuioObject, behaviour);
            if (puck == null) return;
            
            activePucks[tuioObject.SessionId] = puck;
            OnPuckAdded.Invoke(puck);
        }
        
        /// <summary>
        /// Handler for when a TUIO object is updated
        /// </summary>
        protected virtual void UpdateTuioObject(object sender, Tuio11Object tuioObject)
        {
            if (activePucks.TryGetValue(tuioObject.SessionId, out TuioPuck puck))
            {
                // Update the puck's data object
                puck.UpdatePuck(tuioObject);
                
                // Also update the visual representation
                puck.TuioBehaviour?.UpdateObject(tuioObject);
                
                OnPuckUpdated.Invoke(puck);
            }
        }
        
        /// <summary>
        /// Handler for when a TUIO object is removed
        /// </summary>
        protected virtual void RemoveTuioObject(object sender, Tuio11Object tuioObject)
        {
            if (activePucks.TryGetValue(tuioObject.SessionId, out TuioPuck puck))
            {
                puck.Remove();
                OnPuckRemoved.Invoke(puck);
                activePucks.Remove(tuioObject.SessionId);
                
                if (puck.gameObject != null)
                {
                    Destroy(puck.gameObject);
                }
            }
        }
        
        #endregion
        
        #region Puck Creation
        
        /// <summary>
        /// Create the visual TuioVisualizer for a TUIO object
        /// </summary>
        protected virtual TuioVisualizer CreateVisual(Tuio11Object tuioObject)
        {
            // Determine parent transform - use canvas if available or fallback to this transform
            Transform parent = (targetCanvas != null) ? targetCanvas.transform : transform;
            
            // Instantiate the prefab
            GameObject puckObject = Instantiate(puckPrefab, parent);
            
            // Get or add TuioVisualizer
            TuioVisualizer behaviour = puckObject.GetComponent<TuioVisualizer>();
            if (behaviour == null)
            {
                Debug.LogError("[TuioPuckManager] Prefab does not have a TuioVisualizer component");
                Destroy(puckObject);
                return null;
            }
            
            // Check if puck has RectTransform (UI element)
            if (puckObject.GetComponent<RectTransform>() != null && targetCanvas == null && !parent.GetComponentInParent<Canvas>())
            {
                Debug.LogWarning("[TuioPuckManager] UI object not parented to Canvas! It will not be visible.");
            }
            
            behaviour.Initialize(tuioObject);
            
            return behaviour;
        }
        
        /// <summary>
        /// Create a TuioPuck instance for a TUIO object
        /// </summary>
        protected virtual TuioPuck CreatePuck(Tuio11Object tuioObject, TuioVisualizer behaviour)
        {
            TuioPuck puck = behaviour.gameObject.GetComponent<TuioPuck>();
            
            if (puck == null)
            {
                Debug.LogError($"[TuioPuckManager] Prefab '{behaviour.gameObject.name}' does not have a TuioPuck or derived component! " +
                                           $"Add TuioPuck or a custom TuioPuck subclass to your prefab.");
                return null;
            }
            
            // Initialize the puck
            puck.Initialize(tuioObject, behaviour);
            
            return puck;
        }
        
        #endregion
        
        /// <summary>
        /// Get a puck by its session ID
        /// </summary>
        public TuioPuck GetPuck(uint sessionId)
        {
            if (activePucks.TryGetValue(sessionId, out TuioPuck puck))
            {
                return puck;
            }
            return null;
        }
        
        /// <summary>
        /// Get all active pucks
        /// </summary>
        public IEnumerable<TuioPuck> GetAllPucks()
        {
            return activePucks.Values;
        }
    }
} 