using System;

namespace Hoki;

/// <summary>
/// Better random class. Uses a single Random for the duration of the program.
/// </summary>
public class Rand
{
    private static Random r;

    static Rand()
    {
        r = new Random();
    }

    /// <summary>
    /// Returns a non-negative random number
    /// </summary>
    public static int Next()
    {
        return r.Next();
    }

    /// <param name="maxValue">The upper bound of the random number being generated. Must be >=0</param>
    public static int Next(int maxValue)
    {
        return r.Next(maxValue);
    }

    /// <param name="minValue">The lower bound of the random number being generated.</param>
    /// <param name="maxValue">The upper bound of the random number being generated. Must be >=minValue</param>
    public static int Next(int minValue, int maxValue)
    {
        return r.Next(minValue, maxValue);
    }

    /// <summary>
    /// Returns a random double between 0.0 and 1.0
    /// </summary>
    public static double NextDouble()
    {
        return r.NextDouble();
    }

    /// <summary>
    /// Returns a random float between 0.0 and 1.0
    /// </summary>
    public static float NextFloat()
    {
        return (float)r.NextDouble();
    }

    /// <param name="maxValue">The upper bound of the random number being generated. If negative, a negative number will be returned.</param>
    public static float NextFloat(float maxValue)
    {
        return (float)r.NextDouble() * maxValue;
    }

    /// <param name="minValue">The lower bound of the random number being generated.</param>
    /// <param name="maxValue">The upper bound of the random number being generated. Must be >=minValue.</param>
    public static float NextFloat(float minValue, float maxValue)
    {
        if (minValue > maxValue) throw new ArgumentOutOfRangeException("maxValue", "maxValue must be greater than or equal to minValue.");
        return minValue + (float)r.NextDouble() * (maxValue - minValue);
    }
}
