using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities;

/// <summary>
/// Polygonal surface over which a SpriteTexture is tiled TODO: Implement tint changes
/// </summary>
public class SpritePolygon : EffectObject
{
    private PositionColoredTextured[] verts;
    private short[] indices;

    private Vector2 size;
    private SpriteTexture tex;
    private bool useIB;

    public SpritePolygon(GraphicsDevice device, SpriteTexture tex) : base(device)
    {
        //Store tex reference
        this.tex = tex;
    }

    public SpritePolygon(GraphicsDevice device, SpriteTexture tex, Vector2[] vertices) : base(device)
    {
        //Store tex reference
        this.tex = tex;

        //Prepare the data
        Build(vertices);
    }

    public SpritePolygon(GraphicsDevice device, SpriteTexture tex, Vector2[] vertices, short[] indices) : base(device)
    {
        //Store tex reference
        this.tex = tex;

        //Prepare the data
        Build(vertices, indices);
    }

    public float Width
    {
        get { return size.X * XScale; }
        set { XScale = value / size.X; }
    }

    public float Height
    {
        get { return size.Y * YScale; }
        set { YScale = value / size.Y; }
    }

    public override Color Tint
    {
        get { return base.Tint; }
        set
        {
            base.Tint = value;
            for (int i = 0; i < verts.Length; i++) verts[i].Color = value;
        }
    }

    public void Build(Vector2[] vertices)
    {
        Vector2 //Store minimum and maximum positions to calculate size
            min = new Vector2(float.PositiveInfinity, float.PositiveInfinity),
            max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        //Create vertices
        verts = new PositionColoredTextured[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            verts[i] = new PositionColoredTextured(vertices[i].X,
                vertices[i].Y,
                1.0f,
                Color.White,
                vertices[i].X / tex.Width,
                vertices[i].Y / tex.Height);
            min.X = Math.Min(vertices[i].X, min.X);
            min.Y = Math.Min(vertices[i].Y, min.Y);
            max.X = Math.Max(vertices[i].X, max.X);
            max.Y = Math.Max(vertices[i].Y, max.Y);
        }

        size = max - min;

        //Don't use an index buffer
        useIB = false;
    }

    public void Build(Vector2[] vertices, short[] indices)
    {
        //Build the vertex list
        Build(vertices);

        //Store the index list
        this.indices = indices;
        useIB = true;
    }

    public void Build(PositionColoredTextured[] verts)
    {
        this.verts = verts;
        useIB = false;
    }

    public void Build(PositionColoredTextured[] verts, short[] indices)
    {
        this.verts = verts;
        this.indices = indices;
        useIB = true;
    }

    protected override void deviceDraw(Matrix trans)
    {
        base.deviceDraw(trans);
        if (verts == null) return;

        if (current != null)
        { //Method 1: use an effect (if the base class set one)
          //Pass constant values to the effect
            foreach (FXConstant fxc in allConstants) sendFXC(fxc, trans);

            foreach (EffectPass pass in current.CurrentTechnique.Passes)
            {
                pass.Apply();
                drawPrimitives(trans);
            }
        }
        else
            //Method 2: the shared BasicEffect stands in for the fixed function pipeline
            drawPrimitives(trans);
    }

    private void drawPrimitives(Matrix trans)
    {
        if (useIB) Renderer.DrawIndexed(trans, tex.Tex, verts, indices);
        else Renderer.DrawList(trans, tex.Tex, verts);
    }

    protected override void sendFXC(SpriteUtilities.EffectObject.FXConstant fxc, Matrix trans)
    {
        if (current != null)
        {
            switch (fxc.Type)
            {
                case ConstType.Color:
                    current.Parameters[fxc.Name].SetValue(new Vector4(color.R, color.G, color.B, color.A));
                    break;
                case ConstType.Texture:
                    if (tex != null) current.Parameters[fxc.Name].SetValue(tex.Tex); //Can't set with a null tex, but it doesn't matter because the effect routine won't be run
                    break;
                case ConstType.WorldMatrix:
                    current.Parameters[fxc.Name].SetValue(trans);
                    break;
            }
        }
    }

}
