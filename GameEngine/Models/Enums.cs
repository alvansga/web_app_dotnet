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
    Heart,
    Brain,
    Lungs,
    Liver,
    Kidneys
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