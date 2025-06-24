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
        OpponentParalysed,
        CatchAttempt,
        Run,
    }   
}