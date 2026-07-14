using System;

namespace Hoki;

/// <summary>
/// Summary description for Score.
/// </summary>
public class Score
{
    private string name, timeString;
    private int time;
    private bool perfect, easy;

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public string TimeString
    {
        get { return timeString; }
    }

    public int Time
    {
        get { return time; }
        set
        {
            time = value;
            timeString = GetTimeString(time);
        }
    }

    public bool Perfect
    {
        get { return perfect; }
        set { perfect = value; }
    }

    public bool Easy
    {
        get { return easy; }
        set { easy = value; }
    }

    public Score(string name, int time, bool perfect, bool easy)
    {
        this.name = name;
        this.time = time;
        this.perfect = perfect;
        this.easy = easy;

        timeString = GetTimeString(time);
    }

    public static String GetTimeString(int time)
    {
        int milliseconds = (time % 1000) / 10;
        int minutes = time / 60000;
        int seconds = time / 1000 - minutes * 60;

        return minutes + ":" + (seconds < 10 ? "0" : "") + seconds + "." + (milliseconds < 10 ? "0" : "") + milliseconds;
    }
}
