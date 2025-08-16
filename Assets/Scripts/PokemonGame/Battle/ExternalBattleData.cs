using System.Collections.Generic;
using PokemonGame.Game.Party;
using PokemonGame.General;

namespace PokemonGame.Battle
{
    public struct ExternalBattleData
    {
        public int playerOneBattlerIndex;
        public int playerTwoBattlerIndex;
        public TurnStatus currentTurn;
        public Party playerOneParty;
        public Party playerTwoParty;
        public Battler playerOneCurrentBattler => playerOneParty[playerOneBattlerIndex];
        public Battler playerTwoCurrentBattler => playerTwoParty[playerTwoBattlerIndex];
        public List<Battler> playerOneBattlersThatParticipated;

        public static ExternalBattleData Construct(Battle battle)
        {
            ExternalBattleData data = new ExternalBattleData(battle.playerOneBattlerIndex, battle.playerTwoBattlerIndex,
                battle.currentTurn, battle.partyOne, battle.partyTwo, battle.playerOneBattlersThatParticipated);
            return data;
        }
        
        public ExternalBattleData(int playerOneBattlerIndex, int playerTwoBattlerIndex, TurnStatus currentTurn,
            Party playerOneParty, Party playerTwoParty, List<Battler> playerOneBattlersThatParticipated)
        {
            this.playerOneBattlerIndex = playerOneBattlerIndex;
            this.playerTwoBattlerIndex = playerTwoBattlerIndex;
            this.currentTurn = currentTurn;
            this.playerOneParty = playerOneParty;
            this.playerTwoParty = playerTwoParty;
            this.playerOneBattlersThatParticipated = playerOneBattlersThatParticipated;
        }
    }
}