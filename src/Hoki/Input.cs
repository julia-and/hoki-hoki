namespace Hoki;

/// <summary>
/// A Controller input at a specific time
/// </summary>
internal struct Input
{
    /// <summary>
    /// Frame in which the input occurred
    /// </summary>
    public int Frame;
    /// <summary>
    /// Button used
    /// </summary>
    public Controls Control;
    /// <summary>
    /// Whether the button was pressed down or released
    /// </summary>
    public bool Down;

    public Input(int frame, Controls control, bool down)
    {
        Frame = frame;
        Control = control;
        Down = down;
    }
}
