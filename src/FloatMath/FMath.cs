using System;
using Microsoft.Xna.Framework;

namespace FloatMath;

/// <summary>
/// Thin float-typed math helpers. Most members delegate to MathF; the rest are
/// small geometry utilities not covered by the BCL.
/// </summary>
public class FMath
{
    public static float Abs(float x) => MathF.Abs(x);

    public static float Pow(float x, float y) => MathF.Pow(x, y);

    public static float Sqrt(float f) => MathF.Sqrt(f);

    public static float Sin(float theta) => MathF.Sin(theta);

    public static float Cos(float theta) => MathF.Cos(theta);

    public static float Tan(float theta) => MathF.Tan(theta);

    public static float Asin(float theta) => MathF.Asin(theta);

    public static float Acos(float theta) => MathF.Acos(theta);

    public static float Atan(float theta) => MathF.Atan(theta);

    public static float Atan2(float y, float x) => MathF.Atan2(y, x);

    public static float PI => MathF.PI;

    public static float Floor(float f) => MathF.Floor(f);

    public static float Ceil(float f) => MathF.Ceiling(f);

    public static int Sgn(float f) => f > 0 ? 1 : f < 0 ? -1 : 0;

    /// <summary>
    /// Limits the value of a float to a set range. Unlike Math.Clamp, does not
    /// throw when min > max (min wins).
    /// </summary>
    public static float Clamp(float x, float min, float max)
    {
        if (x < min) return min;
        if (x > max) return max;
        return x;
    }

    /// <summary>
    /// If x is positive or negative infinity, returns float.MaxValue or float.MinValue, respectively. Otherwise returns x.
    /// </summary>
    public static float MakeFinite(float x)
    {
        if (float.IsPositiveInfinity(x)) return float.MaxValue;
        if (float.IsNegativeInfinity(x)) return float.MinValue;
        return x;
    }

    /// <summary>
    /// Returns the distance between two points
    /// </summary>
    public static float Distance(Vector2 a, Vector2 b) => Vector2.Distance(a, b);

    public static float Angle(Vector2 a, Vector2 b) => MathF.Atan2(a.Y - b.Y, a.X - b.X);

    public static float Round(float val) => MathF.Round(val);

    public static float Round(float val, int places) => MathF.Round(val, places);

    /// <summary>
    /// Rotates a point about another point
    /// </summary>
    public static void RotatePoint(ref Vector2 point, Vector2 center, float radians)
    {
        float distance = Distance(point, center);
        float angle = MathF.Atan2(point.Y - center.Y, point.X - center.X) + radians;
        point.X = center.X + MathF.Cos(angle) * distance;
        point.Y = center.Y + MathF.Sin(angle) * distance;
    }

    public static float CrossProduct(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public static float VectorAngle(Vector2 a, Vector2 b) =>
        MathF.Acos(Vector2.Dot(a, b) / a.Length() / b.Length());
}
