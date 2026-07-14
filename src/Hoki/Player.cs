using System;
using System.Collections;

namespace Hoki;

/// <summary>
/// A player name and data about their accomplishments and preferences
/// </summary>
public class Player
{
    private const int numLevels = 26;   //Number of levels in the regular game

    private string name;
    private Hashtable scores;
    private ArrayList hashes;
    private bool
        easy;                   //Whether the player is currently using easy mode

    private int[] trophies;     //Total number of gold, silver, bronze trophies
    private int perfects;       //Number of perfect scores
    private bool highestEasy;   //Whether the highest level was beaten on easy mode
    private int highest;        //Highest level beaten
    private int totalTime;      //Combined time of all finished levels

    public Player(string name)
    {
        this.name = name;
        hashes = new ArrayList();
        scores = new Hashtable();
        trophies = new int[3];
    }

    public string Name
    {
        get { return name; }
    }

    public bool Easy
    {
        get { return easy; }
        set { easy = value; }
    }

    public void AddScore(string levelHash, Score score)
    {
        Score old = (Score)scores[levelHash];
        if (old != null)
        {
            //Old entry exists - replace it if the new one is better, or if either is non-easy keep that, and if either has a perfect then keep it
            Score newScore;
            if (old.Easy != score.Easy)
            {
                if (old.Easy) newScore = score;
                else newScore = old;
            }
            else newScore = new Score(name, Math.Min(score.Time, old.Time), old.Perfect || score.Perfect, old.Easy);
            scores[levelHash] = newScore;
        }
        else
        {
            //New entry
            hashes.Add(levelHash);
            scores.Add(levelHash, score);
        }
    }

    public Score GetScore(string levelHash)
    {
        return (Score)scores[levelHash];
    }

    public override string ToString()
    {
        string output = "#" + name;

        //Get keys and values
        foreach (string hash in hashes)
        {
            Score score = (Score)scores[hash];
            output += "\n" + hash + ":" + score.Time + ":" + (score.Perfect ? 1 : 0) + ":" + (score.Easy ? 1 : 0);
        }
        output += "\n>EASY:" + (easy ? 1 : 0);  //Add difficulty preference

        return output;
    }

    public void RefreshStatus(Level[] gameLevels, int[,] trophyTimes)
    {
        perfects = 0;
        trophies = new int[3];
        totalTime = 0;

        int easyScores = 0, normalScores = 0;

        for (int i = 0; i < gameLevels.Length; i++)
        {
            Score s = GetScore(gameLevels[i].Hash);
            if (s == null) break;   //No more scores to get

            if (i < numLevels) totalTime += s.Time; //Don't count the bonus level in total time

            easyScores++;
            if (!s.Easy)
            {
                normalScores++;
                if (s.Perfect) perfects++;
                if (s.Time <= trophyTimes[i, 0]) trophies[0]++;
                else if (s.Time <= trophyTimes[i, 1]) trophies[1]++;
                else if (s.Time <= trophyTimes[i, 2]) trophies[2]++;
            }
        }

        highestEasy = (normalScores == 0);
        highest = Math.Min(easyScores, normalScores) - 1;
    }

    public bool CompletedEasy
    {
        get { return highest >= numLevels - 1; }
    }

    public bool CompletedNormal
    {
        get { return highest >= numLevels - 1 && !highestEasy; }
    }

    public int Highest
    {
        get { return highest; }
    }

    public bool HighestEasy
    {
        get { return highestEasy; }
    }

    public int Perfects
    {
        get { return perfects; }
    }

    public int[] Trophies
    {
        get { return trophies; }
    }

    public int TotalTime
    {
        get { return totalTime; }
    }
}
