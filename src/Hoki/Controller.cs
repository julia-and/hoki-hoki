namespace Hoki;

/// <summary>
/// Object that provides input from an enumerated list of virtual two-state buttons
/// </summary>
public interface Controller
{
    event ControlEventHandler ControlDown;
    event ControlEventHandler ControlUp;
    bool Down(Controls c);
}
