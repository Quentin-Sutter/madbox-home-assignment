namespace Madbox.InputSystem
{
    /// <summary>
    /// Simple seam so movement systems can consume input intent without knowing UI details.
    /// </summary>
    public interface IMoveIntentSource
    {
        MoveIntent CurrentIntent { get; }
    }
}
