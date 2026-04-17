namespace CodenameApp.Domain;

public class Codename
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    public Codename(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
    }
}