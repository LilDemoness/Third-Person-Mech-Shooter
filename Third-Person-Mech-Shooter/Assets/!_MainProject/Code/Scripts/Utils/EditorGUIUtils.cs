#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class EditorGUIUtils
{
    public static void DrawHorizontalLine(float thickness, float topPadding = 0.0f, float bottomPadding = 0.0f, float leftPadding = 0.0f, float rightPadding = 0.0f)
    {
        const float WIDTH_EXTEND = 6.0f; // Extends the underline to touch both sides of the inspector when the left & right margins are 0.

        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(topPadding + bottomPadding + thickness));
        r.height = thickness;
        r.y += topPadding;
        r.x -= (WIDTH_EXTEND / 2.0f) - leftPadding;
        r.width += WIDTH_EXTEND - leftPadding - rightPadding;
        EditorGUI.DrawRect(r, Color.grey);
    }
}
#endif