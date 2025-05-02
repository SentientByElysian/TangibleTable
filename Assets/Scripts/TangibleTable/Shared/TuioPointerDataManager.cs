using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TangibleTable.Shared
{
    /// <summary>
    /// Static manager class to track TUIO pointer data across frames.
    /// This ensures that UI elements respond correctly to TUIO cursor interactions.
    /// </summary>
    public static class TuioPointerDataManager
    {
        // Dictionary to track pointer data by session ID
        private static Dictionary<int, PointerEventData> _pointerDataMap = 
            new Dictionary<int, PointerEventData>();
        
        /// <summary>
        /// Get or create pointer data for a specific TUIO cursor
        /// </summary>
        /// <param name="sessionId">TUIO cursor session ID</param>
        /// <returns>Pointer event data for this cursor</returns>
        public static PointerEventData GetPointerData(int sessionId)
        {
            // Create new pointer data if it doesn't exist for this session
            if (!_pointerDataMap.TryGetValue(sessionId, out var pointerData))
            {
                pointerData = new PointerEventData(EventSystem.current);
                pointerData.pointerId = sessionId;
                _pointerDataMap[sessionId] = pointerData;
            }
            
            return pointerData;
        }
        
        /// <summary>
        /// Remove pointer data when a TUIO cursor is removed
        /// </summary>
        /// <param name="sessionId">TUIO cursor session ID</param>
        public static void RemovePointerData(int sessionId)
        {
            _pointerDataMap.Remove(sessionId);
        }
        
        /// <summary>
        /// Clear all tracked pointer data
        /// </summary>
        public static void ClearAllPointerData()
        {
            _pointerDataMap.Clear();
        }
    }
} 