using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Engine.Graphics;

/// <summary>
/// Dev QoL zoom:
/// - Mouse wheel zooms around cursor.
/// - GamePad bumpers zoom around screen center (controller-friendly).
///
/// Assumes Camera2D has Position (Vector2) and Zoom (float).
/// </summary>
public static class CameraZoom
{
    public sealed class State
    {
        public int PrevWheel;
        public bool Initialized;

        public float MinZoom = 0.50f;
        public float MaxZoom = 4.00f;

        /// <summary>~12% zoom per notch/bumper press (multiplicative).</summary>
        public float Step = 0.12f;
    }

    public static void ApplyMouseWheel(Camera2D cam, State st, int screenW, int screenH)
    {
        if (cam == null) throw new ArgumentNullException(nameof(cam));
        if (st == null) throw new ArgumentNullException(nameof(st));

        var ms = Mouse.GetState();

        if (!st.Initialized)
        {
            st.PrevWheel = ms.ScrollWheelValue;
            st.Initialized = true;
            return;
        }

        int wheelDelta = ms.ScrollWheelValue - st.PrevWheel;
        st.PrevWheel = ms.ScrollWheelValue;

        if (wheelDelta == 0) return;

        float notches = wheelDelta / 120f; // trackpads may be fractional
        ZoomAroundScreenPoint(cam, st, screenW, screenH, new Vector2(ms.X, ms.Y), notches);
    }

    public static void ApplyBumpers(Camera2D cam, State st, GamePadState pad, GamePadState prevPad, int screenW, int screenH)
    {
        if (cam == null) throw new ArgumentNullException(nameof(cam));
        if (st == null) throw new ArgumentNullException(nameof(st));

        // Edge-triggered: one step per press.
        bool zoomIn  = pad.IsButtonDown(Buttons.RightShoulder) && !prevPad.IsButtonDown(Buttons.RightShoulder);
        bool zoomOut = pad.IsButtonDown(Buttons.LeftShoulder)  && !prevPad.IsButtonDown(Buttons.LeftShoulder);

        if (!zoomIn && !zoomOut) return;

        float notches = zoomIn ? +1f : -1f;

        // Controller zoom anchors to screen center.
        var center = new Vector2(screenW * 0.5f, screenH * 0.5f);
        ZoomAroundScreenPoint(cam, st, screenW, screenH, center, notches);
    }

    private static void ZoomAroundScreenPoint(Camera2D cam, State st, int screenW, int screenH, Vector2 screenPoint, float notches)
    {
        float oldZoom = cam.Zoom;

        float zoomFactor = MathF.Pow(1f + st.Step, notches);
        float newZoom = Math.Clamp(oldZoom * zoomFactor, st.MinZoom, st.MaxZoom);

        if (MathF.Abs(newZoom - oldZoom) < 0.0001f)
            return;

        Vector2 worldBefore = ScreenToWorld(cam.Position, oldZoom, screenW, screenH, screenPoint);

        cam.Zoom = newZoom;

        Vector2 worldAfter = ScreenToWorld(cam.Position, newZoom, screenW, screenH, screenPoint);

        // Keep the same world point under the anchor screen point
        cam.Position += (worldBefore - worldAfter);
    }

    private static Vector2 ScreenToWorld(Vector2 camPos, float zoom, int screenW, int screenH, Vector2 screen)
    {
        // Matches a common Camera2D matrix:
        // world = camPos + (screen - screenCenter) / zoom
        var screenCenter = new Vector2(screenW * 0.5f, screenH * 0.5f);
        return camPos + (screen - screenCenter) / zoom;
    }
}
