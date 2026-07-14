using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NVec2 = System.Numerics.Vector2;

namespace HokiEdit;

/// <summary>
/// HokiEdit rewrite: MonoGame window, Dear ImGui chrome, schematic canvas.
/// The canvas draws entirely through ImGui's background draw list.
/// </summary>
public partial class EditorGame : Game
{
    private GraphicsDeviceManager graphics;
    private ImGuiRenderer imgui;

    private MapDocument doc = new MapDocument();
    private UndoStack undoStack = new UndoStack();
    private string currentPath = null;
    private List<string> parseErrors = new List<string>();
    private bool dirty;

    //View: world px (file units * 8) -> screen
    private float zoom = 1f;
    private NVec2 pan = new NVec2(64, 64);

    //File browser state
    private bool browserOpen, browserSave;
    private string browserDir;
    private string browserFileName = "untitled.map";

    private bool settingsOpen;
    private string screenshotPath = null;   //--screenshot mode
    private int frameCount;

    //Heli mockup: rotatable actual-size outline for judging spacing; pinnable to a world position
    private bool heliMockup;
    private float heliMockupAngle;
    private bool heliPinned;
    private NVec2 heliPinPos;   //World px, valid while pinned

    //Playtest trace: heli position+rotation per frame dumped by the game (one segment per attempt)
    private struct TraceSample { public NVec2 P; public float R; }
    private bool showTrace = true;
    private readonly List<List<TraceSample>> traceSegments = new();
    private DateTime traceStamp;
    private static readonly string tracePath = Path.Combine(Path.GetTempPath(), "hokiedit-playtest.trace");

    //Trace playback (pseudo-ghost)
    private int traceAttempt = -1;  //-1 = follow latest attempt
    private float traceFrame;
    private bool tracePlaying;

    private const int Unit = 8; //map file unit -> pixels

    //Platform shortcut modifier: labeled Cmd on macOS, Ctrl elsewhere; both are accepted
    private static readonly bool IsMac = OperatingSystem.IsMacOS();
    private static readonly string ModName = IsMac ? "Cmd" : "Ctrl";
    private static bool modDown(ImGuiIOPtr io) => io.KeyCtrl || io.KeySuper;

    public EditorGame(string[] args)
    {
        graphics = new GraphicsDeviceManager(this);
        graphics.PreferredBackBufferWidth = 1280;
        graphics.PreferredBackBufferHeight = 800;
        Window.Title = "HokiEdit";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        //--screenshot <map> <out.png>: load, render one frame, save, exit (self-test hook)
        if (args.Length >= 3 && args[0] == "--screenshot")
        {
            loadMapFile(args[1]);
            screenshotPath = args[2];
        }
        else if (args.Length >= 1 && File.Exists(args[0]))
        {
            loadMapFile(args[0]);
        }

        browserDir = findDefaultMapDir();
    }

    private static string findDefaultMapDir()
    {
        //Prefer the repo's map data when running from the source tree
        string probe = Path.Combine(AppContext.BaseDirectory, "../../../../Hoki/data/maps/maingame");
        if (Directory.Exists(probe)) return Path.GetFullPath(probe);
        return Directory.GetCurrentDirectory();
    }

    protected override void LoadContent()
    {
        imgui = new ImGuiRenderer(this);
    }

    #region file ops
    private void loadMapFile(string path)
    {
        try
        {
            doc = MapDocument.Parse(File.ReadAllText(path), out parseErrors);
            currentPath = path;
            undoStack.Clear();
            dirty = false;
        }
        catch (Exception e)
        {
            parseErrors = new List<string> { "could not open " + path + ": " + e.Message };
        }
    }

    private void saveMapFile(string path)
    {
        try
        {
            File.WriteAllText(path, doc.Serialize());
            currentPath = path;
            dirty = false;
        }
        catch (Exception e)
        {
            parseErrors = new List<string> { "could not save " + path + ": " + e.Message };
        }
    }
    #endregion

    #region update/draw
    protected override void Draw(GameTime gameTime)
    {
        imgui.Update(gameTime);

        handleCanvasInput();
        toolInput();
        drawCanvas();
        drawOverlays();
        drawUI();

        GraphicsDevice.Clear(new Color(245, 245, 245));
        imgui.Render();

        base.Draw(gameTime);

        //ImGui auto-resize windows need a frame to measure, so capture the second frame
        if (screenshotPath != null && ++frameCount >= 2)
        {
            saveBackbuffer(screenshotPath);
            Exit();
        }
    }

    private void saveBackbuffer(string path)
    {
        int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
        Color[] data = new Color[w * h];
        GraphicsDevice.GetBackBufferData(data);
        using var tex = new Texture2D(GraphicsDevice, w, h);
        tex.SetData(data);
        using var fs = File.Create(path);
        tex.SaveAsPng(fs, w, h);
    }
    #endregion

    #region view/canvas
    private NVec2 worldToScreen(float wx, float wy) => new NVec2(wx * zoom + pan.X, wy * zoom + pan.Y);
    private NVec2 screenToWorld(NVec2 s) => new NVec2((s.X - pan.X) / zoom, (s.Y - pan.Y) / zoom);

    private void handleCanvasInput()
    {
        var io = ImGui.GetIO();
        if (io.WantCaptureMouse) return;

        //Pan: middle-drag
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0))
        {
            var d = io.MouseDelta;
            pan += d;
        }

        //The decal tool claims shift/alt+wheel for index/depth
        if (tool == Tool.Decal && (io.KeyShift || io.KeyAlt)) return;

        if (io.MouseWheel != 0 || io.MouseWheelH != 0)
        {
            if (modDown(io))
            {
                //Mod+wheel: zoom around cursor
                float factor = MathF.Pow(1.1f, io.MouseWheel);
                float newZoom = Math.Clamp(zoom * factor, 0.1f, 8f);
                factor = newZoom / zoom;
                pan = io.MousePos - (io.MousePos - pan) * factor;
                zoom = newZoom;
            }
            else
            {
                //Plain wheel / two-finger scroll: pan
                pan += new NVec2(io.MouseWheelH, io.MouseWheel) * 30f;
            }
        }
    }

    private static uint rgba(byte r, byte g, byte b, byte a = 255) => (uint)(r | (g << 8) | (b << 16) | (a << 24));

    private void drawCanvas()
    {
        var dl = ImGui.GetBackgroundDrawList();
        var io = ImGui.GetIO();
        float w = io.DisplaySize.X, h = io.DisplaySize.Y;

        dl.AddRectFilled(new NVec2(0, 0), new NVec2(w, h), rgba(245, 245, 245));

        //Grid: one line per file unit (8 world px), fade minor lines out when zoomed away
        float step = Unit * zoom;
        if (step >= 5)
        {
            uint minor = rgba(220, 220, 220), major = rgba(190, 190, 190);
            float x0 = pan.X % step, y0 = pan.Y % step;
            int ix = (int)MathF.Floor(-pan.X / step);
            for (float x = x0; x < w; x += step, ix++)
                dl.AddLine(new NVec2(x, 0), new NVec2(x, h), ix % 8 == 0 ? major : minor);
            int iy = (int)MathF.Floor(-pan.Y / step);
            for (float y = y0; y < h; y += step, iy++)
                dl.AddLine(new NVec2(0, y), new NVec2(w, y), iy % 8 == 0 ? major : minor);
        }

        NVec2 nodePos(int i) => worldToScreen(doc.Nodes[i].X * Unit, doc.Nodes[i].Y * Unit);

        //Triangles (foreground fill)
        uint triCol = rgba(36, 67, 102, 90);
        foreach (var t in doc.Tris)
            dl.AddTriangleFilled(nodePos(t.A), nodePos(t.B), nodePos(t.C), triCol);

        //Walls
        uint wallCol = rgba(62, 126, 208);
        foreach (var wl in doc.Walls)
            dl.AddLine(nodePos(wl.A), nodePos(wl.B), wallCol, MathF.Max(1.5f, 3 * zoom));

        //Pads: game reads x*8-4 with size 196
        foreach (var p in doc.Pads)
        {
            var a = worldToScreen(p.X * Unit - 4, p.Y * Unit - 4);
            var b = worldToScreen(p.X * Unit - 4 + 196, p.Y * Unit - 4 + 196);
            uint col = p.Type switch { 0 => rgba(108, 185, 244), 2 => rgba(245, 27, 42), _ => rgba(255, 156, 0) };
            string label = p.Type switch { 0 => "START", 2 => "HEAL", _ => "END" };
            dl.AddRect(a, b, col, 0, ImDrawFlags.None, 2);
            dl.AddLine(a, b, col);
            dl.AddLine(new NVec2(a.X, b.Y), new NVec2(b.X, a.Y), col);
            dl.AddText(new NVec2(a.X + 4, a.Y + 4), col, label);
        }

        //Springs: box with direction tick (turns*90deg)
        uint springCol = rgba(30, 140, 30);
        foreach (var s in doc.Springs)
        {
            var c = worldToScreen(s.X * Unit, s.Y * Unit);
            float r = 6 * zoom;
            dl.AddRect(new NVec2(c.X - r, c.Y - r), new NVec2(c.X + r, c.Y + r), springCol, 0, ImDrawFlags.None, 2);
            float ang = s.Turns * MathF.PI / 2 - MathF.PI / 2;  //Turns=0 points up
            dl.AddLine(c, new NVec2(c.X + MathF.Cos(ang) * r * 2, c.Y + MathF.Sin(ang) * r * 2), springCol, 2);
        }

        //Launchers: sender circle, catcher circle, arrow between
        uint lCol = rgba(150, 60, 180);
        foreach (var l in doc.Launchers)
        {
            var s = worldToScreen(l.SX * Unit, l.SY * Unit);
            var c = worldToScreen(l.CX * Unit, l.CY * Unit);
            dl.AddCircle(s, 7 * zoom, lCol, 0, 2);
            dl.AddCircleFilled(c, 4 * zoom, lCol);
            dl.AddLine(s, c, rgba(150, 60, 180, 120), 1.5f);
            dl.AddText(new NVec2(s.X + 8, s.Y - 8), lCol, $"{l.FreqPct}%/{l.OffPct}%");
        }

        //Decals (sprites): 4px units. Red when the index doesn't exist in the theme (game skips those).
        uint dCol = rgba(120, 120, 120), dBad = rgba(220, 60, 60);
        int themeDecals = themeDecalCount();
        foreach (var m in doc.Decals)
        {
            uint col = themeDecals >= 0 && m.Index >= themeDecals ? dBad : dCol;
            var c = worldToScreen(m.X * 4, m.Y * 4);
            float r = 5 * zoom;
            dl.AddCircle(c, r, col, 4, 1.5f);   //Diamond
            dl.AddText(new NVec2(c.X + r, c.Y - r), col, $"#{m.Index} d{m.Depth:0.00}");
        }

        //Nodes on top
        uint nodeCol = rgba(40, 40, 40), nodeFill = rgba(255, 255, 255);
        float nr = MathF.Max(2.5f, 3.5f * zoom);
        for (int i = 0; i < doc.Nodes.Count; i++)
        {
            var c = nodePos(i);
            dl.AddRectFilled(new NVec2(c.X - nr, c.Y - nr), new NVec2(c.X + nr, c.Y + nr), nodeFill);
            dl.AddRect(new NVec2(c.X - nr, c.Y - nr), new NVec2(c.X + nr, c.Y + nr), nodeCol, 0, ImDrawFlags.None, 1.5f);
        }
    }
    #endregion

    #region ui
    private void drawUI()
    {
        var io = ImGui.GetIO();

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("New", ModName + "+N")) menuNew();
                if (ImGui.MenuItem("Open...", ModName + "+O")) { browserOpen = true; browserSave = false; }
                if (ImGui.MenuItem("Save", ModName + "+S", false, true)) menuSave();
                if (ImGui.MenuItem("Save As...")) { browserOpen = true; browserSave = true; }
                ImGui.Separator();
                if (ImGui.MenuItem("Playtest", ModName + "+P")) playtest();
                ImGui.Separator();
                if (ImGui.MenuItem("Quit", ModName + "+Q")) Exit();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                if (ImGui.MenuItem("Undo", ModName + "+Z", false, undoStack.CanUndo)) menuUndo();
                if (ImGui.MenuItem("Redo", ModName + "+Shift+Z", false, undoStack.CanRedo)) menuRedo();
                ImGui.Separator();
                if (ImGui.MenuItem("Copy", ModName + "+C", false, selection.Count > 0)) copySelection();
                if (ImGui.MenuItem("Paste", ModName + "+V", false, clipboard != null)) pasteClipboard(screenToWorld(ImGui.GetIO().DisplaySize / 2));
                if (ImGui.MenuItem("Delete", "Del", false, selection.Count > 0)) deleteSelection();
                ImGui.Separator();
                if (ImGui.MenuItem("Flip Horizontal", null, false, selection.Count > 0)) flipSelection(true);
                if (ImGui.MenuItem("Flip Vertical", null, false, selection.Count > 0)) flipSelection(false);
                if (ImGui.MenuItem("Scale Up 25%", ModName + "+=", false, selection.Count > 0)) scaleSelection(1.25f);
                if (ImGui.MenuItem("Scale Down 20%", ModName + "+-", false, selection.Count > 0)) scaleSelection(0.8f);
                ImGui.Separator();
                if (ImGui.MenuItem("Map Settings...")) settingsOpen = true;
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Reset view")) { zoom = 1; pan = new NVec2(64, 64); }
                if (ImGui.MenuItem("Fit map")) fitView();
                ImGui.Separator();
                if (ImGui.MenuItem("Heli mockup", "H", heliMockup)) heliMockup = !heliMockup;
                if (ImGui.MenuItem("Playtest trace", null, showTrace)) showTrace = !showTrace;
                if (ImGui.MenuItem("Clear trace", null, false, traceSegments.Count > 0)) { traceSegments.Clear(); try { File.Delete(tracePath); } catch { } }
                ImGui.EndMenu();
            }

            string name = currentPath == null ? "untitled" : Path.GetFileName(currentPath);
            ImGui.SameLine(io.DisplaySize.X - 400);
            ImGui.TextDisabled($"{name}{(dirty ? " *" : "")}  |  {doc.Nodes.Count}n {doc.Walls.Count}w {doc.Tris.Count}t {doc.Pads.Count}p");
            ImGui.EndMainMenuBar();
        }

        //Shortcuts (Cmd on macOS, Ctrl elsewhere)
        if (modDown(io))
        {
            if (ImGui.IsKeyPressed(ImGuiKey.N, false)) menuNew();
            if (ImGui.IsKeyPressed(ImGuiKey.S, false)) menuSave();
            if (ImGui.IsKeyPressed(ImGuiKey.O, false)) { browserOpen = true; browserSave = false; }
            if (ImGui.IsKeyPressed(ImGuiKey.P, false)) playtest();
            if (ImGui.IsKeyPressed(ImGuiKey.Z, false) && !io.KeyShift && undoStack.CanUndo) menuUndo();
            if (ImGui.IsKeyPressed(ImGuiKey.Z, false) && io.KeyShift && undoStack.CanRedo) menuRedo();
            if (ImGui.IsKeyPressed(ImGuiKey.C, false)) copySelection();
            if (ImGui.IsKeyPressed(ImGuiKey.V, false)) pasteClipboard(screenToWorld(io.MousePos));
            if (ImGui.IsKeyPressed(ImGuiKey.Equal, false)) scaleSelection(1.25f);
            if (ImGui.IsKeyPressed(ImGuiKey.Minus, false)) scaleSelection(0.8f);
            if (ImGui.IsKeyPressed(ImGuiKey.Q, false)) Exit();
        }

        //Status bar: cursor position in map units
        var world = screenToWorld(io.MousePos);
        ImGui.SetNextWindowPos(new NVec2(0, io.DisplaySize.Y - 24));
        ImGui.SetNextWindowSize(new NVec2(io.DisplaySize.X, 24));
        ImGui.Begin("##status", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground);
        //TODO remove "mods" readout once the macOS Cmd mapping question is settled
        ImGui.Text($"({(int)MathF.Round(world.X / Unit)}, {(int)MathF.Round(world.Y / Unit)})   zoom {zoom:0.0}x   theme: {doc.Theme}   mods: ctrl={io.KeyCtrl} super={io.KeySuper}");
        ImGui.End();

        if (browserOpen) drawFileBrowser();
        if (settingsOpen) drawSettings();
        if (parseErrors.Count > 0) drawErrors();
    }

    private void menuSave()
    {
        if (currentPath != null) saveMapFile(currentPath);
        else { browserOpen = true; browserSave = true; }
    }

    private void menuNew() { undoStack.Push(doc); doc = new MapDocument(); selection.Clear(); currentPath = null; dirty = false; }
    private void menuUndo() { doc = undoStack.Undo(doc); selection.Clear(); dirty = true; }
    private void menuRedo() { doc = undoStack.Redo(doc); selection.Clear(); dirty = true; }

    /// <summary>
    /// Saves the working map to a temp file and launches the game on it.
    /// </summary>
    private void playtest()
    {
        try
        {
            string tmp = Path.Combine(Path.GetTempPath(), "hokiedit-playtest.map");
            File.WriteAllText(tmp, doc.Serialize());

            string gameDir = findGameDir();
            if (gameDir == null)
            {
                parseErrors = new List<string> { "playtest: game binary not found — build src/Hoki first (dotnet build src/Hoki)" };
                return;
            }
            try { File.Delete(tracePath); } catch { }
            traceSegments.Clear();
            traceStamp = default;

            string exe = Path.Combine(gameDir, OperatingSystem.IsWindows() ? "Hoki.exe" : "Hoki");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exe,
                Arguments = "\"" + tmp + "\" --trace \"" + tracePath + "\"",
                WorkingDirectory = gameDir  //So config/scores/levels resolve like a normal run
            });
        }
        catch (Exception e)
        {
            parseErrors = new List<string> { "playtest failed: " + e.Message };
        }
    }

    private static string findGameDir()
    {
        //Editor and game live in sibling project dirs; probe both configurations
        foreach (string cfg in new[] { "Debug", "Release" })
        {
            string dir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, $"../../../../Hoki/bin/{cfg}/net8.0"));
            if (File.Exists(Path.Combine(dir, OperatingSystem.IsWindows() ? "Hoki.exe" : "Hoki"))) return dir;
        }
        return null;
    }

    /// <summary>
    /// Reloads the playtest trace when the game has appended a new attempt.
    /// Format: "x,y,millirads" per line (world px), "---" between attempts.
    /// </summary>
    private void refreshTrace()
    {
        try
        {
            if (!File.Exists(tracePath)) return;
            DateTime stamp = File.GetLastWriteTimeUtc(tracePath);
            if (stamp == traceStamp) return;
            traceStamp = stamp;

            traceSegments.Clear();
            var seg = new List<TraceSample>();
            foreach (string line in File.ReadAllLines(tracePath))
            {
                if (line == "---") { if (seg.Count > 1) traceSegments.Add(seg); seg = new List<TraceSample>(); continue; }
                string[] f = line.Split(',');
                if (f.Length >= 2 && float.TryParse(f[0], out float x) && float.TryParse(f[1], out float y))
                {
                    float r = f.Length >= 3 && int.TryParse(f[2], out int mr) ? mr / 1000f : 0;
                    seg.Add(new TraceSample { P = new NVec2(x, y), R = r });
                }
            }
            if (seg.Count > 1) traceSegments.Add(seg);
        }
        catch { }   //Best-effort: the game may be mid-write
    }

    private void fitView()
    {
        if (doc.Nodes.Count == 0) return;
        var io = ImGui.GetIO();
        float minX = doc.Nodes.Min(n => n.X) * Unit, maxX = doc.Nodes.Max(n => n.X) * Unit;
        float minY = doc.Nodes.Min(n => n.Y) * Unit, maxY = doc.Nodes.Max(n => n.Y) * Unit;
        float mw = MathF.Max(maxX - minX, 1), mh = MathF.Max(maxY - minY, 1);
        zoom = Math.Clamp(MathF.Min((io.DisplaySize.X - 100) / mw, (io.DisplaySize.Y - 100) / mh), 0.1f, 8f);
        pan = new NVec2(50 - minX * zoom, 50 - minY * zoom);
    }

    private void drawFileBrowser()
    {
        ImGui.SetNextWindowSize(new NVec2(520, 420), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(browserSave ? "Save map" : "Open map", ref browserOpen))
        {
            ImGui.TextWrapped(browserDir);
            if (ImGui.Button("..")) browserDir = Path.GetFullPath(Path.Combine(browserDir, ".."));
            ImGui.SameLine();
            ImGui.TextDisabled("|");
            ImGui.BeginChild("files", new NVec2(0, browserSave ? -64 : -8));
            foreach (string d in safeDirs(browserDir))
            {
                ImGui.SameLine(0, 0); ImGui.NewLine();
                if (ImGui.Selectable("[dir] " + Path.GetFileName(d))) browserDir = d;
            }
            foreach (string f in safeFiles(browserDir, "*.map"))
            {
                if (ImGui.Selectable(Path.GetFileName(f)))
                {
                    if (browserSave) browserFileName = Path.GetFileName(f);
                    else { loadMapFile(f); browserOpen = false; }
                }
            }
            ImGui.EndChild();
            if (browserSave)
            {
                ImGui.InputText("name", ref browserFileName, 128);
                if (ImGui.Button("Save"))
                {
                    string name = browserFileName.EndsWith(".map") ? browserFileName : browserFileName + ".map";
                    saveMapFile(Path.Combine(browserDir, name));
                    browserOpen = false;
                }
            }
        }
        ImGui.End();
    }

    private static IEnumerable<string> safeDirs(string dir)
    {
        try { return Directory.GetDirectories(dir).OrderBy(x => x); } catch { return Array.Empty<string>(); }
    }
    private static IEnumerable<string> safeFiles(string dir, string pat)
    {
        try { return Directory.GetFiles(dir, pat).OrderBy(x => x); } catch { return Array.Empty<string>(); }
    }

    private void drawSettings()
    {
        ImGui.SetNextWindowSize(new NVec2(380, 160), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Map Settings", ref settingsOpen))
        {
            string a = doc.Author, t = doc.Theme, g = doc.Ghost;
            if (ImGui.InputText("Author", ref a, 64)) { doc.Author = a; dirty = true; }
            if (ImGui.InputText("Theme", ref t, 64)) { doc.Theme = t; dirty = true; }
            ImGui.SameLine(); helpMarker("Theme file name, e.g. castle.theme. Must exist in the game's data/themes.");
            if (ImGui.InputText("Ghost", ref g, 64)) { doc.Ghost = g; dirty = true; }
            ImGui.SameLine(); helpMarker("Tutor ghost file name (optional).");
        }
        ImGui.End();
    }

    private void drawErrors()
    {
        ImGui.SetNextWindowPos(new NVec2(ImGui.GetIO().DisplaySize.X / 2 - 200, 60), ImGuiCond.Appearing);
        bool open = true;
        if (ImGui.Begin("Load warnings", ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            foreach (string e in parseErrors.Take(20)) ImGui.TextWrapped(e);
            if (parseErrors.Count > 20) ImGui.TextDisabled($"...and {parseErrors.Count - 20} more");
            if (ImGui.Button("Dismiss")) parseErrors.Clear();
        }
        ImGui.End();
        if (!open) parseErrors.Clear();
    }

    private static void helpMarker(string text)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }
    #endregion

    private static void Main(string[] args)
    {
        //--roundtrip <dir>: parse+serialize every .map twice, verify stability. CI/self-check, no window.
        if (args.Length >= 2 && args[0] == "--roundtrip")
        {
            int bad = 0;
            foreach (string f in Directory.GetFiles(args[1], "*.map"))
            {
                var d1 = MapDocument.Parse(File.ReadAllText(f), out var errs);
                string s1 = d1.Serialize();
                var d2 = MapDocument.Parse(s1, out var errs2);
                string s2 = d2.Serialize();
                bool stable = s1 == s2 && errs2.Count == 0;
                bool counts = d1.Nodes.Count == d2.Nodes.Count && d1.Walls.Count == d2.Walls.Count && d1.Tris.Count == d2.Tris.Count
                    && d1.Pads.Count == d2.Pads.Count && d1.Springs.Count == d2.Springs.Count && d1.Launchers.Count == d2.Launchers.Count && d1.Decals.Count == d2.Decals.Count;
                Console.WriteLine($"{Path.GetFileName(f)}: {(stable && counts ? "OK" : "FAIL")} ({d1.Nodes.Count}n {d1.Walls.Count}w {d1.Tris.Count}t {d1.Pads.Count}p {d1.Springs.Count}s {d1.Launchers.Count}l {d1.Decals.Count}d){(errs.Count > 0 ? $" [{errs.Count} load warnings]" : "")}");
                if (!(stable && counts)) bad++;
            }
            Environment.Exit(bad == 0 ? 0 : 1);
        }

        using var game = new EditorGame(args);
        game.Run();
    }
}
