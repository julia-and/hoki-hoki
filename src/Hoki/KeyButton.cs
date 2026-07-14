using System;
using FontStashSharp;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// A button that is operated with keys
/// </summary>
public class KeyButton : MenuElement
{
    private SpriteObject left, middle, right;
    private float buttonWidth;
    protected SpriteText text;

    public event EventHandler Press;
    public event EventHandler Over;
    public event EventHandler Out;

    public override float Height
    {
        get { return middle.Height; }
        set { XScale = value / middle.Height; }
    }

    public override float Width
    {
        get
        {
            return buttonWidth;
        }
    }

    public string Text
    {
        get { return text.Text; }
        set { text.Text = value; }
    }

    public KeyButton(Device device, SpriteTexture leftTex, SpriteTexture middleTex, SpriteTexture rightTex, SpriteFontBase font, float width) : base(device, null)
    {
        buttonWidth = width;

        left = new SpriteObject(device, leftTex);
        Add(left);

        middle = new SpriteObject(device, middleTex);
        middle.X = leftTex.Width;
        middle.Width = width - rightTex.Width - leftTex.Width;
        Add(middle);

        right = new SpriteObject(device, rightTex);
        right.X = middle.X + middle.Width;
        Add(right);

        text = new SpriteText(device, font, (int)middle.Width - 2, (int)middle.Height);
        text.X = middle.X + 2;
        text.Format = FontDrawFlags.VerticalCenter;
        Add(text);

        //Hook events
        Press += new EventHandler(onPress); //Own press (prevent NRE)
    }

    private void onPress(object sender, EventArgs e)
    {
        ; // (nop)
    }

    #region MenuElement Members

    public override void Input(Controls control)
    {
        if (control == Controls.A) Press(this, new EventArgs());
    }

    public override void Select()
    {
        left.Frame = middle.Frame = right.Frame = 1;
        if (Over != null) Over(this, new EventArgs());
    }

    public override void Deselect()
    {
        left.Frame = middle.Frame = right.Frame = 0;
        if (Out != null) Out(this, new EventArgs());
    }

    #endregion
}
