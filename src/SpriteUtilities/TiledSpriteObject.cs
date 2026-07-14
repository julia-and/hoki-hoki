using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteUtilities;

/// <summary>
/// A SpriteObject whose texture tiles over an arbitrary width and height.
/// </summary>
public class TiledSpriteObject : SpriteObject
{
    private Vector2 size;

    public override int Frame
    { //Prevent the vertices from getting messed up
        get { return 0; }
        set {; /*(nop)*/ }
    }

    public TiledSpriteObject(GraphicsDevice device, SpriteTexture texture, float width, float height) : base(device, texture)
    {
        size = new Vector2(width, height);

        //Set the vertices up properly
        verts[2].X = verts[3].X = width;    //Right
        verts[1].Y = verts[3].Y = height;   //Bottom

        //Adjust the texture coordinates
        verts[2].Tu = verts[3].Tu = width / texture.Width;  //Right
        verts[1].Tv = verts[3].Tv = height / texture.Height;    //Bottom
    }
}
