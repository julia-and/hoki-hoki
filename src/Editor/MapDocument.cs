using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HokiEdit;

// The .map data model. Coordinates are stored in file units exactly as written:
// nodes/pads/springs/launchers in 8px map units, sprites in 4px units, all ints.
// The game's reader (src/Hoki/Map.cs FromString) is the authoritative spec; the
// original editor is NOT — it disagreed with the game on pad/launcher offsets.

public class Node { public int X, Y; }
public class Wall { public int A, B; }          //Node indices
public class Tri { public int A, B, C; }
public class Pad { public int X, Y; public int Type; }  //Type: 0=Start 1=End 2=Heal
public class Spring { public int X, Y; public int Turns; }
public class LauncherPair
{
    public int SX, SY, STurns;      //Sender
    public int FreqPct, OffPct;     //Timing, integer percent
    public int CX, CY, CTurns;      //Catcher
}
public class Decal { public int X, Y; public int Index; public float Depth; }   //4px units

public class MapDocument
{
    public List<Node> Nodes = new();
    public List<Wall> Walls = new();
    public List<Tri> Tris = new();
    public List<Pad> Pads = new();
    public List<Spring> Springs = new();
    public List<LauncherPair> Launchers = new();
    public List<Decal> Decals = new();
    public string Author = "Anonymous";
    public string Theme = "default.theme";
    public string Ghost = "";

    #region parse
    /// <summary>
    /// Parses map text. Never throws on bad input: unreadable lines are skipped and
    /// reported in errors. (The original editor crashed on any malformed line.)
    /// </summary>
    public static MapDocument Parse(string text, out List<string> errors)
    {
        MapDocument d = new MapDocument();
        errors = new List<string>();
        string section = "";
        var catchers = new List<(int x, int y, int t)>();
        int lineNo = 0;

        foreach (string raw in text.Split('\n', '\r'))
        {
            lineNo++;
            string line = raw.Trim();
            if (line.Length == 0) continue;

            if (line[0] == '>') { section = line; continue; }

            if (line[0] == '#')
            {
                int sp = line.IndexOf(' ');
                if (sp < 0) continue;   //Counts we recompute; tags without values are ignorable
                string tag = line.Substring(0, sp), val = line.Substring(sp + 1).Trim();
                switch (tag)
                {
                    case "#AUTHOR": d.Author = val; break;
                    case "#THEME": d.Theme = val; break;
                    case "#GHOST": if (val != "<None>") d.Ghost = val; break;
                        //#NODECOUNT/#WALLCOUNT/#POLYCOUNT recomputed on save; not trusted on load
                }
                continue;
            }

            string[] p = line.Split(',');
            bool ok = true;
            int f(int i) { int v = 0; if (i >= p.Length || !int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) ok = false; return v; }
            float ff(int i) { float v = 0; if (i >= p.Length || !float.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) ok = false; return v; }

            switch (section)
            {
                case ">NODES": d.Nodes.Add(new Node { X = f(0), Y = f(1) }); break;
                case ">LINES": d.Walls.Add(new Wall { A = f(0), B = f(1) }); break;
                case ">TRIANGLES": d.Tris.Add(new Tri { A = f(0), B = f(1), C = f(2) }); break;
                case ">PADS": d.Pads.Add(new Pad { X = f(0), Y = f(1), Type = f(2) }); break;
                case ">SPRINGS": d.Springs.Add(new Spring { X = f(0), Y = f(1), Turns = f(2) }); break;
                case ">LAUNCHERS": d.Launchers.Add(new LauncherPair { SX = f(0), SY = f(1), STurns = f(2), FreqPct = f(3), OffPct = f(4) }); break;
                case ">CATCHERS": catchers.Add((f(0), f(1), f(2))); break;
                case ">SPRITES": d.Decals.Add(new Decal { X = f(0), Y = f(1), Index = f(2), Depth = ff(3) }); break;
                default: ok = false; break;
            }
            if (!ok) errors.Add($"line {lineNo}: could not read '{line}' in section '{section}'");
        }

        //Pair catchers with senders by ordinal, like the game does
        for (int i = 0; i < catchers.Count; i++)
        {
            if (i < d.Launchers.Count)
            {
                d.Launchers[i].CX = catchers[i].x;
                d.Launchers[i].CY = catchers[i].y;
                d.Launchers[i].CTurns = catchers[i].t;
            }
            else errors.Add($"catcher {i} has no matching launcher; dropped");
        }
        //A sender without a catcher would crash the game loader; give it one on top of itself
        for (int i = catchers.Count; i < d.Launchers.Count; i++)
        {
            errors.Add($"launcher {i} had no catcher; catcher placed at sender");
            d.Launchers[i].CX = d.Launchers[i].SX;
            d.Launchers[i].CY = d.Launchers[i].SY;
        }

        //Drop anything referencing a missing node (would crash the game loader)
        bool valid(int n) => n >= 0 && n < d.Nodes.Count;
        int dw = d.Walls.RemoveAll(w => !valid(w.A) || !valid(w.B));
        int dt = d.Tris.RemoveAll(t => !valid(t.A) || !valid(t.B) || !valid(t.C));
        if (dw > 0) errors.Add($"{dw} wall(s) referenced missing nodes; dropped");
        if (dt > 0) errors.Add($"{dt} triangle(s) referenced missing nodes; dropped");

        return d;
    }
    #endregion

    #region serialize
    /// <summary>
    /// Writes the format in the original editor's section order, with correct counts.
    /// </summary>
    public string Serialize()
    {
        var inv = CultureInfo.InvariantCulture;
        StringBuilder s = new StringBuilder();
        s.Append("#NODECOUNT ").Append(Nodes.Count).Append('\n');
        s.Append("#WALLCOUNT ").Append(Walls.Count).Append('\n');
        s.Append("#POLYCOUNT ").Append(Tris.Count).Append('\n');
        s.Append("#AUTHOR ").Append(Author).Append('\n');
        s.Append("#THEME ").Append(string.IsNullOrEmpty(Theme) ? "default.theme" : Theme).Append('\n');
        if (!string.IsNullOrEmpty(Ghost)) s.Append("#GHOST ").Append(Ghost).Append('\n');

        s.Append(">NODES\n");
        foreach (var n in Nodes) s.Append(n.X).Append(',').Append(n.Y).Append('\n');
        s.Append(">TRIANGLES\n");
        foreach (var t in Tris) s.Append(t.A).Append(',').Append(t.B).Append(',').Append(t.C).Append('\n');
        s.Append(">LINES\n");
        foreach (var w in Walls) s.Append(w.A).Append(',').Append(w.B).Append('\n');
        s.Append(">PADS\n");
        foreach (var p in Pads) s.Append(p.X).Append(',').Append(p.Y).Append(',').Append(p.Type).Append('\n');
        s.Append(">SPRINGS\n");
        foreach (var sp in Springs) s.Append(sp.X).Append(',').Append(sp.Y).Append(',').Append(sp.Turns).Append('\n');
        if (Launchers.Count > 0)
        {
            s.Append(">LAUNCHERS\n");
            foreach (var l in Launchers) s.Append(l.SX).Append(',').Append(l.SY).Append(',').Append(l.STurns).Append(',').Append(l.FreqPct).Append(',').Append(l.OffPct).Append('\n');
            s.Append(">CATCHERS\n");
            foreach (var l in Launchers) s.Append(l.CX).Append(',').Append(l.CY).Append(',').Append(l.CTurns).Append('\n');
        }
        s.Append(">SPRITES\n");
        foreach (var m in Decals) s.Append(m.X).Append(',').Append(m.Y).Append(',').Append(m.Index).Append(',').Append(m.Depth.ToString(inv)).Append('\n');
        return s.ToString();
    }
    #endregion
}

/// <summary>
/// Undo/redo as a stack of serialized snapshots. The map format is a small text
/// blob, so full snapshots are simpler and safer than command objects.
/// </summary>
public class UndoStack
{
    private readonly List<string> undo = new(), redo = new();
    private const int Max = 200;

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    /// <summary>Call BEFORE mutating the document.</summary>
    public void Push(MapDocument d)
    {
        undo.Add(d.Serialize());
        if (undo.Count > Max) undo.RemoveAt(0);
        redo.Clear();
    }

    public MapDocument Undo(MapDocument current)
    {
        if (!CanUndo) return current;
        redo.Add(current.Serialize());
        return pop(undo);
    }

    public MapDocument Redo(MapDocument current)
    {
        if (!CanRedo) return current;
        undo.Add(current.Serialize());
        return pop(redo);
    }

    public void Clear() { undo.Clear(); redo.Clear(); }

    private static MapDocument pop(List<string> stack)
    {
        string s = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return MapDocument.Parse(s, out _);
    }
}
