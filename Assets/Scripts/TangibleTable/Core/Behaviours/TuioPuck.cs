using UnityEngine;
using TuioNet.Tuio11;
using TangibleTable.Core.Behaviours;

namespace TangibleTable.Core.Behaviours
{
    /// <summary>
    /// Base class for TUIO pucks (physical objects tracked by the TUIO system)
    /// Handles common properties and provides virtual methods for customization
    /// </summary>
    public class TuioPuck : MonoBehaviour
    {
        // Identification properties
        protected uint sessionId;
        protected int symbolId = -1;
        
        // Transform data
        protected Vector2 position;
        protected Vector2 previousPosition;
        protected Vector2 positionDelta;
        protected float rotation;
        protected float previousRotation;
        protected float rotationDelta;
        
        // Reference to the TUIO object from the tracker
        protected Tuio11Object tuioObject;
        
        // Reference to the TuioVisualizer 
        public TuioVisualizer TuioBehaviour { get; protected set; }
        
        // Status
        protected bool isActive = false;
        
        // Properties with public getters
        public uint SessionId => sessionId;
        public int SymbolId => symbolId;
        public Vector2 Position => position;
        public Vector2 PreviousPosition => previousPosition;
        public Vector2 PositionDelta => positionDelta;
        public float Rotation => rotation;
        public float PreviousRotation => previousRotation;
        public float RotationDelta => rotationDelta;
        public bool IsActive => isActive;
        
        /// <summary>
        /// Initialize the puck with a TUIO object
        /// </summary>
        public virtual void Initialize(Tuio11Object tuioObj, TuioVisualizer behaviour)
        {
            tuioObject = tuioObj;
            TuioBehaviour = behaviour;
            sessionId = tuioObj.SessionId;
            symbolId = (int)tuioObj.SymbolId;
            
            // Initialize position and rotation
            position = new Vector2(tuioObj.Position.X, tuioObj.Position.Y);
            previousPosition = position;
            positionDelta = Vector2.zero;
            
            rotation = Mathf.Rad2Deg * tuioObj.Angle;
            previousRotation = rotation;
            rotationDelta = 0f;
            
            isActive = true;
            
            OnInitialize();
        }
        
        /// <summary>
        /// Update the puck with new TUIO data
        /// </summary>
        public virtual void UpdatePuck(Tuio11Object tuioObj)
        {
            if (!isActive || tuioObject == null) return;
            
            // Store previous values
            previousPosition = position;
            previousRotation = rotation;
            
            // Update current values
            position = new Vector2(tuioObj.Position.X, tuioObj.Position.Y);
            rotation = Mathf.Rad2Deg * tuioObj.Angle;
            
            // Calculate deltas
            positionDelta = position - previousPosition;
            rotationDelta = Mathf.DeltaAngle(previousRotation, rotation);
            
            OnUpdate();
        }
        
        /// <summary>
        /// Called when the puck is removed
        /// </summary>
        public virtual void Remove()
        {
            if (!isActive) return;
            
            isActive = false;
            OnRemove();
        }
        
        /// <summary>
        /// Called after initialization
        /// </summary>
        protected virtual void OnInitialize() { }
        
        /// <summary>
        /// Called every time the puck is updated with new TUIO data
        /// </summary>
        protected virtual void OnUpdate() { }
        
        /// <summary>
        /// Called when the puck is removed
        /// </summary>
        protected virtual void OnRemove() { }
        
        /// <summary>
        /// Called every frame by Unity's Update loop
        /// </summary>
        protected virtual void Update()
        {
        }
    }
} 