using TMPro;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using UnityEngine;
using UnityEngine.UI;

namespace TangibleTable
{
    /// <summary>
    /// Custom behavior for TUIO 1.1 objects that uses our dual-display coordinate transformation.
    /// </summary>
    public class CustomTuio11Behaviour : MonoBehaviour
    {
        private RectTransform _rectTransform;
        
        // Use the base object type instead
        private object _component;
        private uint _sessionId;
        private bool _isFirstDisplay;
        
        private Vector2 _tuioPosition = Vector2.zero;
        private float _angle;
        
        // Text component for displaying info
        private TextMeshProUGUI _tmpText;
        private Text _legacyText;
        private bool _textInitialized = false;
        
        // Should we display debug text on the object
        [SerializeField] private bool _showDebugText = true;
        
        // Symbol ID (for TUIO objects)
        private int _symbolId = -1;

        public void Initialize(Tuio11Cursor cursor, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = cursor;
            _sessionId = cursor.SessionId;
            _isFirstDisplay = isFirstDisplay;
            
            UpdateComponent();
        }

        public void Initialize(Tuio11Object obj, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = obj;
            _sessionId = obj.SessionId;
            _isFirstDisplay = isFirstDisplay;
            _symbolId = (int)obj.SymbolId;
            
            // Find and initialize text components
            FindTextComponents();
            
            UpdateComponent();
        }

        public void Initialize(Tuio11Blob blob, bool isFirstDisplay)
        {
            _rectTransform = GetComponent<RectTransform>();
            _component = blob;
            _sessionId = blob.SessionId;
            _isFirstDisplay = isFirstDisplay;
            
            UpdateComponent();
        }
        
        private void FindTextComponents()
        {
            if (_textInitialized) return;
            
            // Try to find TextMeshPro component
            _tmpText = GetComponentInChildren<TextMeshProUGUI>();
            
            // Try to find legacy Text component
            if (_tmpText == null)
            {
                _legacyText = GetComponentInChildren<Text>();
            }
            
            _textInitialized = true;
        }
        
        private void Update()
        {
            UpdateComponent();
        }

        private void UpdateComponent()
        {
            if (_component == null || _rectTransform == null)
                return;
                
            // Get position from component based on its type
            if (_component is Tuio11Cursor cursor)
            {
                _tuioPosition.x = cursor.Position.X;
                _tuioPosition.y = cursor.Position.Y;
                _angle = 0f;
            }
            else if (_component is Tuio11Object obj)
            {
                _tuioPosition.x = obj.Position.X;
                _tuioPosition.y = obj.Position.Y;
                _angle = Mathf.Rad2Deg * obj.Angle;
                _symbolId = (int)obj.SymbolId;
            }
            else if (_component is Tuio11Blob blob)
            {
                _tuioPosition.x = blob.Position.X;
                _tuioPosition.y = blob.Position.Y;
                _angle = 0f;
            }

            // Apply our custom coordinate transformation
            Vector3 screenPosition;
            if (_isFirstDisplay)
            {
                screenPosition = DualDisplayTuioTransform.GetFirstDisplayPosition(_tuioPosition);
            }
            else
            {
                screenPosition = DualDisplayTuioTransform.GetSecondDisplayPosition(_tuioPosition);
            }

            // Update the transform
            _rectTransform.position = screenPosition;
            _rectTransform.rotation = Quaternion.Euler(0, 0, _angle);
            
            // Update text display
            UpdateTextDisplay();
        }
        
        private void UpdateTextDisplay()
        {
            if (!_showDebugText) return;
            
            string displayText = GetDisplayText();
            
            // Update TextMeshPro if available
            if (_tmpText != null)
            {
                _tmpText.text = displayText;
            }
            // Update legacy Text if available
            else if (_legacyText != null)
            {
                _legacyText.text = displayText;
            }
        }
        
        private string GetDisplayText()
        {
            string displaySide = _isFirstDisplay ? "1" : "2";
            string position = $"({_tuioPosition.x:F2}, {_tuioPosition.y:F2})";
            string angleText = $"{_angle:F0}°";
            
            return $"ID: {_symbolId}\nPos: {position}\nAngle: {angleText}\nDisp: {displaySide}";
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
        
        public string DebugText()
        {
            string displayType = _isFirstDisplay ? "First" : "Second";
            return $"{displayType} Display | ID: {_sessionId} | Position: {_tuioPosition}";
        }
    }
} 