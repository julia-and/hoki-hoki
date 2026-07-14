using System;
using Microsoft.Xna.Framework;

namespace FloatMath;

/// <summary>
/// A pair segments such that if their corresponding endpoints
/// were connected they would form a rectangle.
/// </summary>
public class SegPair : ICloneable
{
    private Segment s1, s2;

    #region getset

    public float Angle
    {
        get { return s1.Angle; }
    }

    public float Length
    {
        get { return s1.Length; }
    }

    public Vector2 Normal
    {
        get { return s1.Normal; }
    }

    public float Slope
    {
        get { return s1.Slope; }
    }

    public Vector2 Midpoint
    {
        get
        {
            Vector2 mp1 = s1.Midpoint, mp2 = s2.Midpoint;
            return new Vector2((mp1.X + mp2.X) / 2, (mp1.Y + mp2.Y) / 2);
        }
    }

    public Segment S1
    {
        get { return s1; }
    }

    public Segment S2
    {
        get { return s2; }
    }

    #endregion

    /// <summary>
    /// Constructs the pair based on the coordinates of the 
    /// segment that would fit directly between them (ie.
    /// such that every one of its points is the midpoint of
    /// any segment connecting the two of the pair), and the
    /// distance they are from each other.
    /// </summary>
    public SegPair(Vector2 position, Vector2 size, float distance)
    {
        //Start each segment as the centered segment
        s1 = new Segment(position, size);
        s2 = new Segment(position, size);

        //Find a vector that describes how far they should be moved from center
        float perpendicular = s1.Angle + FMath.PI / 2;
        float halfDistance = distance / 2;
        Vector2 move = new Vector2(FMath.Cos(perpendicular) * halfDistance, FMath.Sin(perpendicular) * halfDistance);

        //Make the move
        s1.Translate(move);
        move = Vector2.Multiply(move, -1);
        //move.Scale(-1);
        s2.Translate(move);
    }

    private SegPair(Segment s1, Segment s2)
    {
        this.s1 = s1;
        this.s2 = s2;
    }

    public void Rotate(Vector2 center, float angle)
    {
        s1.Rotate(center, angle);
        s2.Rotate(center, angle);
    }

    public void Translate(Vector2 shift)
    {
        s1.Translate(shift);
        s2.Translate(shift);
    }

    /// <summary>
    /// Determines whether two SegPairs intersect and, if so, provides
    /// information about the intersection
    /// </summary>
    /// <param name="point">Point of intersection</param>
    /// <param name="seg1">Segment from SegPair a that intersected</param>
    /// <param name="seg2">Segment from SegPair b that intersected</param>
    public static bool Intersection(SegPair a, SegPair b, out Vector2 point, out Segment seg1, out Segment seg2)
    {
        //Assign
        point = Vector2.Zero;
        seg1 = seg2 = null;

        //Test collisions
        if (SegPair.Intersection(a, b.S1, out point, out seg1))
        {
            seg2 = b.S1;
            return true;
        }
        else if (SegPair.Intersection(a, b.S2, out point, out seg1))
        {
            seg2 = b.S2;
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Determines whether a Segment intersects with a SegPair
    /// and, if so, provides information about the intersection
    /// </summary>
    /// <param name="point">Point of intersection</param>
    /// <param name="outseg">Segment in the pair that intersected</param>
    public static bool Intersection(SegPair pair, Segment seg, out Vector2 point, out Segment outseg)
    {
        //Assign
        point = Vector2.Zero;
        outseg = null;

        //Test collisions
        if (Segment.Intersection(pair.S1, seg, out point))
        {
            seg = pair.S1;
            return true;
        }
        else if (Segment.Intersection(pair.S2, seg, out point))
        {
            seg = pair.S2;
            return true;
        }
        else
            return false;
    }

    #region ICloneable Members

    /// <summary>
    /// Clones the SegPair with references to the same members
    /// </summary>
    /// <returns></returns>
    public object Clone()
    {
        return new SegPair(s1, s2);
    }

    /// <summary>
    /// Clones the SegPair with references to cloned members
    /// </summary>
    public object DeepClone()
    {
        return new SegPair((Segment)s1.Clone(), (Segment)s2.Clone());
    }

    #endregion
}
