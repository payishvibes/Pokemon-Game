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
        
        public TurnItem(TurnItemType type)
        {
            Type = type;
            Variables = new List<object>();
        }

        public TurnItem()
        {
            Variables = new List<object>();
        }
    }
    
    
    public enum TurnItemType
    {
        StartDelay,
        PlayerOneMove,
        PlayerTwoMove,
        PlayerOneSwapBecauseFainted,
        PlayerTwoSwapBecauseFainted,
        PlayerOneSwap,
        PlayerTwoSwap,
        PlayerOneLevelUp,
        PlayerOneEvolved,
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