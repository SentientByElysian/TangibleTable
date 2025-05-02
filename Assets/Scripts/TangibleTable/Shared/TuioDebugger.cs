using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UTool.TabSystem;

namespace TangibleTable.Shared
{
    /// <summary>
    /// Debug component to monitor TUIO input values and diagnose jitter issues.
    /// Attach to any object in the scene to monitor TUIO markers.
    /// </summary>
    [HasTabField]
    public class TuioDebugger : MonoBehaviour
    {
        // Singleton instance
        public static TuioDebugger Instance { get; private set; }
        
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
        [SerializeField] private bool _displayDebug = true;
        
        [SerializeField] private PanelAlignment _panelAlignment = PanelAlignment.Right;
        
        [TabField]
        [SerializeField, Range(-100, 100)] private int _canvasSortingOrder = -10;
        
        [Header("Data Collection")]
        [TabField]
        [SerializeField] private bool _collectRotationData = true;
        
        [TabField]
        [SerializeField] private int _dataPointsToCollect = 100;
        
        // Public property to access _displayDebug from other classes
        public bool ShowUIPanel
        {
            get { return _displayDebug; }
            set
            {
                if (_displayDebug != value)
                {
                    _displayDebug = value;
                    if (_canvasObj != null)
                    {
                        _canvasObj.SetActive(_displayDebug);
                    }
                }
            }
        }
        
        // Debug data storage
        private List<CustomTuio11Behaviour> _trackedObjects = new List<CustomTuio11Behaviour>();
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
        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create UI if needed
            if (_displayDebug)
            {
                CreateDebugUI();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
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
        public void AddTrackedObject(CustomTuio11Behaviour obj)
        {
            if (obj == null || _trackedObjects.Contains(obj))
                return;
                
            _trackedObjects.Add(obj);
            
            // Initialize data collection for this object
            if (!_rotationHistory.ContainsKey(obj._symbolId))
            {
                _rotationHistory[obj._symbolId] = new List<float>();
                _maxRotationDelta[obj._symbolId] = 0f;
                _avgRotationDelta[obj._symbolId] = 0f;
                _jitterCount[obj._symbolId] = 0;
            }
        }
        
        /// <summary>
        /// Remove a TUIO object from being tracked
        /// </summary>
        public void RemoveTrackedObject(CustomTuio11Behaviour obj)
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
                    _panelRT.pivot = new Vector2(0, 1);
                    break;
                    
                case PanelAlignment.Right:
                    _panelRT.anchorMin = new Vector2(0.7f, 0);
                    _panelRT.anchorMax = new Vector2(1, 1);
                    _panelRT.pivot = new Vector2(1, 1);
                    break;
            }
            
            _panelRT.anchoredPosition = Vector2.zero;
        }
        
        private void CreateDebugUI()
        {
            // Create canvas for debugging
            _canvasObj = new GameObject("TUIO Debug Canvas");
            _canvasObj.transform.SetParent(transform);
            _canvas = _canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _canvasSortingOrder; // Use the configurable sorting order
            _canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            _canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Create panel
            GameObject panelObj = new GameObject("Debug Panel");
            panelObj.transform.SetParent(_canvas.transform, false);
            _panelRT = panelObj.AddComponent<RectTransform>();
            UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
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
        }
        
        private void Start()
        {
            // Ensure the UI is correctly set up
            if (_displayDebug && _debugText == null)
            {
                CreateDebugUI();
            }
        }
        
        private void Update()
        {
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
                        if (_monitorRotation)
                        {
                            Debug.Log($"TUIO ID: {obj._symbolId} | Rotation: {obj.transform.eulerAngles.z:F2}° | " +
                                      $"Max Delta: {_maxRotationDelta[obj._symbolId]:F2}° | Jitter Count: {_jitterCount[obj._symbolId]}");
                        }
                    }
                }
            }
            
            // Update panel visibility
            if (_canvasObj != null)
            {
                // Always show panel when enabled, even if no objects are tracked
                _canvasObj.SetActive(_displayDebug);
                
                // Make sure the text is visible
                if (_debugText != null && _trackedObjects.Count == 0 && _displayDebug)
                {
                    _debugText.text = "Waiting for TUIO objects...";
                }
            }
            
            // Update sorting order if changed
            if (_canvas != null && _canvas.sortingOrder != _canvasSortingOrder)
            {
                _canvas.sortingOrder = _canvasSortingOrder;
            }
        }
        
        private void UpdateDebugData()
        {
            // Remove any null objects
            _trackedObjects.RemoveAll(obj => obj == null);
            
            foreach (var obj in _trackedObjects)
            {
                int id = obj._symbolId;
                
                if (_monitorRotation)
                {
                    // Record current rotation
                    float currentRotation = obj.transform.eulerAngles.z;
                    
                    // Add to history
                    if (_collectRotationData)
                    {
                        _rotationHistory[id].Add(currentRotation);
                        
                        // Limit history size
                        if (_rotationHistory[id].Count > _dataPointsToCollect)
                        {
                            _rotationHistory[id].RemoveAt(0);
                        }
                        
                        // Calculate max delta from history
                        if (_rotationHistory[id].Count >= 2)
                        {
                            float prevRotation = _rotationHistory[id][_rotationHistory[id].Count - 2];
                            float delta = Mathf.Abs(Mathf.DeltaAngle(currentRotation, prevRotation));
                            
                            // Update max delta
                            if (delta > _maxRotationDelta[id])
                            {
                                _maxRotationDelta[id] = delta;
                            }
                            
                            // Calculate average delta
                            float sum = 0f;
                            for (int i = 1; i < _rotationHistory[id].Count; i++)
                            {
                                sum += Mathf.Abs(Mathf.DeltaAngle(_rotationHistory[id][i], _rotationHistory[id][i-1]));
                            }
                            _avgRotationDelta[id] = sum / (_rotationHistory[id].Count - 1);
                            
                            // Detect jitter (small oscillations)
                            if (delta > 0.1f && delta < 3.0f)
                            {
                                _jitterCount[id]++;
                            }
                        }
                    }
                }
            }
        }
        
        private void UpdateDebugText()
        {
            if (_trackedObjects.Count == 0)
            {
                if (_debugText != null)
                    _debugText.text = "No TUIO objects to track";
                return;
            }
            
            string text = "=== TUIO DEBUG ===\n\n";
            
            foreach (var obj in _trackedObjects)
            {
                int id = obj._symbolId;
                
                text += $"<color=yellow>TUIO ID: {id}</color>\n";
                
                if (_monitorRotation)
                {
                    text += $"Rotation: {obj.transform.eulerAngles.z:F2}°\n";
                    text += $"Max Delta: {_maxRotationDelta[id]:F2}°\n";
                    text += $"Avg Delta: {_avgRotationDelta[id]:F2}°\n";
                    text += $"Jitter Events: {_jitterCount[id]}\n";
                    
                    // Get stability settings directly from the object
                    var field = obj.GetType().GetField("_useRotationStabilizer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool useStabilizer = field != null ? (bool)field.GetValue(obj) : false;
                    
                    var thresholdField = obj.GetType().GetField("_rotationStabilityThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    float threshold = thresholdField != null ? (float)thresholdField.GetValue(obj) : 0f;
                    
                    var framesField = obj.GetType().GetField("_stableFramesRequired", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int frames = framesField != null ? (int)framesField.GetValue(obj) : 0;
                    
                    text += $"<color=cyan>Stabilizer: {(useStabilizer ? "ON" : "OFF")}</color>\n";
                    text += $"Threshold: {threshold:F1}° | Frames: {frames}\n";
                }
                
                if (_monitorPosition)
                {
                    text += $"Position: ({obj.transform.position.x:F1}, {obj.transform.position.y:F1})\n";
                }
                
                text += "\n";
            }
            
            if (_debugText != null)
                _debugText.text = text;
        }
    }
} 