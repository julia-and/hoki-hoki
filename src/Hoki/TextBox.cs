using FontStashSharp;
using Microsoft.Xna.Framework;
using SpriteUtilities;
using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;

/// <summary>
/// Summary description for TextBox.
/// </summary>
public class TextBox : KeyButton
{
    private int maxLength, maxWidth;
    private bool selected, defaultCleared;
    private Menu parentMenu;
    private Game form;

    public bool DefaultCleared
    {
        get { return defaultCleared; }
        set { defaultCleared = value; }
    }

    public TextBox(Device device, SpriteTexture leftTex, SpriteTexture middleTex, SpriteTexture rightTex, SpriteFontBase font, float width, int maxLength, Game form, Menu parent) : base(device, leftTex, middleTex, rightTex, font, width)
    {
        this.form = form;
        form.KeyPress += new KeyPressEventHandler(onKeyPress);
        this.maxLength = maxLength;
        parentMenu = parent;
    }

    public void Unhook()
    {
        form.KeyPress -= new KeyPressEventHandler(onKeyPress);
    }

    /// <summary>
    /// Imposes a limit on the size of the text
    /// </summary>
    public int MaxWidth
    {
        get { return maxWidth; }
        set { maxWidth = value; }
    }

    public override void Input(Controls control)
    {
        ; //(nop) ignore inputs
    }

    public override void Select()
    {
        base.Select();
        selected = true;
    }

    public override void Deselect()
    {
        base.Deselect();
        selected = false;
    }

    private void onKeyPress(object sender, KeyPressEventArgs e)
    {
        if (!selected || parentMenu.Locked) return;

        if (!defaultCleared)
        {
            text.Text = ""; //Clear the default text;
            defaultCleared = true;
        }

        if (text.Text.Length < maxLength && (char.IsLetterOrDigit(e.KeyChar) || char.IsPunctuation(e.KeyChar) || e.KeyChar == ' '))
        {
            text.Text += e.KeyChar;                                 //Add a char

            //Test the width restriction
            if (maxWidth != 0)
            {
                Rectangle area = text.Area();
                area = text.Area(); //Don't understand why I have to do this twice, but otherwise it lags behind
                if (area.Width > maxWidth)  //If exceeded, cut off the new character
                    text.Text = text.Text.Substring(0, text.Text.Length - 1);
            }
        }

        if (char.IsControl(e.KeyChar) && text.Text.Length > 0)
            text.Text = text.Text.Substring(0, text.Text.Length - 1);   //Back one
    }
}
