using UnityEngine;

namespace TangibleTable.Pucks
{
    /// <summary>
    /// Base class for puck states that can be attached to TUIO objects.
    /// Each puck state represents a different functionality or mode that is activated
    /// when the corresponding puck is placed on the table.
    /// </summary>
    public abstract class PuckState : ScriptableObject
    {
        [Header("Puck Settings")]
        [SerializeField] private string _stateName = "Default State";
        [SerializeField] private Color _puckColor = Color.white;
        [SerializeField] private int _symbolId = -1; // TUIO symbol ID for this puck
        
        /// <summary>
        /// Name of the state for display purposes
        /// </summary>
        public string StateName => _stateName;
        
        /// <summary>
        /// Color to use when displaying this puck
        /// </summary>
        public Color PuckColor => _puckColor;
        
        /// <summary>
        /// TUIO symbol ID associated with this puck
        /// </summary>
        public int SymbolId => _symbolId;
        
        /// <summary>
        /// Called when the puck is first placed on the table
        /// </summary>
        public abstract void OnActivate();
        
        /// <summary>
        /// Called when the puck is removed from the table
        /// </summary>
        public abstract void OnDeactivate();
        
        /// <summary>
        /// Called every frame while the puck is on the table
        /// </summary>
        /// <param name="position">Position of the puck in world space</param>
        /// <param name="rotation">Rotation of the puck in degrees</param>
        public abstract void OnUpdate(Vector3 position, float rotation);
    }
} 