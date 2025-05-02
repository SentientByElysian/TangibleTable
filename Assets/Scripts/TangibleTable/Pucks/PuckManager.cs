using System.Collections.Generic;
using UnityEngine;
using TangibleTable.Shared;
using UTool.TabSystem;

namespace TangibleTable.Pucks
{
    /// <summary>
    /// Manages the TUIO pucks and their associated states.
    /// Handles puck activation/deactivation and coordinates state changes.
    /// </summary>
    public class PuckManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color _defaultPuckColor = Color.gray;
        
        [Header("Puck States")]
        [SerializeField] private List<PuckState> _availablePuckStates = new List<PuckState>();
        
        [Header("Multiple Puck Behavior")]
        [Tooltip("How to handle when multiple pucks with states are placed")]
        [SerializeField] private MultiplePuckMode _multiplePuckMode = MultiplePuckMode.FirstOnly;
        
        // Dictionary to map symbol IDs to puck states
        private Dictionary<int, PuckState> _statesBySymbolId = new Dictionary<int, PuckState>();
        // Dictionary to track active pucks and their states
        private Dictionary<uint, PuckState> _activePucks = new Dictionary<uint, PuckState>();
        // Dictionary to track active pucks and their TUIO objects
        private Dictionary<uint, CustomTuio11Behaviour> _activePuckBehaviours = new Dictionary<uint, CustomTuio11Behaviour>();
        
        // Keep track of the most recently placed puck with a state
        private uint _mostRecentPuckId;
        
        private bool _initialized = false;
        
        /// <summary>
        /// The different ways to handle multiple active pucks
        /// </summary>
        public enum MultiplePuckMode
        {
            AllActive,       // All pucks with states are active simultaneously
            MostRecent,      // Only the most recently placed puck is active
            FirstOnly        // Only the first placed puck is active until removed
        }
        
        private void Start()
        {
            InitializePuckStates();
        }
        
        private void Update()
        {
            // Update all active pucks based on the multi-puck mode
            if (_multiplePuckMode == MultiplePuckMode.AllActive)
            {
                // Update all active pucks
                UpdateAllPucks();
            }
            else if (_multiplePuckMode == MultiplePuckMode.MostRecent && _activePuckBehaviours.ContainsKey(_mostRecentPuckId))
            {
                // Update only the most recent puck
                UpdateSinglePuck(_mostRecentPuckId);
            }
            else if (_multiplePuckMode == MultiplePuckMode.FirstOnly && _activePuckBehaviours.Count > 0)
            {
                // Find the first active puck (we don't track this separately, so get the first key)
                uint firstPuckId = GetFirstActivePuck();
                if (_activePuckBehaviours.ContainsKey(firstPuckId))
                {
                    UpdateSinglePuck(firstPuckId);
                }
            }
            
            // Check if debug state changed in TuioDebugger
            if (TuioDebugger.Instance != null && _lastDebugState != TuioDebugger.Instance.ShowUIPanel)
            {
                UpdateAllPucksDebugState();
                _lastDebugState = TuioDebugger.Instance.ShowUIPanel;
            }
        }
        
        // Track the last debug state
        private bool _lastDebugState = false;
        
        /// <summary>
        /// Updates the debug state on all active pucks
        /// </summary>
        private void UpdateAllPucksDebugState()
        {
            bool showDebug = TuioDebugger.Instance != null && TuioDebugger.Instance.ShowUIPanel;
            
            foreach (var behaviour in _activePuckBehaviours.Values)
            {
                if (behaviour != null)
                {
                    // Configure debug based on TuioDebugger's ShowUIPanel
                    ConfigurePuckDebug(behaviour, GetPuckState(behaviour), showDebug);
                }
            }
        }
        
        private PuckState GetPuckState(CustomTuio11Behaviour behaviour)
        {
            foreach (var kvp in _activePucks)
            {
                if (_activePuckBehaviours.TryGetValue(kvp.Key, out var b) && b == behaviour)
                {
                    return kvp.Value;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Update all active pucks with states
        /// </summary>
        private void UpdateAllPucks()
        {
            foreach (var kvp in _activePuckBehaviours)
            {
                uint sessionId = kvp.Key;
                UpdateSinglePuck(sessionId);
            }
        }
        
        /// <summary>
        /// Update a single puck by its session ID
        /// </summary>
        private void UpdateSinglePuck(uint sessionId)
        {
            if (_activePuckBehaviours.TryGetValue(sessionId, out CustomTuio11Behaviour behaviour) && 
                _activePucks.TryGetValue(sessionId, out PuckState state) && 
                state != null)
            {
                // Get position and rotation from the TUIO behaviour
                Vector3 position = behaviour.transform.position;
                float rotation = behaviour.transform.eulerAngles.z;
                
                // Update the puck state
                state.OnUpdate(position, rotation);
            }
        }
        
        /// <summary>
        /// Get the session ID of the first active puck
        /// </summary>
        private uint GetFirstActivePuck()
        {
            foreach (var kvp in _activePucks)
            {
                if (kvp.Value != null)
                {
                    return kvp.Key;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Initialize the puck states and map them by symbol ID
        /// </summary>
        private void InitializePuckStates()
        {
            if (_initialized) return;
            
            _statesBySymbolId.Clear();
            
            // Map each puck state to its symbol ID
            foreach (var state in _availablePuckStates)
            {
                if (state != null && state.SymbolId >= 0)
                {
                    _statesBySymbolId[state.SymbolId] = state;
                    Debug.Log($"Registered puck state '{state.StateName}' for symbol ID {state.SymbolId}");
                }
            }
            
            _initialized = true;
        }
        
        /// <summary>
        /// Called when a TUIO object is added to the table
        /// </summary>
        public void OnPuckAdded(uint sessionId, int symbolId, CustomTuio11Behaviour behaviour)
        {
            if (!_initialized)
                InitializePuckStates();
                
            // Check if this symbol ID has an associated state
            PuckState state = null;
            bool hasState = _statesBySymbolId.TryGetValue(symbolId, out state);
            
            if (hasState && state != null)
            {
                // This is a puck with a valid state
                
                // If we're using MostRecent mode, deactivate the previously active state
                if (_multiplePuckMode == MultiplePuckMode.MostRecent)
                {
                    DeactivateAllExcept(sessionId);
                }
                // If we're using FirstOnly mode and already have an active puck, don't activate this one
                else if (_multiplePuckMode == MultiplePuckMode.FirstOnly && HasActivePuckWithState())
                {
                    // Don't activate this one, but still track it
                    state = null;
                }
                else
                {
                    // Activate the puck state
                    state.OnActivate();
                }
                
                // Set the puck color based on its state
                SetPuckColor(behaviour, state != null ? state.PuckColor : _defaultPuckColor);
                
                // Configure debug settings
                bool showDebug = TuioDebugger.Instance != null && TuioDebugger.Instance.ShowUIPanel;
                ConfigurePuckDebug(behaviour, state, showDebug);
                
                // Track as most recent
                _mostRecentPuckId = sessionId;
            }
            else
            {
                // No state for this puck, use default color
                SetPuckColor(behaviour, _defaultPuckColor);
                
                // Still configure debug
                bool showDebug = TuioDebugger.Instance != null && TuioDebugger.Instance.ShowUIPanel;
                ConfigurePuckDebug(behaviour, null, showDebug);
            }
            
            // Track the active puck
            _activePucks[sessionId] = state;
            _activePuckBehaviours[sessionId] = behaviour;
        }
        
        /// <summary>
        /// Called when a TUIO object is removed from the table
        /// </summary>
        public void OnPuckRemoved(uint sessionId)
        {
            // Deactivate the puck state if it exists
            if (_activePucks.TryGetValue(sessionId, out PuckState state) && state != null)
            {
                state.OnDeactivate();
                
                // If this was the most recent or only active puck, update accordingly
                if (_multiplePuckMode == MultiplePuckMode.MostRecent && _mostRecentPuckId == sessionId)
                {
                    // Find the next most recent puck and activate it
                    ActivateMostRecentRemainingPuck();
                }
                else if (_multiplePuckMode == MultiplePuckMode.FirstOnly)
                {
                    // If the first puck was removed, activate the next one
                    ActivateNextPuckIfNeeded();
                }
            }
            
            // Remove from active pucks
            _activePucks.Remove(sessionId);
            _activePuckBehaviours.Remove(sessionId);
        }
        
        /// <summary>
        /// Check if we have any active pucks with states
        /// </summary>
        private bool HasActivePuckWithState()
        {
            foreach (var state in _activePucks.Values)
            {
                if (state != null)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Deactivate all puck states except for the specified one
        /// </summary>
        private void DeactivateAllExcept(uint activeSessionId)
        {
            foreach (var kvp in _activePucks)
            {
                uint sessionId = kvp.Key;
                PuckState state = kvp.Value;
                
                if (sessionId != activeSessionId && state != null)
                {
                    state.OnDeactivate();
                }
            }
        }
        
        /// <summary>
        /// Activate the most recently placed remaining puck
        /// </summary>
        private void ActivateMostRecentRemainingPuck()
        {
            // This is just a simple implementation - we're not tracking placement order
            // so we just activate any puck with a state
            foreach (var kvp in _activePucks)
            {
                if (kvp.Value != null)
                {
                    _mostRecentPuckId = kvp.Key;
                    kvp.Value.OnActivate();
                    return;
                }
            }
        }
        
        /// <summary>
        /// Activate the next puck if needed (for FirstOnly mode)
        /// </summary>
        private void ActivateNextPuckIfNeeded()
        {
            if (!HasActivePuckWithState())
            {
                // Try to find a puck with a state and activate it
                foreach (var kvp in _activePucks)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.OnActivate();
                        return;
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the color of a puck's visual representation
        /// </summary>
        private void SetPuckColor(CustomTuio11Behaviour behaviour, Color color)
        {
            if (behaviour == null) return;
            
            // Find Image components on the puck and set their color
            var images = behaviour.GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach (var image in images)
            {
                image.color = color;
            }
            
            // Check if it has a TuioDebug component and set its color
            var debug = behaviour.GetComponentsInChildren<TuioUnity.Utils.TuioDebug>();
            foreach (var debugComponent in debug)
            {
                debugComponent.tuioColor = color;
            }
        }
        
        /// <summary>
        /// Configure debug settings for a TUIO object
        /// </summary>
        private void ConfigurePuckDebug(CustomTuio11Behaviour behaviour, PuckState state, bool showDebug)
        {
            if (behaviour == null) return;
            
            // Set the internal debug text (always available)
            if (state != null)
            {
                behaviour.SetPuckState(state.StateName);
            }
            else
            {
                behaviour.SetPuckState("No State");
            }
            
            // Configure the debug panel setting
            var debugField = behaviour.GetType().GetField("_showDebugPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (debugField != null)
            {
                debugField.SetValue(behaviour, showDebug);
            }
        }
    }
} 