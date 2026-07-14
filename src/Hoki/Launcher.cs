using FloatMath;
using Microsoft.Xna.Framework;
using SpriteUtilities;

using Device = Microsoft.Xna.Framework.Graphics.GraphicsDevice;

namespace Hoki;
/// <summary>
/// Launches mines at a Catcher
/// </summary>
public class Launcher : SpriteObject, Updateable
{
    private const float zeroFrequency = 5;  //Frequency (secs) at 0%

    private int
        turns;
    private float
        frequency,  //Frequency of launches
        timeLeft;   //Time until the next launch
    private Vector2
        direction;  //Direction of launches
    private SpriteTexture
        mineTex;    //Texture to apply to mines
    private Map
        map;        //Map the launcher is in
    private Catcher
        catcher;    //Catches the mines this launches
    private Segment
        seg;        //Represents the front face, acts as a wall

    public int Turns
    {
        get { return turns; }
        set
        {
            turns = value;
            Rotation = turns * FMath.PI / 4;
        }
    }

    public Catcher Catcher
    {
        get { return catcher; }
        set
        {
            this.catcher = value;
            float angle = FMath.Angle(catcher.Position, position);
            direction = new Vector2(FMath.Cos(angle), FMath.Sin(angle));
        }
    }

    public Segment Segment
    {
        get { return seg; }
    }

    /// <param name="mineTex">Texture for the mines shot out</param>
    /// <param name="frequency">Decimal % of allowed range of time between shots (0=rarely,1=constantly)</param>
    /// <param name="offset">Timing shift, in % of frequency's time</param>
    /// <param name="map">The Map this belongs to</param>
    public Launcher(Device device, SpriteTexture tex, SpriteTexture mineTex, float frequency, float offset, Map map) : base(device, tex)
    {
        //Store direction, map ref, mine tex, catcher
        this.mineTex = mineTex;
        this.map = map;

        //Calculate frequency in seconds from frequency in %
        this.frequency = zeroFrequency - frequency / 100 * (zeroFrequency - mineTex.Height / Mine.speed);
        timeLeft = offset / 100 * this.frequency;   //Get the offset in seconds

        //Center origin in texture
        origin.X = tex.Width / 2;
        origin.Y = tex.Height / 2;
    }

    /// <summary>
    /// Updates the Segment returned by Seg() to reflect the current transformations
    /// </summary>
    public void UpdateSeg()
    {
        //Get half dimensions
        float halfWidth = tex.Width / 2;
        float halfHeight = tex.Height / 2;

        //Get corners in local space
        Vector2 p1 = new Vector2(halfWidth, halfHeight);
        Vector2 p2 = new Vector2(halfWidth, -halfHeight);

        //Convert them to the parent (map's/zero'd layer's) coordinate space
        LocalToParent(ref p1);
        LocalToParent(ref p2);

        //Put them in the seg
        seg = new Segment(p1, p2, null);
    }

    #region Updateable Members
    public void Update(float elapsedTime)
    {
        timeLeft -= elapsedTime;
        if (timeLeft < 0)
        {   //Launch
            //Set the timer for the next launch
            timeLeft += frequency;

            //Construct a mine
            Mine m = new Mine(device, mineTex, direction, catcher);
            m.Position = position;
            m.Catcher = catcher;

            map.AddMine(m);
        }
    }
    #endregion
}
