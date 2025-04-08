using System;
using System.Collections.Generic;
using TuioNet.Tuio11;
using TuioUnity.Common;
using TuioUnity.Tuio11;
using TuioUnity.Utils;
using UnityEngine;

namespace TangibleTable
{
    /// <summary>
    /// Custom TUIO 1.1 visualizer for dual/single display setup.
    /// This visualizer focuses only on TUIO objects (not cursors or blobs).
    /// </summary>
    public class CustomTuio11Visualizer : MonoBehaviour
    {
        [SerializeField] private TuioSessionBehaviour _tuioSessionBehaviour;
        [SerializeField] private Tuio11ObjectTransform _objectPrefab;
        
        // Set to true if this visualizer is for the first display, false for second display
        [SerializeField] private bool _isFirstDisplay = true;
        
        // Enable debug mode for coordinate transformation
        [SerializeField] private bool _debugMode = false;
        
        // Color to use for objects from this display
        [SerializeField] private Color _objectColor = Color.white;

        private readonly Dictionary<uint, CustomTuio11Behaviour> _tuioBehaviours = new Dictionary<uint, CustomTuio11Behaviour>();

        private Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)_tuioSessionBehaviour.TuioDispatcher;

        private void OnEnable()
        {
            try
            {
                DualDisplayTuioTransform.DebugMode = _debugMode;
                
                // Register for TUIO object events only
                Dispatcher.OnObjectAdd += AddTuioObject;
                Dispatcher.OnObjectRemove += RemoveTuioObject;
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[Custom Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                // Unregister from TUIO events
                Dispatcher.OnObjectAdd -= AddTuioObject;
                Dispatcher.OnObjectRemove -= RemoveTuioObject;
            }
            catch (InvalidCastException exception)
            {
                Debug.LogError($"[Custom Tuio Client] Check the TUIO-Version on the TuioSession object. {exception.Message}");
            }
        }
        
        private void AddTuioObject(object sender, Tuio11Object tuioObject)
        {
            var objectBehaviour = Instantiate(_objectPrefab, transform);
            
            // Try to set color if there's a renderer component
            var TUIO = objectBehaviour.GetComponentsInChildren<TuioDebug>();
            foreach (var tuioDebug in TUIO)
            {
                if (tuioDebug.tuioColor != null)
                    tuioDebug.tuioColor = _objectColor;
            }
            
            // Try to set color if there's an image component
            var images = objectBehaviour.GetComponentsInChildren<UnityEngine.UI.Image>();
            foreach (var image in images)
            {
                image.color = _objectColor;
            }
            
            var customBehaviour = objectBehaviour.gameObject.AddComponent<CustomTuio11Behaviour>();
            customBehaviour.Initialize(tuioObject, _isFirstDisplay);
            _tuioBehaviours.Add(tuioObject.SessionId, customBehaviour);
        }
        
        private void RemoveTuioObject(object sender, Tuio11Object tuioObject)
        {
            if (_tuioBehaviours.TryGetValue(tuioObject.SessionId, out var objectBehaviour))
            {
                _tuioBehaviours.Remove(tuioObject.SessionId);
                objectBehaviour.Destroy();
            }
        }
    }
} 