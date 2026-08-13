using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Cards;

public static class CardLibrary
{
    public static IReadOnlyList<Card> BuildDeck()
    {
        var cards = new List<Card>();
        var organTypes = Enum.GetValues<OrganType>();

        // Affliction: 2 per organ type
        foreach (var organ in organTypes)
        {
            for (int i = 0; i < 2; i++)
            {
                cards.Add(new Card
                {
                    Id = $"affliction-{organ.ToString().ToLowerInvariant()}-{i}",
                    Name = $"Afflict {organ}",
                    Type = CardType.Affliction,
                    TargetSide = TargetSide.Opponent,
                    TargetOrganType = organ,
                    AfflictionType = AfflictionType.Afflicted,
                    Description = $"Afflict target's {organ}."
                });
            }
        }

        // Attack: 2 per organ type (requires match + afflicted)
        foreach (var organ in organTypes)
        {
            for (int i = 0; i < 2; i++)
            {
                cards.Add(new Card
                {
                    Id = $"attack-{organ.ToString().ToLowerInvariant()}-{i}",
                    Name = $"Attack {organ}",
                    Type = CardType.Attack,
                    TargetSide = TargetSide.Opponent,
                    TargetOrganType = organ,
                    Description = $"Destroy target's afflicted {organ}."
                });
            }
        }

        // Treatment: 4 generic (target self, remove affliction)
        for (int i = 0; i < 4; i++)
        {
            cards.Add(new Card
            {
                Id = $"treatment-{i}",
                Name = "Treatment",
                Type = CardType.Treatment,
                TargetSide = TargetSide.Self,
                Description = "Remove an affliction from one of your organs."
            });
        }

        // Defense: 4 generic (target self, shield)
        for (int i = 0; i < 4; i++)
        {
            cards.Add(new Card
            {
                Id = $"defense-{i}",
                Name = "Defense",
                Type = CardType.Defense,
                TargetSide = TargetSide.Self,
                Description = "Shield one of your organs."
            });
        }

        return cards;
    }
}