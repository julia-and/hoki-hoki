using System.Collections;

namespace Hoki;

/// <summary>
/// A pair of coordinates and a list of Edges that meet at them.
/// Used for map construction.
/// </summary>
public class Node
{
    public int X, Y;                //Position
    private ArrayList edges;    //List of nodes with edges connecting to this

    public Node(int x, int y)
    {
        X = x;
        Y = y;
        edges = new ArrayList();
    }

    public void AddEdge(Edge e)
    {
        edges.Add(e);
    }

    public void OrderEdges()
    {
        edges.Sort(new EdgeComparer(this));
    }

    public int NumEdges()
    {
        return edges.Count;
    }

    public IEnumerator GetEdges()
    {
        return edges.GetEnumerator();
    }
}
