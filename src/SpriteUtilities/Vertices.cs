using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

//Modern replacements for the D3D9 FVF vertex structs. Field names kept so call sites port cleanly.

public struct PositionColoredTextured : IVertexType
{
    public float X;
    public float Y;
    public float Z;
    public Color Color;
    public float Tu;
    public float Tv;

    public static readonly VertexDeclaration Declaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    VertexDeclaration IVertexType.VertexDeclaration { get { return Declaration; } }

    public Vector3 Position
    {
        get { return new Vector3(X, Y, Z); }
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public PositionColoredTextured(Vector3 value, Color c, float u, float v)
    {
        X = value.X; Y = value.Y; Z = value.Z;
        Color = c; Tu = u; Tv = v;
    }

    public PositionColoredTextured(float xvalue, float yvalue, float zvalue, Color c, float u, float v)
    {
        X = xvalue; Y = yvalue; Z = zvalue;
        Color = c; Tu = u; Tv = v;
    }
}

public struct PositionColored : IVertexType
{
    public float X;
    public float Y;
    public float Z;
    public Color Color;

    public static readonly VertexDeclaration Declaration = new VertexDeclaration(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    VertexDeclaration IVertexType.VertexDeclaration { get { return Declaration; } }

    public Vector3 Position
    {
        get { return new Vector3(X, Y, Z); }
        set { X = value.X; Y = value.Y; Z = value.Z; }
    }

    public PositionColored(Vector3 value, Color c)
    {
        X = value.X; Y = value.Y; Z = value.Z; Color = c;
    }

    public PositionColored(float xvalue, float yvalue, float zvalue, Color c)
    {
        X = xvalue; Y = yvalue; Z = zvalue; Color = c;
    }
}
