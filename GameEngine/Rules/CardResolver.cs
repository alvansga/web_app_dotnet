using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Rules;

public class CardResolver
{
    public bool ShieldConsumed { get; private set; }

    public void Resolve(Card card, Organ targetOrgan)
    {
        ShieldConsumed = false;

        switch (card.Type)
        {
            case CardType.Affliction:
                ResolveAffliction(card, targetOrgan);
                break;
            case CardType.Attack:
                ResolveAttack(card, targetOrgan);
                break;
            case CardType.Treatment:
                ResolveTreatment(targetOrgan);
                break;
            case CardType.Defense:
                ResolveDefense(targetOrgan);
                break;
            case CardType.Special:
                throw new GameRuleException("Special cards are not implemented in MVP.");
            default:
                throw new GameRuleException($"Unknown card type: {card.Type}.");
        }
    }

    /// <summary>
    /// Validates that an offensive card can legally target the given organ,
    /// without mutating any state. Used when staging a PendingAttack so the
    /// attacker's card is not discarded if the target is invalid.
    /// </summary>
    public void ValidateOffensiveTarget(Card card, Organ target)
    {
        if (target.IsDestroyed)
        {
            throw new GameRuleException(
                $"Cannot {(card.Type == CardType.Affliction ? "afflict" : "attack")} a destroyed organ.");
        }

        if (card.Type == CardType.Attack && card.AfflictionAmount <= 0)
        {
            if (target.Type == OrganType.Wild_Organ)
            {
                throw new GameRuleException("Wild organ cannot be destroyed by a standard attack. It needs 4 afflictions.");
            }

            if (!target.IsAfflicted)
            {
                throw new GameRuleException("Attack requires the target organ to be afflicted.");
            }
        }
    }

    private void ResolveAffliction(Card card, Organ target)
    {
        ValidateOffensiveTarget(card, target);

        if (target.IsShielded)
        {
            target.IsShielded = false;
            ShieldConsumed = true;
            return;
        }

        int amount = card.AfflictionAmount > 0 ? card.AfflictionAmount : 1;
        ApplyAfflictions(target, amount);
    }

    private void ResolveAttack(Card card, Organ target)
    {
        ValidateOffensiveTarget(card, target);

        if (target.IsShielded)
        {
            target.IsShielded = false;
            ShieldConsumed = true;
            return;
        }

        // Necrosis-style attack: applies affliction counters instead of
        // instantly destroying the organ.
        if (card.AfflictionAmount > 0)
        {
            ApplyAfflictions(target, card.AfflictionAmount);
            return;
        }

        target.IsDestroyed = true;
    }

    private void ApplyAfflictions(Organ target, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            target.AddAffliction(AfflictionType.Afflicted);
        }

        if (target.Afflictions.Count >= target.AfflictionsToDestroy)
        {
            target.IsDestroyed = true;
        }
    }

    private void ResolveTreatment(Organ target)
    {
        if (target.IsDestroyed)
        {
            throw new GameRuleException("Cannot treat a destroyed organ.");
        }

        if (!target.IsAfflicted)
        {
            throw new GameRuleException("Target organ is not afflicted.");
        }

        target.Afflictions.Clear();
    }

    private void ResolveDefense(Organ target)
    {
        if (target.IsDestroyed)
        {
            throw new GameRuleException("Cannot shield a destroyed organ.");
        }

        if (target.IsShielded)
        {
            throw new GameRuleException("Target organ is already shielded.");
        }

        target.IsShielded = true;
    }
}