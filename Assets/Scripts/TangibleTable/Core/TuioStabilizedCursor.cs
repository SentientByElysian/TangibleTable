using TangibleTable.Core.Managers;
using UnityEngine;
using System.Collections.Generic;

namespace TangibleTable.Core.Behaviours.Cursors.Extended
{
    public class TuioStabilizedCursor : TuioCursor
    {
        [SerializeField] private List<BaseModifier> modifiers = new();
        
        private PositionStabilizer positionStabilizer;
        
        
        protected override void Awake()
        {
            base.Awake();
            positionStabilizer = modifiers.Find(m => m is PositionStabilizer) as PositionStabilizer;
        }
        
        public void ApplySettings(List<BaseModifierSettings> settingsList)
        {
            foreach (var setting in settingsList)
            {
                foreach (var modifier in modifiers)
                {
                    if (modifier.ModifierId == setting.ModifierId)
                    {
                        modifier.Initialize(setting);
                        break;
                    }
                }
            }
        }
        
        protected override void OnInitialize()
        {
            Vector2 stabilizedPos = positionStabilizer.SetTarget(position);
            SimulateUIEvents(stabilizedPos, CursorPointerManager.PointerEventType.Down);
        }
        
        protected override void OnUpdate()
        {
            Vector2 stabilizedPos = positionStabilizer.SetTarget(position);
            SimulateUIEvents(stabilizedPos, CursorPointerManager.PointerEventType.Move);
        }
        
        protected override void OnRemove()
        {
            Vector2 stabilizedPos = positionStabilizer.GetCurrent();
            SimulateUIEvents(stabilizedPos, CursorPointerManager.PointerEventType.Up);
        }
    }
}