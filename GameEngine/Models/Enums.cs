namespace WebAppSandbox.GameEngine.Models;

public enum CardType
{
    Affliction,
    Attack,
    Treatment,
    Defense,
    Special
}

public enum OrganType
{
    Appendix,
    Bladder,
    Bones,
    Bowels,
    Brain,
    Esophagus,
    Eyes,
    Gallbladder,
    Heart,
    Kidneys,
    Liver,
    Lungs,
    Nose,
    Pancreas,
    Skin,
    Spleen,
    Stomach,
    Teeth,
    Thyroid,
    Tongue,
    Tonsils,
    Trachea,
    Wild_Organ,
}

public enum GamePhase
{
    WaitingForPlayer,
    Playing,
    GameOver
}

public enum TargetSide
{
    None,
    Self,
    Opponent
}

public enum AfflictionType
{
    None,
    Afflicted
}