using System.Collections;
using SpriteUtilities;

namespace Hoki;

/// <summary>
/// Executes controls using ghost data
/// </summary>
public class GhostController : Controller, Updateable
{
    private const int numControls = 7;

    private Queue inputs;
    private bool[] states;
    private int frame = 0;
    private bool started = false;

    public Queue Inputs
    {
        get { return inputs; }
    }

    public GhostController()
    {
        //Create an input queue
        inputs = new Queue();

        //Create a states array
        states = new bool[numControls]; //Automatically initialized to false

        //Capture own events to avoid NRE
        ControlDown += new ControlEventHandler(onControlDown);
        ControlUp += new ControlEventHandler(onControlUp);
    }

    public bool Started
    {
        get { return started; }
    }

    public void Construct(string inputs)
    {
        string[] lines = inputs.Split('\n');
        foreach (string line in lines)
        {
            if (line.Length == 0) continue; //Ignore blank lines
            string[] data = line.Split(',');
            this.inputs.Enqueue(new Input(int.Parse(data[0]), (Controls)int.Parse(data[1]), int.Parse(data[2]) == 1));
        }
    }

    /// <summary>
    /// Begins keeping time and working through the queue
    /// </summary>
    public void Start()
    {
        started = true;
    }

    #region Controller Members

    public event Hoki.ControlEventHandler ControlDown;
    public event Hoki.ControlEventHandler ControlUp;

    public bool Down(Controls c)
    {
        return states[(int)c];
    }

    #endregion

    #region Updateable Members

    public void Update(float elapsedTime)
    {
        if (!started || inputs.Count == 0) return;  //Haven't begun, or no inputs left

        //Keep track of what frame it is
        while (((Input)inputs.Peek()).Frame == frame)
        {   //While there are inputs that have occurred...
            //Get the control
            Input i = (Input)inputs.Dequeue();

            //Fire the event
            if (i.Down) ControlDown(this, new ControlEventArgs(i.Control));
            else ControlUp(this, new ControlEventArgs(i.Control));

            //Avoid peeking at an empty queue
            if (inputs.Count == 0) break;
        }
        frame++;
    }

    public void Reset()
    {
        frame = 0;
    }

    #endregion

    private void onControlDown(object sender, ControlEventArgs e)
    {
        states[(int)e.Control] = true;
    }

    private void onControlUp(object sender, ControlEventArgs e)
    {
        states[(int)e.Control] = false;
    }
}
