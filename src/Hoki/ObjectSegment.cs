using System;
using FloatMath;

namespace Hoki;

/// <summary>
/// Contains a Segment and a reference to the object it came from
/// </summary>
public class ObjectSegment
{
    private Segment seg;
    private object obj;

    public object Object
    {
        get { return obj; }
    }

    public Segment Segment
    {
        get { return seg; }
    }

    public ObjectSegment(Segment seg, Object obj)
    {
        this.seg = seg;
        this.obj = obj;
    }
}
