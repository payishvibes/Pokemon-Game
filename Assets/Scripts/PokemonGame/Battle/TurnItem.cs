namespace PokemonGame.Battle
{
    public enum TurnItem
    {
        StartDelay,
        PlayerMove,
        PlayerMissed,
        OpponentMove,
        OpponentMissed,
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