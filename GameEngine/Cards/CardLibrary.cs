using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Cards;

public static class CardLibrary
{
    public const int AfflictionsPerOrgan = 3;
    public const int AttacksPerOrgan = 2;
    public const int TreatmentCopies = 4;
    public const int DefenseCopies = 4;
    public const int NecrosisCopies = 5;
    public const int TransplantCopies = 1;

    /// <summary>
    /// Builds the card library for a specific set of organ types.
    /// The Wild organ gets affliction cards but no standard attack cards,
    /// because it can only be destroyed by accumulating 4 afflictions.
    /// </summary>
    public static IReadOnlyList<Card> BuildDeck(IReadOnlyCollection<OrganType> organTypes)
    {
        var cards = new List<Card>();

        foreach (var organ in organTypes)
        {
            // Wild organ cannot be destroyed by a standard attack.
            // MODIFIED: Wild organ can be destroyed by any standard attack or affliction, but it requires 4 afflictions to destroy it. So we will not add standard attack cards for the wild organ.
            if (organ == OrganType.Wild_Organ)
            {
                continue;
            }

            // Affliction: 2 per organ type (1 affliction counter each).
            for (int i = 0; i < AfflictionsPerOrgan; i++)
            {
                cards.Add(new Card
                {
                    Id = $"affliction-{organ.ToString().ToLowerInvariant()}-{i}",
                    Name = $"Afflict {organ}",
                    Type = CardType.Affliction,
                    TargetSide = TargetSide.Opponent,
                    TargetOrganType = organ,
                    AfflictionType = AfflictionType.Afflicted,
                    AfflictionAmount = 1,
                    Description = $"Afflict target's {organ}."
                });
            }

            // Attack: 2 per organ type (requires match + afflicted).
            for (int i = 0; i < AttacksPerOrgan; i++)
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

        // Necrosis: counts as 2 full afflictions on any organ.
        for (int i = 0; i < NecrosisCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"necrosis-{i}",
                Name = "Necrosis",
                Type = CardType.Attack,
                TargetSide = TargetSide.Opponent,
                AfflictionAmount = 2,
                Description = "Counts as 2 full afflictions on any organ."
            });
        }

        // Transplant: steal one completely healthy organ from an opponent.
        for (int i = 0; i < TransplantCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"transplant-{i}",
                Name = "Transplant",
                Type = CardType.Special,
                TargetSide = TargetSide.Opponent,
                SpecialCard = SpecialCardType.Transplant,
                Description = "Permanently steal one completely healthy organ from an opponent's body to add to your own."
            });
        }

        // Treatment: 4 generic (target self, remove affliction)
        for (int i = 0; i < TreatmentCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"treatment-{i}",
                Name = "Treatment",
                Type = CardType.Treatment,
                TargetSide = TargetSide.Self,
                Description = "Remove all afflictions from one of your organs."
            });
        }

        // Defense: 4 generic (target self, shield)
        for (int i = 0; i < DefenseCopies; i++)
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