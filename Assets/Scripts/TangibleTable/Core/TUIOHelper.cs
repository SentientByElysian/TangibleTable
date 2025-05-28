using TuioNet.Tuio11;
using UnityEngine;

public static class TUIOHelper 
{
    /// <summary>
    /// Convert TUIO cursor position to screen position and apply an offset.
    /// </summary>
    public static Vector2 GetCursorScreenPosition(Tuio11Cursor cursor, Vector2 offset)
    {
        float screenX = cursor.Position.X * Screen.width + offset.x;
        float screenY = (1 - cursor.Position.Y) * Screen.height + offset.y;
            
        return new Vector2(screenX, screenY);
    }
    
    /// <summary>
    /// Convert TUIO cursor position to screen position
    /// </summary>
    public static Vector2 GetCursorScreenPosition(Tuio11Cursor cursor)
    {
        float screenX = cursor.Position.X * Screen.width;
        float screenY = (1 - cursor.Position.Y) * Screen.height;
            
        return new Vector2(screenX, screenY);
    }
}
