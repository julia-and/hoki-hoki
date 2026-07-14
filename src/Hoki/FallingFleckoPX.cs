using System;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// A single falling pixel in the FLECKO.NET intro effect
/// </summary>
public class FallingFleckoPX : FleckoPX
{
    private static float fadeIn, fadeOut;   //Height at which to being fading in and out

    private const int fallSpeed = 100, fadeSpeed = 255;

    private int[] pxList;
    private int pxLast, pxSpace;
    private float delay;

    public static event FleckoPXHandler OnFleckoPXCreate;

    public FallingFleckoPX(Device device, SpriteTexture tex, float pxDist, int[] pxList, float delay) : base(device, tex, pxDist)
    {
        this.pxList = pxList;
        this.delay = delay;

        //Variables used to decide which pixels to create in a given frame
        pxLast = 0;
        pxSpace = (int)(tex.Height * pxDist);

        //Start completely transparent
        Alpha = 0;
    }

    public static void Initialize(int fadeIn, int fadeOut)
    {
        FallingFleckoPX.fadeIn = fadeIn;
        FallingFleckoPX.fadeOut = fadeOut;
    }

    public override void Update(float ElapsedTime)
    {
        delay -= ElapsedTime;
        if (delay <= 0)
        {
            //Fall and fade
            this.Y += fallSpeed * ElapsedTime;
            if (Y > fadeIn && Y < fadeOut && Alpha < 255) Alpha = (int)Math.Min(Alpha + fadeSpeed * ElapsedTime, 255.0f);
            else if (Y > fadeOut && Alpha > 0) Alpha = (int)Math.Max(Alpha - fadeSpeed * ElapsedTime, 0.0f);

            //Make new pixels as necessary
            if (pxLast < pxList.Length && (pxLast + 1) * pxSpace < Y)
            {
                int highestPX = Math.Min(((int)Y - (int)Y % pxSpace) / pxSpace, pxList.Length);
                for (int i = pxLast; i < highestPX; i++)
                {
                    if (pxList[i] == 1)
                    {
                        //Create a new FleckoPX
                        FleckoPX px = new FleckoPX(device, tex, pxDist);

                        //Position it
                        px.X = X;
                        px.Y = i * pxSpace;

                        //Pass it to an event call
                        OnFleckoPXCreate(this, new FleckoPXEventArgs(px));
                    }
                }
                pxLast = highestPX;
            }

            //Perform normal updates (position and color)
            base.Update(ElapsedTime);
        }
    }
}

#region event classes
/// <summary>
/// Delegate fo events that require a FleckoPX to be passed
/// </summary>
public delegate void FleckoPXHandler(object sender, FleckoPXEventArgs e);

/// <summary>
/// EventArgs that holds a FleckoPX
/// </summary>
public class FleckoPXEventArgs : EventArgs
{
    public FleckoPX created;

    /// <param name="created">The FleckoPX that was just created</param>
    public FleckoPXEventArgs(FleckoPX created) : base()
    {
        this.created = created;
    }
}
#endregion
