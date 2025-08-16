namespace PokemonGame.Battle
{
    public enum TurnItem
    {
        StartDelay,
        PlayerOneMove,
        PlayerOneMissed,
        PlayerTwoMove,
        PlayerTwoMissed,
        PlayerOneSwapBecauseFainted,
        PlayerOneSwap,
        PlayerTwoSwap,
        PlayerTwoSwapBecauseFainted,
        PlayerOneLevelUp,
        PlayerOneItem,
        PlayerTwoItem,
        EndBattlePlayerOneWin,
        EndBattlePlayerTwoWin,
        StartOfTurnStatusEffects,
        EndOfTurnStatusEffects,
        PlayerOneParalysed,
        PlayerOneAsleep,
        PlayerTwoParalysed,
        PlayerTwoAsleep,
        CatchAttempt,
        Run,
    }   
}