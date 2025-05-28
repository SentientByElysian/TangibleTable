using System;
using System.Collections.Generic;
using TuioNet.Tuio11;
using TuioUnity.Common;
using UnityEngine;
using UnityEngine.Events;
using TangibleTable.Core.Behaviours.Cursors;

namespace TangibleTable.Core.Managers
{
    public class TuioCursorManager : MonoBehaviour
    {
        [Header("TUIO Settings")]
        [SerializeField] protected TuioSessionBehaviour tuioSession;
        [SerializeField] protected TuioCursor cursorPrefab;
        [SerializeField] protected Transform cursorContainer;
        
        [Header("Events")]
        public UnityEvent<TuioCursor> OnCursorAdded = new();
        public UnityEvent<TuioCursor> OnCursorUpdated = new();
        public UnityEvent<TuioCursor> OnCursorRemoved = new();
        
        protected Dictionary<uint, TuioCursor> activeCursors = new();
        protected Tuio11Dispatcher Dispatcher => (Tuio11Dispatcher)tuioSession.TuioDispatcher;
        
        protected virtual void Start()
        {
            if (tuioSession == null)
            {
                Debug.LogError("[TuioCursorManager] No TUIO session assigned!");
                return;
            }
            RegisterTuioEvents();
        }
        
        protected virtual void OnDestroy()
        {
            UnregisterTuioEvents();
        }
        
        protected virtual void RegisterTuioEvents()
        {
            try
            {
                Dispatcher.OnCursorAdd += AddTuioCursor;
                Dispatcher.OnCursorUpdate += UpdateTuioCursor;
                Dispatcher.OnCursorRemove += RemoveTuioCursor;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TuioCursorManager] Failed to register TUIO events: {e.Message}");
            }
        }
        
        protected virtual void UnregisterTuioEvents()
        {
            if (tuioSession == null) return;
            try
            {
                Dispatcher.OnCursorAdd -= AddTuioCursor;
                Dispatcher.OnCursorUpdate -= UpdateTuioCursor;
                Dispatcher.OnCursorRemove -= RemoveTuioCursor;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TuioCursorManager] Failed to unregister TUIO events: {e.Message}");
            }
        }
        
        protected virtual void AddTuioCursor(object sender, Tuio11Cursor cursor)
        {
            if (cursorPrefab == null) return;
            
            Transform parent = cursorContainer != null ? cursorContainer : transform;
            TuioCursor tuioCursor = Instantiate(cursorPrefab, parent);
            
            tuioCursor.Initialize(cursor);
            activeCursors[cursor.SessionId] = tuioCursor;
            OnCursorAdded.Invoke(tuioCursor);
        }
        
        protected virtual void UpdateTuioCursor(object sender, Tuio11Cursor cursor)
        {
            if (activeCursors.TryGetValue(cursor.SessionId, out TuioCursor tuioCursor))
            {
                tuioCursor.UpdateCursor(cursor);
                OnCursorUpdated.Invoke(tuioCursor);
            }
        }
        
        protected virtual void RemoveTuioCursor(object sender, Tuio11Cursor cursor)
        {
            if (activeCursors.TryGetValue(cursor.SessionId, out TuioCursor tuioCursor))
            {
                tuioCursor.Remove();
                OnCursorRemoved.Invoke(tuioCursor);
                activeCursors.Remove(cursor.SessionId);
                if (tuioCursor.gameObject != null)
                {
                    Destroy(tuioCursor.gameObject);
                }
            }
        }
        
        public TuioCursor GetCursor(uint sessionId)
        {
            activeCursors.TryGetValue(sessionId, out TuioCursor cursor);
            return cursor;
        }
        
        public IEnumerable<TuioCursor> GetAllCursors()
        {
            return activeCursors.Values;
        }
    }
}