using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UTool.TabSystem;
using TJ.Utils;
using TangibleTable.Core.Behaviours;
using TangibleTable.Core.Behaviours.Visualization;

namespace TangibleTable.Core.Debugging
{
    /// <summary>
    /// Debug component to monitor TUIO input values and diagnose jitter issues.
    /// Attach to any object in the scene to monitor TUIO markers.
    /// </summary>
    [HasTabField]
    public class GlobalTuioDebugger : Singleton<GlobalTuioDebugger>
    {
        public Canvas DebugCanvas => _canvas;
        
        // Make right side panel accessible
        public RectTransform RightSidePanel => _panelRT;

        // Panel alignment options
        public enum PanelAlignment
        {
            Left,
            Right
        }

        [Header("Monitoring")]
        [TabField]
        [SerializeField] private int _markerIdToMonitor = -1; // Set to -1 to monitor all

        [TabField]
        [SerializeField] private bool _monitorRotation = true;

        [TabField]
        [SerializeField] private bool _monitorPosition = false;

        [TabField]
        [SerializeField] private bool _logToConsole = false;

        [Header("UI Display")]
        [TabField]
        public bool _displayDebug = true;

        [SerializeField] private PanelAlignment _panelAlignment = PanelAlignment.Right;

        [TabField]
        [SerializeField, Range(-100, 100)] private int _canvasSortingOrder = -10;

        [Header("Data Collection")]
        [TabField]
        [SerializeField] private bool _collectRotationData = true;

        [TabField]
        [SerializeField] private int _dataPointsToCollect = 100;

        // Debug data storage
        private List<TuioVisualizer> _trackedObjects = new List<TuioVisualizer>();
        private Dictionary<int, List<float>> _rotationHistory = new Dictionary<int, List<float>>();
        private Dictionary<int, Vector2> _positionHistory = new Dictionary<int, Vector2>();
        private Dictionary<int, float> _maxRotationDelta = new Dictionary<int, float>();
        private Dictionary<int, float> _avgRotationDelta = new Dictionary<int, float>();
        private Dictionary<int, int> _jitterCount = new Dictionary<int, int>();

        private float _updateInterval = 0.5f;
        private float _timer = 0f;

        private GameObject _canvasObj;
        private RectTransform _panelRT;
        private TextMeshProUGUI _debugText;
        private Canvas _canvas;

        public override void Awake()
        {
            base.Awake();
            CreateDebugUI();
            TabContent.SettingsApplied += OnDebugSettingsApplied;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            TabContent.SettingsApplied -= OnDebugSettingsApplied;
        }

        private void OnDebugSettingsApplied()
        {
            Debug.Log($"[GlobalTuioDebugger] Debug settings applied, _displayDebug: {_displayDebug}");
            
            // Update canvas visibility
            if (_canvasObj != null)
            {
                _canvasObj.SetActive(_displayDebug);
                Debug.Log($"[GlobalTuioDebugger] Canvas active state set to: {_canvasObj.activeSelf}");
                
                // Make sure the text is visible
                if (_debugText != null && _trackedObjects.Count == 0 && _displayDebug)
                {
                    _debugText.text = "Waiting for TUIO objects...";
                }
            }
            
            // Update canvas sorting order
            if (_canvas != null)
            {
                _canvas.sortingOrder = _canvasSortingOrder;
            }
            
            // Update panel alignment
            UpdatePanelAlignment();
            
            // Update debug visibility on all tracked objects
            foreach (var visualizer in _trackedObjects)
            {
                if (visualizer != null)
                {
                    visualizer.SetDebugMode(_displayDebug);
                }
            }
        }

        /// <summary>
        /// Set the panel alignment (left or right side of screen)
        /// </summary>
        public void SetAlignment(PanelAlignment alignment)
        {
            _panelAlignment = alignment;
            UpdatePanelAlignment();
        }

        /// <summary>
        /// Set the canvas sorting order to control display hierarchy
        /// </summary>
        public void SetSortingOrder(int order)
        {
            _canvasSortingOrder = order;
            if (_canvas != null)
            {
                _canvas.sortingOrder = _canvasSortingOrder;
            }
        }

        /// <summary>
        /// Add a TUIO object to be tracked by the debugger
        /// </summary>
        public void AddTrackedObject(TuioVisualizer obj)
        {
            if (obj == null || _trackedObjects.Contains(obj))
                return;

            _trackedObjects.Add(obj);

            // Initialize data collection for this object
            int symbolId = obj.IsCursor ? -1 : obj.GetSymbolId();
            
            if (!_rotationHistory.ContainsKey(symbolId))
            {
                _rotationHistory[symbolId] = new List<float>();
                _maxRotationDelta[symbolId] = 0f;
                _avgRotationDelta[symbolId] = 0f;
                _jitterCount[symbolId] = 0;
            }
            
            // Set debug mode based on current visibility
            obj.SetDebugMode(_displayDebug);
        }

        /// <summary>
        /// Remove a TUIO object from being tracked
        /// </summary>
        public void RemoveTrackedObject(TuioVisualizer obj)
        {
            if (obj == null)
                return;

            _trackedObjects.Remove(obj);
        }

        private void UpdatePanelAlignment()
        {
            if (_panelRT == null)
                return;

            switch (_panelAlignment)
            {
                case PanelAlignment.Left:
                    _panelRT.anchorMin = new Vector2(0, 0);
                    _panelRT.anchorMax = new Vector2(0.3f, 1);
                    _panelRT.pivot = new Vector2(0, 0.5f);
                    _panelRT.anchoredPosition = Vector2.zero;
                    break;

                case PanelAlignment.Right:
                default:
                    _panelRT.anchorMin = new Vector2(0.7f, 0);
                    _panelRT.anchorMax = new Vector2(1, 1);
                    _panelRT.pivot = new Vector2(1, 0.5f);
                    _panelRT.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        private void CreateDebugUI()
        {
            // Create canvas for debugging
            _canvasObj = new GameObject("TUIO Debug Canvas");
            _canvasObj.transform.SetParent(transform);
            _canvas = _canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _canvasSortingOrder; // Use the configurable sorting order
            _canvasObj.AddComponent<CanvasScaler>();
            _canvasObj.AddComponent<GraphicRaycaster>();

            // Create panel
            GameObject panelObj = new GameObject("Debug Panel");
            panelObj.transform.SetParent(_canvas.transform, false);
            _panelRT = panelObj.AddComponent<RectTransform>();
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            // Set panel position based on alignment
            UpdatePanelAlignment();

            // Create text
            GameObject textObj = new GameObject("Debug Text");
            textObj.transform.SetParent(panelObj.transform, false);
            RectTransform textRT = textObj.AddComponent<RectTransform>();
            _debugText = textObj.AddComponent<TextMeshProUGUI>();
            _debugText.fontSize = 16;
            _debugText.color = Color.white;
            _debugText.alignment = TextAlignmentOptions.TopLeft;
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.offsetMin = new Vector2(10, 10);
            textRT.offsetMax = new Vector2(-10, -10);

            // Ensure the debug UI is in the correct state initially
            if (_canvasObj != null)
            {
                _canvasObj.SetActive(_displayDebug);
                Debug.Log($"[GlobalTuioDebugger] Initial debug panel state: {_displayDebug}, _canvasObj active: {_canvasObj.activeSelf}");
            }
        }

        private void Update()
        {
            // Make sure canvas visibility matches _displayDebug every frame
            if (_canvasObj != null && _canvasObj.activeSelf != _displayDebug)
            {
                _canvasObj.SetActive(_displayDebug);
                Debug.LogWarning($"[GlobalTuioDebugger] Fixed debug panel visibility. Was: {!_displayDebug}, should be: {_displayDebug}");
            }
            
            _timer += Time.deltaTime;

            if (_timer >= _updateInterval)
            {
                _timer = 0f;

                // Update debug data
                UpdateDebugData();

                // Update UI
                if (_displayDebug && _debugText != null)
                {
                    UpdateDebugText();
                }

                // Log to console if enabled
                if (_logToConsole)
                {
                    foreach (var obj in _trackedObjects)
                    {
                        int symbolId = obj.IsCursor ? -1 : obj.GetSymbolId();
                        
                        if (_monitorRotation && _maxRotationDelta.ContainsKey(symbolId))
                        {
                            UnityEngine.Debug.Log($"TUIO ID: {symbolId} | Rotation: {obj.transform.eulerAngles.z:F2}° | " +
                                      $"Max Delta: {_maxRotationDelta[symbolId]:F2}° | Jitter Count: {_jitterCount[symbolId]}");
                        }
                    }
                }
            }
        }

        private void UpdateDebugData()
        {
            // Clean up null references
            _trackedObjects.RemoveAll(obj => obj == null);

            foreach (var obj in _trackedObjects)
            {
                int symbolId = obj.IsCursor ? -1 : obj.GetSymbolId();
                
                // Skip objects we're not monitoring
                if (_markerIdToMonitor != -1 && symbolId != _markerIdToMonitor)
                    continue;

                if (_monitorRotation)
                {
                    // Record current rotation
                    float currentRotation = obj.transform.eulerAngles.z;

                    // Add to history
                    if (_collectRotationData)
                    {
                        if (!_rotationHistory.ContainsKey(symbolId))
                        {
                            _rotationHistory[symbolId] = new List<float>();
                            _maxRotationDelta[symbolId] = 0f;
                            _avgRotationDelta[symbolId] = 0f;
                            _jitterCount[symbolId] = 0;
                        }
                        
                        _rotationHistory[symbolId].Add(currentRotation);

                        // Limit history size
                        if (_rotationHistory[symbolId].Count > _dataPointsToCollect)
                        {
                            _rotationHistory[symbolId].RemoveAt(0);
                        }

                        // Calculate max delta from history
                        if (_rotationHistory[symbolId].Count >= 2)
                        {
                            float prevRotation = _rotationHistory[symbolId][_rotationHistory[symbolId].Count - 2];
                            float delta = Mathf.Abs(Mathf.DeltaAngle(currentRotation, prevRotation));

                            // Update max delta
                            if (delta > _maxRotationDelta[symbolId])
                            {
                                _maxRotationDelta[symbolId] = delta;
                            }

                            // Calculate average delta
                            float sum = 0f;
                            for (int i = 1; i < _rotationHistory[symbolId].Count; i++)
                            {
                                sum += Mathf.Abs(Mathf.DeltaAngle(_rotationHistory[symbolId][i],
                                    _rotationHistory[symbolId][i - 1]));
                            }

                            _avgRotationDelta[symbolId] = sum / (_rotationHistory[symbolId].Count - 1);

                            // Detect jitter (small oscillations)
                            if (delta > 0.1f && delta < 3.0f)
                            {
                                _jitterCount[symbolId]++;
                            }
                        }
                    }
                }
                
                if (_monitorPosition)
                {
                    // Store current position
                    _positionHistory[symbolId] = new Vector2(obj.transform.position.x, obj.transform.position.y);
                }
            }
        }

        private string GetDeltaRotationInfo(TuioVisualizer visualizer)
        {
            if (visualizer == null) return "";
            
            float deltaAngle = visualizer.GetDeltaRotation();
            return $"Delta Rotation: {deltaAngle:F0}° (0-360°)\n";
        }

        private void UpdateDebugText()
        {
            if (_trackedObjects.Count == 0)
            {
                _debugText.text = "No TUIO objects to track";
                return;
            }

            string text = "<color=#FFCC00>=== TUIO DEBUG ===</color>\n\n";

            foreach (var obj in _trackedObjects)
            {
                // Skip if null (safety check)
                if (obj == null) continue;
                
                int symbolId = obj.IsCursor ? -1 : obj.GetSymbolId();
                
                // Skip objects we're not monitoring
                if (_markerIdToMonitor != -1 && symbolId != _markerIdToMonitor)
                    continue;

                // Use different colors for cursors and objects
                string titleColor = obj.IsCursor ? "#00CCFF" : "#FFAA00"; // Blue for cursors, Orange for objects
                text += $"<color={titleColor}>TUIO {(obj.IsCursor ? "Cursor" : "Object")} ID: {symbolId}</color>\n";

                if (_monitorRotation)
                {
                    text += $"Rotation: {obj.transform.eulerAngles.z:F2}°\n";
                    
                    // Add delta rotation information
                    text += GetDeltaRotationInfo(obj);
                    
                    if (_rotationHistory.ContainsKey(symbolId))
                    {
                        text += $"Max Delta: {_maxRotationDelta[symbolId]:F2}°\n";
                        text += $"Avg Delta: {_avgRotationDelta[symbolId]:F2}°\n";
                        text += $"Jitter Events: {_jitterCount[symbolId]}\n";
                    }

                    // Access stabilizer settings directly from the component
                    var stabilizer = obj.GetComponent<TuioStabilizer>();
                    if (stabilizer != null)
                    {
                        text += $"Stabilizer: {(stabilizer.enabled ? "ON" : "OFF")}\n";
                    }
                }

                if (_monitorPosition)
                {
                    text += $"Position: ({obj.transform.position.x:F1}, {obj.transform.position.y:F1})\n";
                }

                text += "\n";
            }

            _debugText.text = text;
        }
    }
} 