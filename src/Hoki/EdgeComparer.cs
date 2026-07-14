using System;
using System.Collections;

namespace Hoki;

/// <summary>
/// Summary description for EdgeComparer.
/// </summary>
public class EdgeComparer : IComparer
{
    //Node to compare on
    private Node center;

    public EdgeComparer(Node n)
    {
        this.center = n;
    }

    public int Compare(Object a, Object b)
    {
        return Compare((Edge)a, (Edge)b);
    }

    public int Compare(Edge a, Edge b)
    {
        float aAngle = a.angleFrom(center);
        float bAngle = b.angleFrom(center);

        if (aAngle > bAngle) return -1;
        else if (aAngle < bAngle) return 1;
        else return 0;
    }
}
