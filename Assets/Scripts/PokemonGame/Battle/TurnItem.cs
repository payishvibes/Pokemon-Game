namespace PokemonGame.Battle
{
    public enum TurnItem
    {
        StartDelay,
        PlayerMove,
        OpponentMove,
        PlayerSwapBecauseFainted,
        PlayerSwap,
        OpponentSwap,
        OpponentSwapBecauseFainted,
        PlayerLevelUp,
        PlayerItem,
        OpponentItem,
        EndBattlePlayerWin,
        EndBattleOpponentWin,
        StartOfTurnStatusEffects,
        EndOfTurnStatusEffects,
        PlayerParalysed,
        PlayerAsleep,
        OpponentParalysed,
        OpponentAsleep,
        CatchAttempt,
        Run,
    }   
}