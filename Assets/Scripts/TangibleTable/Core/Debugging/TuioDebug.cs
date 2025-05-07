using UnityEngine;
using TMPro;

namespace TangibleTable.Core.Debugging
{
    /// <summary>
    /// Debug component for TUIO objects that shows or hides based on global debug settings.
    /// Designed to be controlled by TuioVisualizer for visibility.
    /// </summary>
    public class TuioDebug : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI debugText;

        /// <summary>
        /// Show or hide the debug object manually.
        /// Called by TuioVisualizer which stays active and monitors the global setting.
        /// </summary>
        public void UpdateVisibility(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// Update the debug text
        /// </summary>
        public void SetData(string sessionId, string symbolId, Vector2 position, float angle, float deltaAngle, string state = "")
        {
            if (debugText == null) return;
            
            string info = $"ID: {sessionId} | Symbol: {symbolId}\n" +
                          $"Pos: ({position.x:F2}, {position.y:F2})\n" +
                          $"Rot: {angle:F0}° | Delta: {deltaAngle:F0}°";
            
            if (!string.IsNullOrEmpty(state))
            {
                info += $"\nState: {state}";
            }
            
            debugText.text = info;
        }
    }
} 