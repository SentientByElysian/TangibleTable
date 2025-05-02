using UnityEngine;

namespace TangibleTable.Pucks
{
    /// <summary>
    /// A puck state that changes the color of UI elements based on the puck's rotation.
    /// Demonstrates how to use the puck's rotation to control parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "New Color Changer Puck", menuName = "TangibleTable/Puck States/Color Changer")]
    public class ColorChangerPuckState : PuckState
    {
        [Header("Color Settings")]
        [SerializeField] private Gradient _colorGradient = new Gradient();
        [SerializeField] private string _targetTag = "ColorChangeable";
        
        [Header("Rotation Settings")]
        [SerializeField] private float _minRotation = 0f;
        [SerializeField] private float _maxRotation = 360f;
        
        private float _currentRotation = 0f;
        
        public override void OnActivate()
        {
            Debug.Log($"Color changer activated: {StateName}");
            UpdateTargetColors(0f);
        }
        
        public override void OnDeactivate()
        {
            Debug.Log($"Color changer deactivated: {StateName}");
        }
        
        public override void OnUpdate(Vector3 position, float rotation)
        {
            // Store the current rotation
            _currentRotation = rotation;
            
            // Update the color based on rotation
            UpdateTargetColors(rotation);
        }
        
        private void UpdateTargetColors(float rotation)
        {
            // Normalize rotation to 0-1 range
            float normalizedRotation = Mathf.InverseLerp(_minRotation, _maxRotation, rotation);
            
            // Get color from gradient
            Color color = _colorGradient.Evaluate(normalizedRotation);
            
            // Find all objects with the target tag
            GameObject[] targets = GameObject.FindGameObjectsWithTag(_targetTag);
            
            // Update colors
            foreach (GameObject target in targets)
            {
                // Update UI Images
                var images = target.GetComponentsInChildren<UnityEngine.UI.Image>();
                foreach (var image in images)
                {
                    image.color = color;
                }
                
                // Update UI Text
                var texts = target.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    text.color = color;
                }
                
                // Update Renderers
                var renderers = target.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }
    }
} 