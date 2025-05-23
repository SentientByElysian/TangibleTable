using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TangibleTable.Core.Managers
{
    /// <summary>
    /// Static class to handle mapping between TUIO cursors and Unity's pointer data
    /// </summary>
    public static class CursorPointerManager
    {
        // Dictionary to track persisted pointer data
        private static Dictionary<int, PointerEventData> _pointerCache = new();
        
        // Enum for pointer event types 
        public enum PointerEventType { Down, Move, Up }
        
        // Flag to ensure initialization only happens once
        private static bool _initialized = false;
        
        /// <summary>
        /// Initialize the Event System and necessary raycasters
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            // Make sure we have an EventSystem
            if (EventSystem.current == null)
            {
                GameObject eventSystemObj = new("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                UnityEngine.Debug.Log("[CursorPointerManager] Created EventSystem");
            }
            
            // Make sure the main camera has a Physics Raycaster if there are any 3D UI elements
            if (Camera.main != null && Camera.main.GetComponent<PhysicsRaycaster>() == null)
            {
                bool needs3DRaycaster = false;
                
                // Check if there are any world space canvases
                Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.renderMode == RenderMode.WorldSpace)
                    {
                        needs3DRaycaster = true;
                        break;
                    }
                }
                
                if (needs3DRaycaster)
                {
                    Camera.main.gameObject.AddComponent<PhysicsRaycaster>();
                    UnityEngine.Debug.Log("[CursorPointerManager] Added PhysicsRaycaster to main camera");
                }
            }
            
            _initialized = true;
        }
        
        /// <summary>
        /// Simulates pointer events to ensure UI elements like buttons respond properly to TUIO cursors
        /// </summary>
        public static void SimulatePointerEvent(Vector2 screenPos, PointerEventType eventType, int sessionId)
        {
            // Ensure the system is initialized
            EnsureInitialized();
            
            // Ensure EventSystem exists
            if (EventSystem.current == null)
                return;
            
            // Find UI elements at the current position
            PointerEventData pointerData;
            
            // Use cached pointer data if it exists
            if (!_pointerCache.TryGetValue(sessionId, out pointerData))
            {
                pointerData = new PointerEventData(EventSystem.current);
                pointerData.pointerId = sessionId;
                _pointerCache[sessionId] = pointerData;
            }
            
            pointerData.position = screenPos;
            pointerData.delta = pointerData.position - pointerData.pressPosition;
            
            // Create a list for raycast results
            var results = new List<RaycastResult>();
            
            // Raycast to find UI elements
            EventSystem.current.RaycastAll(pointerData, results);
            
            // Debug output to help identify issues
            if (results.Count > 0)
            {
                // UnityEngine.Debug.Log($"[CursorPointerManager] Hit element: {results[0].gameObject.name}");
            }
            else
            {
                // UnityEngine.Debug.Log($"[CursorPointerManager] No elements hit at position {screenPos}");
            }
            
            // Process the event based on type
            switch (eventType)
            {
                case PointerEventType.Down:
                    // Set press position for tracking drags
                    pointerData.pressPosition = screenPos;
                    pointerData.eligibleForClick = true;
                    
                    // Clear any existing pressed state
                    pointerData.pointerPress = null;
                    
                    // Send pointer down events
                    if (results.Count > 0)
                    {
                        // Send pointer enter first
                        var enterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(results[0].gameObject);
                        if (enterHandler != null)
                        {
                            ExecuteEvents.Execute(
                                enterHandler,
                                pointerData,
                                ExecuteEvents.pointerEnterHandler);
                        }
                        
                        // Execute pointer down on the hit element
                        var downHandler = ExecuteEvents.GetEventHandler<IPointerDownHandler>(results[0].gameObject);
                        if (downHandler != null)
                        {
                            ExecuteEvents.Execute(
                                downHandler,
                                pointerData,
                                ExecuteEvents.pointerDownHandler);
                                
                            // Set pressed object for tracking
                            pointerData.pointerPress = downHandler;
                        }
                        
                        // Find drag handler
                        var dragHandler = ExecuteEvents.GetEventHandler<IDragHandler>(results[0].gameObject);
                        if (dragHandler != null)
                        {
                            pointerData.pointerDrag = dragHandler;
                            
                            // Also trigger begin drag event
                            ExecuteEvents.Execute(
                                dragHandler,
                                pointerData,
                                ExecuteEvents.initializePotentialDrag);
                        }
                    }
                    break;
                    
                case PointerEventType.Move:
                    // Send drag events if we have a pressed object
                    if (pointerData.pointerDrag != null)
                    {
                        // Store the current object under pointer
                        GameObject currentObjectUnderPointer = null;
                        if (results.Count > 0)
                        {
                            currentObjectUnderPointer = results[0].gameObject;
                        }
                        
                        // Execute drag event
                        ExecuteEvents.Execute(
                            pointerData.pointerDrag,
                            pointerData,
                            ExecuteEvents.dragHandler);
                        
                        // Check if we've dragged off the original pressed object
                        bool isStillOverPressedObject = false;
                        if (currentObjectUnderPointer != null && pointerData.pointerPress != null)
                        {
                            // Check if the pressed object is still under the pointer
                            isStillOverPressedObject = 
                                pointerData.pointerPress == ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentObjectUnderPointer);
                        }
                        
                        // If we moved off the pressed object, send exit and up events to deselect it
                        if (!isStillOverPressedObject && pointerData.pointerPress != null)
                        {
                            // Force exit on the original pressed object
                            ExecuteEvents.Execute(
                                pointerData.pointerPress,
                                pointerData,
                                ExecuteEvents.pointerExitHandler);
                                
                            // Force up on the original pressed object to ensure it's not stuck in pressed state
                            ExecuteEvents.Execute(
                                pointerData.pointerPress,
                                pointerData,
                                ExecuteEvents.pointerUpHandler);
                                
                            // Reset press state
                            pointerData.pointerPress = null;
                            pointerData.eligibleForClick = false;
                        }
                        
                        // Handle entering new elements
                        if (currentObjectUnderPointer != null && 
                            ExecuteEvents.GetEventHandler<IPointerEnterHandler>(currentObjectUnderPointer) != pointerData.pointerEnter)
                        {
                            // Exit old enter target
                            if (pointerData.pointerEnter != null)
                            {
                                ExecuteEvents.Execute(
                                    pointerData.pointerEnter,
                                    pointerData,
                                    ExecuteEvents.pointerExitHandler);
                            }
                            
                            // Enter new target
                            var newEnterTarget = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(currentObjectUnderPointer);
                            if (newEnterTarget != null)
                            {
                                ExecuteEvents.Execute(
                                    newEnterTarget,
                                    pointerData,
                                    ExecuteEvents.pointerEnterHandler);
                            }
                            
                            pointerData.pointerEnter = newEnterTarget;
                        }
                    }
                    break;
                    
                case PointerEventType.Up:
                    // Send pointer up events if we have a pressed object
                    if (pointerData.pointerPress != null)
                    {
                        // Execute pointer up on the pressed object
                        ExecuteEvents.Execute(
                            pointerData.pointerPress,
                            pointerData,
                            ExecuteEvents.pointerUpHandler);
                        
                        // Check if pointer is still over the same object for click
                        if (results.Count > 0 && 
                            ExecuteEvents.GetEventHandler<IPointerClickHandler>(results[0].gameObject) == pointerData.pointerPress && 
                            pointerData.eligibleForClick)
                        {
                            // Execute click if pointer is still over same object
                            ExecuteEvents.Execute(
                                pointerData.pointerPress,
                                pointerData,
                                ExecuteEvents.pointerClickHandler);
                                
                            UnityEngine.Debug.Log($"[CursorPointerManager] Clicked on {pointerData.pointerPress.name}");
                        }
                        
                        // Clear pressed state
                        pointerData.pointerPress = null;
                    }
                    
                    // End any drag operation
                    if (pointerData.pointerDrag != null)
                    {
                        ExecuteEvents.Execute(
                            pointerData.pointerDrag,
                            pointerData,
                            ExecuteEvents.endDragHandler);
                            
                        // Clear drag state
                        pointerData.pointerDrag = null;
                    }
                    
                    // Handle exit if needed
                    if (pointerData.pointerEnter != null)
                    {
                        ExecuteEvents.Execute(
                            pointerData.pointerEnter,
                            pointerData,
                            ExecuteEvents.pointerExitHandler);
                            
                        pointerData.pointerEnter = null;
                    }
                        
                    // Reset the state
                    pointerData.eligibleForClick = false;
                    
                    // Remove cached pointer data
                    _pointerCache.Remove(sessionId);
                    break;
            }
        }
        
        /// <summary>
        /// Clear all cached pointer data
        /// </summary>
        public static void ClearAll()
        {
            _pointerCache.Clear();
        }
    }
} 