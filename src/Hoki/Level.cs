using System;
using System.Collections;

namespace Hoki;

/// <summary>
/// Summary description for Level.
/// </summary>
public class Level
{
    private string hash, map, name; //Hash code for the level this data relates to,full map string,name
    private Score playerBest = null;    //Best time of the player currently active
    private Score[] topScores;      //Best times of any players
    private ArrayList ghosts;       //List of ghost files on this level
    private const int numScores = 3;    //Number of top scores to store

    public string GameMap
    {
        get { return map; }
        set { map = value; }
    }

    public string Hash
    {
        get { return hash; }
    }

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public Score[] TopScores
    {
        get { return topScores; }
    }

    public ArrayList Ghosts
    {
        get { return ghosts; }
    }

    public Score PlayerBest
    {
        get { return playerBest; }
    }

    public Level(string hash)
    {
        //Store the level's hash code
        this.hash = hash;

        //Create the top score list
        topScores = new Score[numScores];

        //Create the ghost list
        ghosts = new ArrayList();

        //Populate it with empty scores
        for (int i = 0; i < topScores.Length; i++)
            topScores[i] = new Score("", int.MaxValue, false, false);   //Empty string name indicates that the score is just a placeholder
    }

    /// <summary>
    /// If the argument is a better score than the previous player's best, the method sets it as the best and returns true
    /// </summary>
    /// <param name="score"></param>
    public bool InsertPlayerBest(Score score)
    {
        if (playerBest.Time > score.Time)
        {
            playerBest = score;
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the argument is better than some score on the list, the method will insert it in the list and return true
    /// </summary>
    /// <param name="score"></param>
    /// <returns></returns>
    public bool InsertScore(Score score)
    {
        for (int i = 0; i < 3; i++)
        {
            if (topScores[i].Time > score.Time)
            {
                placeScore(score, i);
                return true;
            }
        }
        return false;
    }

    public void InsertGhost(String filename)
    {
        if (!ghosts.Contains(filename)) ghosts.Add(filename);
    }

    public void RemoveGhost(String filename)
    {
        ghosts.Remove(filename);
    }

    /// <summary>
    /// Insert a score into the high score list, shift everything beneath it down
    /// </summary>
    /// <param name="score">New score</param>
    /// <param name="index">Index to place it at</param>
    private void placeScore(Score score, int index)
    {
        int current = numScores - 1;
        while (current > index)
        {
            topScores[current] = topScores[current - 1];
            current--;
        }
        topScores[index] = score;
    }
}
