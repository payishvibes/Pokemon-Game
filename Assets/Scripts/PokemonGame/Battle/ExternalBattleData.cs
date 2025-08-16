using System.Collections.Generic;
using PokemonGame.Game.Party;
using PokemonGame.General;

namespace PokemonGame.Battle
{
    public struct ExternalBattleData
    {
        public int currentBattlerIndex;
        public int opponentBattlerIndex;
        public TurnStatus currentTurn;
        public Party playerParty;
        public Party opponentParty;
        public Battler playerCurrentBattler => playerParty[currentBattlerIndex];
        public Battler opponentCurrentBattler => opponentParty[opponentBattlerIndex];
        public List<Battler> battlersThatParticipated;

        public static ExternalBattleData Construct(Battle battle)
        {
            ExternalBattleData data = new ExternalBattleData(battle.currentBattlerIndex, battle.opponentBattlerIndex,
                battle.currentTurn, battle.partyOne, battle.partyTwo, battle.battlersThatParticipated);
            return data;
        }
        
        public ExternalBattleData(int currentBattlerIndex, int opponentBattlerIndex, TurnStatus currentTurn, Party playerParty, Party opponentParty, List<Battler> battlersThatParticipated)
        {
            this.currentBattlerIndex = currentBattlerIndex;
            this.opponentBattlerIndex = opponentBattlerIndex;
            this.currentTurn = currentTurn;
            this.playerParty = playerParty;
            this.opponentParty = opponentParty;
            this.battlersThatParticipated = battlersThatParticipated;
        }
    }
}