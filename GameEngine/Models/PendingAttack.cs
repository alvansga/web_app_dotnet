namespace WebAppSandbox.GameEngine.Models;

/// <summary>
/// An offensive card that has been played but is held in a short response
/// window so the defending player can respond with an Instant (e.g. Immunity
/// Boost). If not blocked before the window expires, the attack resolves.
/// </summary>
public class PendingAttack
{
    public required string Id { get; init; }
    public required Card Card { get; init; }
    public required Player Caster { get; init; }
    public required Player TargetOwner { get; init; }
    public required Organ TargetOrgan { get; init; }
}