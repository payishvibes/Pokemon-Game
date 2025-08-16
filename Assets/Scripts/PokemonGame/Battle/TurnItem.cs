using System.Collections.Generic;

namespace PokemonGame.Battle
{
    public class TurnItem
    {
        public TurnItemType Type;
        public List<object> Variables;

        public TurnItem(TurnItemType type, List<object> variables)
        {
            Type = type;
            Variables = variables;
        }

        public TurnItem()
        {
            
        }
    }
    
    
    public enum TurnItemType
    {
        StartDelay,
        PlayerOneMove,
        PlayerTwoMove,
        PlayerOneMissed,
        PlayerTwoMissed,
        PlayerOneSwapBecauseFainted,
        PlayerTwoSwapBecauseFainted,
        PlayerOneSwap,
        PlayerTwoSwap,
        PlayerOneLevelUp,
        PlayerOneItem,
        PlayerTwoItem,
        EndBattlePlayerOneWin,
        EndBattlePlayerTwoWin,
        StartOfTurnStatusEffects,
        EndOfTurnStatusEffects,
        PlayerOneParalysed,
        PlayerTwoParalysed,
        PlayerOneAsleep,
        PlayerTwoAsleep,
        CatchAttempt,
        Run,
    }   
}