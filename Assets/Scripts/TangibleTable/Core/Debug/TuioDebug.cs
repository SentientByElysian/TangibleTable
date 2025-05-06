using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TangibleTable.Core.Debug
{
    /// <summary>
    /// Debug component for TUIO objects that displays information and visual overlays.
    /// Designed to be controlled by a parent CustomTuioBehaviour.
    /// </summary>
    public class TuioDebug : MonoBehaviour
    {
        [Header("Debug UI References")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI debugText;
        
        // Internal data
        private string _sessionId = "";
        private string _symbolId = "";
        private string _position = "";
        private string _rotation = "";
        private string _stateName = "";
        
        private void Awake()
        {
            // Make sure we have a debug panel and text
            if (debugPanel == null)
            {
                debugPanel = CreateDebugPanel();
            }
            
            if (debugText == null && debugPanel != null)
            {
                debugText = debugPanel.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        public void SetData(string sessionId, string symbolId, Vector2 position, float angle, string state = "")
        {
            _sessionId = sessionId;
            _symbolId = symbolId;
            _position = $"({position.x:F2}, {position.y:F2})";
            _rotation = $"{angle:F1}°";
            _stateName = state;
            
            UpdateDebugText();
        }
        
        /// <summary>
        /// Show or hide the debug panel
        /// </summary>
        /// <param name="visible">Whether the panel should be visible</param>
        public void UpdateVisibility(bool visible = false)
        {
            debugPanel?.SetActive(visible);
        }
        
        private void UpdateDebugText()
        {
            if (debugText != null)
            {
                debugText.text = $"ID: {_sessionId}\n" +
                                 $"Symbol: {_symbolId}\n" +
                                 $"Pos: {_position}\n" +
                                 $"Angle: {_rotation}";
                
                if (!string.IsNullOrEmpty(_stateName))
                {
                    debugText.text += $"\nState: {_stateName}";
                }
            }
        }
        
        private GameObject CreateDebugPanel()
        {
            // Determine the parent for the debug panel
            Transform panelParent;
            
            // If the GlobalTuioDebugger exists and has a right side panel, use that as parent
            if (GlobalTuioDebugger.Instance != null && GlobalTuioDebugger.Instance.DebugCanvas != null)
            {
                // Look for the right panel and container
                var rightPanel = GlobalTuioDebugger.Instance.DebugCanvas.transform.Find("Debug Panel");
                if (rightPanel != null)
                {
                    Transform container = rightPanel.Find("Content/Container");
                    if (container != null)
                    {
                        panelParent = container;
                    }
                    else
                    {
                        panelParent = rightPanel;
                    }
                }
                else
                {
                    // Fallback to the canvas itself
                    panelParent = GlobalTuioDebugger.Instance.DebugCanvas.transform;
                }
            }
            else
            {
                // Fallback to being a child of this object
                panelParent = transform;
            }
            
            // Create the debug panel
            GameObject panel = new("DebugPanel");
            panel.transform.SetParent(panelParent);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            
            // If it's parented to the right panel's container, use layout-friendly settings
            if (panelParent.name == "Container")
            {
                panelRect.anchorMin = new Vector2(0, 0);
                panelRect.anchorMax = new Vector2(1, 0);
                panelRect.pivot = new Vector2(0.5f, 0);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(0, 100);
            }
            else
            {
                // Otherwise use positioning relative to the TUIO object
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(150, 100);
            }
            
            // Add background image
            Image background = panel.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0.7f);
            
            // Add text component
            GameObject textObj = new("DebugText");
            textObj.transform.SetParent(panel.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(-20, -20);
            
            debugText = textObj.AddComponent<TextMeshProUGUI>();
            debugText.fontSize = 14;
            debugText.color = Color.white;
            debugText.alignment = TextAlignmentOptions.TopLeft;
            
            return panel;
        }
    }
} 