using System.Collections;
using SpriteUtilities;

namespace Hoki;

/// <summary>
/// A recorded input sequence
/// </summary>
public class GhostRecorder : Updateable
{
    private Queue inputs;   //Queue of inputs recorded
    private int frame;      //Frames passed since recording began

    public GhostRecorder(Controller controller)
    {
        //Make a queue to hold inputs
        inputs = new Queue();

        //Capture the controller's events
        controller.ControlDown += new ControlEventHandler(onControlDown);
        controller.ControlUp += new ControlEventHandler(onControlUp);
    }

    /// <summary>
    /// Compiles the ghost and returns it as a string. This will clear the input queue.
    /// </summary>
    public string Compile()
    {
        string output = "";
        while (inputs.Count > 0)
        {
            Input i = (Input)inputs.Dequeue();
            int down = 0;
            if (i.Down) down = 1;
            output += "\n" + i.Frame + "," + (int)i.Control + "," + down;
        }
        return output;
    }

    #region Updateable Members
    public void Update(float elapsedTime)
    {
        frame++;
    }
    #endregion

    #region event handlers
    private void onControlDown(object sender, ControlEventArgs e)
    {
        inputs.Enqueue(new Input(frame, e.Control, true));
    }

    private void onControlUp(object sender, ControlEventArgs e)
    {
        inputs.Enqueue(new Input(frame, e.Control, false));
    }
    #endregion
}
