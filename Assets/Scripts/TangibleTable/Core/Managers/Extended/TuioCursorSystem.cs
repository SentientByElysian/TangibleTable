using TangibleTable.Core.Behaviours.Cursors.Extended;
using UnityEngine;
using TuioNet.Tuio11;
using System.Collections.Generic;

namespace TangibleTable.Core.Managers.Extended
{
    public class TuioCursorSystem : TuioCursorManager
    {
        [Header("Modifier Settings")]
        [SerializeField] private List<BaseModifierSettings> modifierSettings = new();
        
        protected override void AddTuioCursor(object sender, Tuio11Cursor cursor)
        {
            if (cursorPrefab == null) return;
            
            Transform parent = cursorContainer != null ? cursorContainer : transform;
            TuioStabilizedCursor tuioCursor = Instantiate(cursorPrefab as TuioStabilizedCursor, parent);
            
            tuioCursor.ApplySettings(modifierSettings);
            tuioCursor.Initialize(cursor);
            
            activeCursors[cursor.SessionId] = tuioCursor;
            OnCursorAdded.Invoke(tuioCursor);
        }
    }
}