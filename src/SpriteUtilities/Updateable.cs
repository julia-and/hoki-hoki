namespace SpriteUtilities;

public interface Updateable
{
    /// <summary>
    /// Call each frame so that the object can make whatever changes to itself necessary
    /// </summary>
    /// <param name="ElapsedTime">Time (in seconds) since the last call</param>
    void Update(float elapsedTime);
}
