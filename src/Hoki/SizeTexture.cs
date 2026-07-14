using Microsoft.Xna.Framework.Graphics;

namespace Hoki;

/// <summary>
/// Holds a Direct3D Texture and its dimensions
/// </summary>
public class SizeTexture
{
    private Texture2D texture;
    private int width, height;

    public Texture2D Tex
    {
        get { return texture; }
    }

    public int Width
    {
        get { return width; }
    }

    public int Height
    {
        get { return height; }
    }

    /// <param name="texture">A Direct3D Texture</param>
    /// <param name="width">The width of the image</param>
    /// <param name="height">The height of the image</param>
    public SizeTexture(Texture2D texture, int width, int height)
    {
        this.texture = texture;
        this.width = width;
        this.height = height;
    }
}
