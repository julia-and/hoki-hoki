using FloatMath;
using Microsoft.Xna.Framework.Media;

namespace Hoki;

public abstract class Song
{
    protected static float volume;  //Volume from 0 to 100

    public static float Volume
    {
        get { return volume; }
        set
        {
            volume = value;
            MediaPlayer.Volume = FMath.Clamp(value, 0, 100) / 100f;
        }
    }

    public abstract void Play();
    public abstract void Stop();
}
