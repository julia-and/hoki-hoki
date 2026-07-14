using Microsoft.Xna.Framework;

namespace FloatMath;

/// <summary>
/// THIS CLASS IS MASSIVELY INCOMPLETE.
/// </summary>
public class Triangle
{
    private Segment[] segs;
    private Vector2[] points;

    public Vector2[] Points
    {
        get { return points; }
    }

    public Segment[] Edges
    {
        get { return segs; }
    }

    public Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        points = new Vector2[] { a, b, c };
        segs = new Segment[] {
            new Segment(a,b,null),
            new Segment(b,c,null),
            new Segment(c,a,null)
        };
    }

    public bool PtInTri(Vector2 pt)
    {
        //Any points are equal/slopes are zero or infinite => zero volume, return false
        for (int i = 0; i < 3; i++)
        {
            if (points[i].Equals(points[(i + 1) % 3])) return false;
        }

        //Foreach point in the triangle, if it on the same side of the segment that the other two points form
        //as the point we're testing, then the point is in the triangle. Otherwise, it's not.
        for (int i = 0; i < points.Length; i++) if (!SameSide(pt, points[i], points[(i + 1) % 3], points[(i + 2) % 3])) return false;

        //Passed all three tests, so it's in the triangle.
        return true;
    }

    private bool SameSide(Vector2 p1, Vector2 p2, Vector2 a, Vector2 b)
    {
        Vector2 diff = b - a;
        return (FMath.CrossProduct(diff, p1 - a) * FMath.CrossProduct(diff, p2 - a)) >= 0;
    }
}
