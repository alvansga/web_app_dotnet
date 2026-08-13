namespace WebAppSandbox.GameEngine.Models;

public class Card
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public CardType Type { get; init; }
    public TargetSide TargetSide { get; init; } = TargetSide.None;
    public OrganType? TargetOrganType { get; init; }
    public AfflictionType AfflictionType { get; init; } = AfflictionType.None;
    public required string Description { get; init; }

    public bool RequiresOrganMatch => TargetOrganType.HasValue;
}