using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TangibleTable.SingleDisplay
{
    /// <summary>
    /// Handles TUIO cursor interactions from a single display and converts them to Unity events.
    /// Can be used to handle clicks and interact with UI elements.
    /// 
    /// Setup requirements:
    /// - Attach this to a GameObject with a SingleDisplayTuio11Visualizer component
    /// - Make sure your main camera has a PhysicsRaycaster component
    /// - Ensure your scene has an EventSystem
    /// </summary>
    public class SingleDisplayCursorHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SingleDisplayTuio11Visualizer _tuioVisualizer;
        [SerializeField] private EventSystem _eventSystem;
        
        [Header("Settings")]
        [SerializeField] private bool _simulateMouseClicks = true;
        [SerializeField] private bool _debugCursorEvents = false;
        
        [Header("Camera")]
        [Tooltip("Make sure this camera has a PhysicsRaycaster component")]
        [SerializeField] private Camera _uiCamera;
        
        // For tracking cursor state
        private Vector2 _lastCursorPosition;
        private bool _cursorDown = false;
        
        private void Awake()
        {
            // Get the visualizer if not set
            if (_tuioVisualizer == null)
            {
                _tuioVisualizer = GetComponent<SingleDisplayTuio11Visualizer>();
                if (_tuioVisualizer == null)
                {
                    Debug.LogError("No SingleDisplayTuio11Visualizer found on this GameObject. The cursor handler won't work.");
                    enabled = false;
                    return;
                }
            }
            
            // Try to find the main camera if UI camera not set
            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
            }
            
            // Check if the camera has a PhysicsRaycaster
            if (_uiCamera != null && _uiCamera.GetComponent<PhysicsRaycaster>() == null)
            {
                Debug.LogWarning("The UI camera doesn't have a PhysicsRaycaster component. UI interactions may not work correctly.");
            }
            
            // Make sure we have an event system
            if (_eventSystem == null)
            {
                _eventSystem = FindObjectOfType<EventSystem>();
                if (_eventSystem == null)
                {
                    Debug.LogWarning("No EventSystem found. UI interactions won't work.");
                }
            }
        }
        
        private void OnEnable()
        {
            // Register for cursor events
            if (_tuioVisualizer != null)
            {
                _tuioVisualizer.OnCursorDown.AddListener(HandleCursorDown);
                _tuioVisualizer.OnCursorMove.AddListener(HandleCursorMove);
                _tuioVisualizer.OnCursorUp.AddListener(HandleCursorUp);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from cursor events
            if (_tuioVisualizer != null)
            {
                _tuioVisualizer.OnCursorDown.RemoveListener(HandleCursorDown);
                _tuioVisualizer.OnCursorMove.RemoveListener(HandleCursorMove);
                _tuioVisualizer.OnCursorUp.RemoveListener(HandleCursorUp);
            }
        }
        
        private void HandleCursorDown(Vector2 position)
        {
            _lastCursorPosition = position;
            _cursorDown = true;
            
            if (_debugCursorEvents)
            {
                Debug.Log($"Cursor Down at {position}");
            }
            
            if (_simulateMouseClicks)
            {
                // Simulate mouse press for UI interaction
                SimulateMouseEvent(position, PointerEventData.InputButton.Left, true);
            }
        }
        
        private void HandleCursorMove(Vector2 position)
        {
            _lastCursorPosition = position;
            
            if (_debugCursorEvents)
            {
                Debug.Log($"Cursor Move to {position}");
            }
            
            if (_simulateMouseClicks && _cursorDown)
            {
                // Simulate mouse drag for UI interaction
                SimulateMouseDrag(position);
            }
        }
        
        private void HandleCursorUp(Vector2 position)
        {
            _lastCursorPosition = position;
            _cursorDown = false;
            
            if (_debugCursorEvents)
            {
                Debug.Log($"Cursor Up at {position}");
            }
            
            if (_simulateMouseClicks)
            {
                // Simulate mouse release for UI interaction
                SimulateMouseEvent(position, PointerEventData.InputButton.Left, false);
            }
        }
        
        /// <summary>
        /// Simulates a mouse press or release event at the specified position
        /// </summary>
        private void SimulateMouseEvent(Vector2 position, PointerEventData.InputButton button, bool isPressed)
        {
            // We need an event system to do this
            if (_eventSystem == null) return;
            
            // Get the event data
            var pointerData = new PointerEventData(_eventSystem);
            pointerData.position = position;
            pointerData.button = button;
            
            // Raycast to find hit objects
            var results = new System.Collections.Generic.List<RaycastResult>();
            _eventSystem.RaycastAll(pointerData, results);
            
            // Sort results by depth (closest first)
            results.Sort((a, b) => a.depth.CompareTo(b.depth));
            
            if (results.Count > 0)
            {
                // Set the current pointerEnter
                pointerData.pointerEnter = results[0].gameObject;
                
                // Process the event
                if (isPressed)
                {
                    // Handle press
                    ExecuteEvents.Execute(pointerData.pointerEnter, pointerData, ExecuteEvents.pointerDownHandler);
                }
                else
                {
                    // Handle release (and possibly click)
                    ExecuteEvents.Execute(pointerData.pointerEnter, pointerData, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(pointerData.pointerEnter, pointerData, ExecuteEvents.pointerClickHandler);
                }
            }
        }
        
        /// <summary>
        /// Simulates a mouse drag event
        /// </summary>
        private void SimulateMouseDrag(Vector2 position)
        {
            // We need an event system to do this
            if (_eventSystem == null) return;
            
            // Get the event data
            var pointerData = new PointerEventData(_eventSystem);
            pointerData.position = position;
            pointerData.button = PointerEventData.InputButton.Left;
            
            // Raycast to find hit objects
            var results = new System.Collections.Generic.List<RaycastResult>();
            _eventSystem.RaycastAll(pointerData, results);
            
            // Sort results by depth (closest first)
            results.Sort((a, b) => a.depth.CompareTo(b.depth));
            
            if (results.Count > 0)
            {
                // Set the current pointerDrag
                pointerData.pointerDrag = results[0].gameObject;
                
                // Process the drag event
                ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.dragHandler);
            }
        }
    }
} 