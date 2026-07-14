using System;
using Microsoft.Xna.Framework;

namespace FloatMath;

/// <summary>
/// Rectangle represented by two points
/// </summary>
public class Rect
{
    private Vector2
        topLeft,
        bottomRight;

    #region getset
    public Vector2 TopLeft
    {
        get { return topLeft; }
    }

    public Vector2 TopRight
    {
        get { return new Vector2(Right, Top); }
    }

    public Vector2 BottomLeft
    {
        get { return new Vector2(Left, Bottom); }
    }

    public Vector2 BottomRight
    {
        get { return bottomRight; }
    }

    public float Left
    {
        get { return topLeft.X; }
    }

    public float Right
    {
        get { return bottomRight.X; }
    }

    public float Top
    {
        get { return topLeft.Y; }
    }

    public float Bottom
    {
        get { return bottomRight.Y; }
    }

    public float Width
    {
        get { return bottomRight.X - topLeft.X; }
    }

    public float Height
    {
        get { return bottomRight.Y - topLeft.Y; }
    }

    #endregion

    public Rect(Vector2 a, Vector2 b)
    {
        //Normalize and store the points
        topLeft = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        bottomRight = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    public void Expand(float size)
    {
        topLeft.X -= size;
        topLeft.Y -= size;
        bottomRight.X += size;
        bottomRight.Y += size;
    }

    /// <summary>
    /// Gets the bounding box of two rectangles
    /// </summary>
    public static Rect GetRect(Rect a, Rect b)
    {
        return new Rect(new Vector2(Math.Min(a.topLeft.X, b.topLeft.X), Math.Min(a.topLeft.Y, b.topLeft.Y)), new Vector2(Math.Max(a.bottomRight.X, b.bottomRight.X), Math.Max(a.bottomRight.Y, b.bottomRight.Y)));
    }

    public static bool PtInRect(Vector2 pt, Rect rect)
    {
        return (pt.X >= rect.Left && pt.X <= rect.Right && pt.Y >= rect.Top && pt.Y <= rect.Bottom);
    }

    public static bool Overlap(Rect a, Rect b)
    {
        return (((a.Left > b.Left && a.Left < b.Right) || (a.Right > b.Left && a.Right < b.Right) || (a.Left < b.Left && a.Right > b.Right)) && ((a.Top > b.Top && a.Top < b.Bottom) || (a.Bottom > b.Top && a.Bottom < b.Bottom) || (a.Top < b.Top && a.Bottom > b.Bottom)));
    }
}
