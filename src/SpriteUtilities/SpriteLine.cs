using FloatMath;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities;

/// <summary>
/// A line segment transformed and drawn as a SpriteObject.
/// D3DX Line is gone; the line is drawn as a screen-space quad with the requested thickness.
/// </summary>
public class SpriteLine : TransformedObject
{
    //Properties
    protected Vector2
        vector;     //Vector that represents the line
    protected float
        angle,      //Angle from 0rad
        magnitude,  //Length
        thickness = 1;//Line width in pixels
    protected bool antialias;

    private PositionColoredTextured[] quad = new PositionColoredTextured[4];

    #region getset

    /// <summary>
    /// Vector representing the line
    /// </summary>
    public Vector2 Vector
    {
        get { return vector; }
        set
        {
            vector = value;
            updateAngle();
        }
    }

    /// <summary>
    /// X-component of the line vector
    /// </summary>
    public float Width
    {
        get { return vector.X; }
        set
        {
            vector.X = value;
            updateAngle();
        }
    }

    /// <summary>
    /// Y-component of the line vector
    /// </summary>
    public float Height
    {
        get { return vector.Y; }
        set
        {
            vector.Y = value;
            updateAngle();
        }
    }

    public float Angle
    {
        get { return angle; }
        set
        {
            angle = value;
            updateVector();
        }
    }

    public float Length
    {
        get { return magnitude; }
        set
        {
            magnitude = value;
            updateVector();
        }
    }

    /// <summary>
    /// Line midpoint
    /// </summary>
    public Vector2 Midpoint
    {
        get { return new Vector2(vector.X / 2, vector.Y / 2); }
    }

    /// <summary>
    /// Line's thickness (in pixels)
    /// </summary>
    virtual public float Thickness
    {
        get { return thickness; }
        set { thickness = value; }
    }

    /// <summary>
    /// Whether the line is antialiased (no-op; multisampling handles this now)
    /// </summary>
    virtual public bool Antialias
    {
        get { return antialias; }
        set { antialias = value; }
    }

    #endregion

    public SpriteLine(GraphicsDevice device) : base(device)
    {
        vector = new Vector2();

        Tint = Color.Black; //Black by default
    }

    /// <summary>
    /// Calculate angle and magnitude from vector
    /// </summary>
    protected void updateAngle()
    {
        angle = FMath.Atan2(vector.Y, vector.X);
        magnitude = FMath.Sqrt(FMath.Pow(Vector.X, 2) + FMath.Pow(Vector.Y, 2));
    }

    /// <summary>
    /// Calculate vector from angle and magnitude
    /// </summary>
    protected void updateVector()
    {
        vector.X = FMath.Cos(angle) * magnitude;
        vector.Y = FMath.Sin(angle) * magnitude;
    }

    /// <summary>
    /// Calculate the line in screen coordinates and draw it
    /// </summary>
    /// <param name="trans">Absolute transformation matrix</param>
    protected override void deviceDraw(Matrix trans)
    {
        //Transform endpoints to screen space (matches the old D3DX Line behavior: thickness is in screen pixels)
        Vector2 p1 = Vector2.Transform(Vector2.Zero, trans);
        Vector2 p2 = Vector2.Transform(new Vector2(vector.X, vector.Y), trans);

        Vector2 dir = p2 - p1;
        if (dir.LengthSquared() < 1e-12f) return;
        dir.Normalize();
        Vector2 normal = new Vector2(-dir.Y, dir.X) * (thickness / 2);

        Color c = ColorX.FromArgb((int)alpha, Tint);
        quad[0] = new PositionColoredTextured(p1.X + normal.X, p1.Y + normal.Y, 0, c, 0, 0);
        quad[1] = new PositionColoredTextured(p1.X - normal.X, p1.Y - normal.Y, 0, c, 0, 0);
        quad[2] = new PositionColoredTextured(p2.X + normal.X, p2.Y + normal.Y, 0, c, 0, 0);
        quad[3] = new PositionColoredTextured(p2.X - normal.X, p2.Y - normal.Y, 0, c, 0, 0);

        Renderer.DrawStrip(Matrix.Identity, null, quad);
    }
}
