using SpriteUtilities;

namespace Hoki;

/// <summary>
/// Set of textures for a UIBox
/// </summary>
public struct UIBoxTex
{
    public SpriteTexture
        Tlc,
        Trc,
        Blc,
        Brc,
        Left,
        Right,
        Top,
        Bottom,
        Middle;

    public static UIBoxTex Empty;

    static UIBoxTex()
    {
        Empty = new UIBoxTex();
    }

    public UIBoxTex(Game game, SizeTexture tex, string texDirectory)
    {
        Tlc = game.loadTexture(tex, texDirectory + ".tlc.dat");
        Trc = game.loadTexture(tex, texDirectory + ".trc.dat");
        Blc = game.loadTexture(tex, texDirectory + ".blc.dat");
        Brc = game.loadTexture(tex, texDirectory + ".brc.dat");
        Top = game.loadTexture(tex, texDirectory + ".top.dat");
        Left = game.loadTexture(tex, texDirectory + ".left.dat");
        Right = game.loadTexture(tex, texDirectory + ".right.dat");
        Bottom = game.loadTexture(tex, texDirectory + ".bottom.dat");
        Middle = game.loadTexture(tex, texDirectory + ".middle.dat");
    }
}
