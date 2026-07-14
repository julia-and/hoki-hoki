using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// Summary description for Planet.
/// </summary>
public class Planet : TransformedObject, Updateable
{
    private PositionColoredTextured[] verts;
    private Vector2 shiftRate;
    private Texture2D tex;
    private Game owner;
    private float radius;

    private static int id;
    private int myId;

    public Planet(Device device, Texture2D tex, SpriteTexture sphereTex, Game owner, int detail, float radius, float texrep, Vector2 shiftRate) : base(device)
    {
        this.tex = tex;
        this.shiftRate = shiftRate;
        this.owner = owner;
        this.radius = radius;

        myId = id++;

        verts = new PositionColoredTextured[detail + 2];
        Microsoft.Xna.Framework.Color white = Microsoft.Xna.Framework.Color.White;

        verts[0] = new PositionColoredTextured(0, 0, 0, white, 0.5f, 0.5f);
        for (int i = 1; i <= detail + 1; i++)
        {
            double angle = ((double)i) / detail * 2 * Math.PI;
            Vector2 v = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            verts[i] = new PositionColoredTextured(radius * v.X, radius * v.Y, 0, white, (1f + v.X * texrep) / 2f, (1f + v.Y * texrep) / 2f);
        }

        SpriteObject sphere = new SpriteObject(device, sphereTex);
        sphere.Width = sphere.Height = 2 * radius;
        sphere.X = -radius;
        sphere.Y = -radius;
        Add(sphere);
    }

    #region Updateable Members
    public void Update(float elapsedTime)
    {
        //HACK: control visibility for speed. This is really bad code, but it's faster than using localToGlobal.
        Vector2 screenPos = parent.Parent.Position + position;
        Visible = (screenPos.X + radius > 0 && screenPos.X - radius < owner.ClientSize.X && screenPos.Y + radius > 0 && screenPos.Y - radius < owner.ClientSize.Y);

        //Slide the texture along if visible.
        if (Visible)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].Tu += shiftRate.X * elapsedTime;
                verts[i].Tv += shiftRate.Y * elapsedTime;
            }
        }
    }
    #endregion

    protected override void deviceDraw(Matrix trans)
    {
        Renderer.DrawFan(trans, tex, verts);
    }
}
