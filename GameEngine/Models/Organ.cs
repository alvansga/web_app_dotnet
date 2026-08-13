namespace WebAppSandbox.GameEngine.Models;

public class Organ
{
    public OrganType Type { get; init; }
    public int Position { get; init; }
    public bool IsDestroyed { get; set; }
    public bool IsShielded { get; set; }
    public List<AfflictionType> Afflictions { get; } = new();

    public bool IsAfflicted => Afflictions.Count > 0;

    public Organ(OrganType type, int position)
    {
        Type = type;
        Position = position;
    }

    public void AddAffliction(AfflictionType affliction)
    {
        if (affliction != AfflictionType.None && !Afflictions.Contains(affliction))
        {
            Afflictions.Add(affliction);
        }
    }

    public void RemoveAffliction(AfflictionType affliction)
    {
        Afflictions.Remove(affliction);
    }
}