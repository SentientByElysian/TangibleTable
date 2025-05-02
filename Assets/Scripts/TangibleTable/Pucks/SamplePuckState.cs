using UnityEngine;

namespace TangibleTable.Pucks
{
    /// <summary>
    /// A sample puck state implementation to demonstrate the system.
    /// </summary>
    [CreateAssetMenu(fileName = "New Sample Puck State", menuName = "TangibleTable/Puck States/Sample State")]
    public class SamplePuckState : PuckState
    {
        [Header("Sample State Settings")]
        [SerializeField] private GameObject _prefabToSpawn;
        [SerializeField] private float _rotationMultiplier = 1.0f;
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;
        
        private GameObject _spawnedObject;
        
        public override void OnActivate()
        {
            Debug.Log($"Sample state activated: {StateName}");
            
            // Spawn a prefab if specified
            if (_prefabToSpawn != null)
            {
                _spawnedObject = Instantiate(_prefabToSpawn);
            }
        }
        
        public override void OnDeactivate()
        {
            Debug.Log($"Sample state deactivated: {StateName}");
            
            // Destroy spawned objects
            if (_spawnedObject != null)
            {
                Destroy(_spawnedObject);
                _spawnedObject = null;
            }
        }
        
        public override void OnUpdate(Vector3 position, float rotation)
        {
            // Update any spawned objects
            if (_spawnedObject != null)
            {
                // Update position with offset
                _spawnedObject.transform.position = position + _positionOffset;
                
                // Update rotation with multiplier
                _spawnedObject.transform.rotation = Quaternion.Euler(0, 0, rotation * _rotationMultiplier);
            }
        }
    }
} 