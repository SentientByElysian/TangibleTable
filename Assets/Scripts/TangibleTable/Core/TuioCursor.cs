using TangibleTable.Core.Managers;
using UnityEngine;
using TuioNet.Tuio11;

namespace TangibleTable.Core.Behaviours.Cursors
{
    public class TuioCursor : MonoBehaviour
    {
        protected RectTransform rectTransform;
        protected uint          sessionId;
        protected Vector2       position;
        protected Tuio11Cursor  tuioCursor;
        protected bool          isActive = false;
        
        public uint SessionId => sessionId;
        public Vector2 Position => position;
        public bool IsActive => isActive;

        protected virtual void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public virtual void Initialize(Tuio11Cursor cursor)
        {
            tuioCursor = cursor;
            sessionId = cursor.SessionId;
            position = TUIOHelper.GetCursorScreenPosition(cursor);
            isActive = true;
            OnInitialize();
        }
        
        public virtual void UpdateCursor(Tuio11Cursor cursor)
        {
            if (!isActive || tuioCursor == null) return;
            position = TUIOHelper.GetCursorScreenPosition(cursor);
            OnUpdate();
        }
        
        public virtual void Remove()
        {
            if (!isActive) return;
            isActive = false;
            OnRemove();
        }

        protected virtual void OnInitialize()
        {
            MoveCursor();
            SimulateUIEvents(position, CursorPointerManager.PointerEventType.Down);
        }

        protected virtual void OnUpdate()
        {
            MoveCursor();
            SimulateUIEvents(position, CursorPointerManager.PointerEventType.Move);
        }

        protected virtual void OnRemove()
        {
            SimulateUIEvents(position, CursorPointerManager.PointerEventType.Up);
        }
        
        protected virtual void Update() { }
        
        protected void MoveCursor()
        {
            rectTransform.anchoredPosition = position;
        }

        public void SimulateUIEvents(Vector2 screenPos, CursorPointerManager.PointerEventType eventType)
        {
            CursorPointerManager.SimulatePointerEvent(
                screenPos, 
                eventType, 
                (int)sessionId);
        }
    }
}
