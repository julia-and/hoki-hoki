using Microsoft.Xna.Framework;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// A map region with special properties.
/// TODO: Make abstract, derive special types
/// </summary>
public class Pad : SpriteObject
{
    private PadType type;
    public const int Size = 192;

    public PadType Type
    {
        get { return type; }
    }

    public Pad(Device device, SpriteTexture tex, PadType type) : base(device, tex)
    {
        //Store the type locally
        this.type = type;
    }

    public bool Collides(Vector2 pos)
    {
        //Check that it's within the box
        return (pos.X > X && pos.X < X + Width && pos.Y > Y && pos.Y < Y + Height);
    }
}
