using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities;

/// <summary>
/// Triangle fan that can be transformed as an EffectObject TODO: Implement tint changes
/// </summary>
public class SpriteTriFan : EffectObject
{
    private PositionColoredTextured[] verts;

    private SpriteTexture tex;

    public SpriteTriFan(GraphicsDevice device, SpriteTexture tex, Vector2[] vertices, Vector2[] texCoords) : base(device)
    {
        //Store a texture reference
        this.tex = tex;

        //Create a vertex list
        verts = new PositionColoredTextured[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            verts[i] = new PositionColoredTextured(vertices[i].X, vertices[i].Y, 1.0f, Color.White, texCoords[i].X, texCoords[i].Y);
    }


    protected override void deviceDraw(Matrix trans)
    {
        base.deviceDraw(trans);

        if (current != null)
        { //Method 1: use an effect (if the base class set one)
          //Pass constant values to the effect
            foreach (FXConstant fxc in allConstants) sendFXC(fxc, trans);

            //Draw with the effect (fan converted to an indexed list by the renderer)
            foreach (EffectPass pass in current.CurrentTechnique.Passes)
            {
                pass.Apply();
                Renderer.DrawFan(trans, tex.Tex, verts);
            }
        }
        else
            //Method 2: the shared BasicEffect stands in for the fixed function pipeline
            Renderer.DrawFan(trans, tex.Tex, verts);
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
