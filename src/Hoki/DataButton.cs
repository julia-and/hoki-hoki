using FontStashSharp;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// Summary description for DataButton.
/// </summary>
public class DataButton : KeyButton
{
    private object data;

    public DataButton(Device device, SpriteTexture leftTex, SpriteTexture middleTex, SpriteTexture rightTex, SpriteFontBase font, float width) : base(device, leftTex, middleTex, rightTex, font, width)
    {
        ; //(nop)
    }

    public object Data
    {
        get { return data; }
        set { data = value; }
    }
}
