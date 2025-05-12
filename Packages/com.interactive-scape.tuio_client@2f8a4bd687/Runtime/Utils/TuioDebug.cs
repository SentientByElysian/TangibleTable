using TMPro;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace TuioUnity.Utils
{
    /// <summary>
    /// Simple component to display properties of tuio objects in the scene and set a random color for easier
    /// distinction between objects or touches.
    /// </summary>
    [RequireComponent(typeof(TuioBehaviour))]
    public class TuioDebug : MonoBehaviour
    {
        [SerializeField] private TMP_Text _debugText;
        [SerializeField] private MaskableGraphic _background;

        private TuioBehaviour _tuioBehaviour;

        public Color tuioColor = Color.white;

        private void Start()
        {
            _tuioBehaviour = GetComponent<TuioBehaviour>();
            _background.color = tuioColor;
            _debugText.color = tuioColor;
        }

        private void Update()
        {
            _debugText.text = _tuioBehaviour.DebugText();
        }
    }
}
