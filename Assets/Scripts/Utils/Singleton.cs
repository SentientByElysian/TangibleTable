using UnityEngine;

namespace TJ.Utils
{
    /// <summary>
    /// A basic singleton implementation that doesn't persist between scenes.
    /// This ensures that only one instance of a component type exists in the scene,
    /// but allows it to be destroyed when scenes change.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T instance;

        public static T Instance => instance;

        public virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                // DontDestroyOnLoad removed to allow scene transitions to destroy this object
                // This ensures clean scene loading/unloading
            }
            else
            {
                Destroy(gameObject);
                Debug.LogWarning($"Duplicate {typeof(T).Name} found and destroyed.");
            }
        }

        protected virtual void OnDestroy()
        {
            // When this object is destroyed (such as during scene changes),
            // clear the static instance reference if it's this instance
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}