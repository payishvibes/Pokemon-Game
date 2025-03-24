using PokemonGame.Global;
using PokemonGame.ScriptableObjects;

namespace PokemonGame.Game.Party
{
    using General;

    using UnityEngine;

    /// <summary>
    /// Manages the players party
    /// </summary>
    public static class PartyManager
    {
        private static Party _playerParty;

        /// <summary>
        /// Add a battler to the player party
        /// </summary>
        /// <param name="battlerToAdd">The battler that you add to the party</param>
        public static void AddBattler(Battler battlerToAdd)
        {
            _playerParty.Add(battlerToAdd);
        }

        /// <summary>
        /// Change the player party without simply adding one
        /// </summary>
        /// <param name="party">The new party</param>
        public static void SetPlayerParty(Party party)
        {
            _playerParty = party;
        }

        public static void HealAll()
        {
            for (int i = 0; i < _playerParty.Count; i++)
            {
                _playerParty[i].currentHealth = _playerParty[i].stats.maxHealth;
                _playerParty[i].isFainted = false;
                _playerParty[i].statusEffect = StatusEffect.Healthy;
                for (int j = 0; j < _playerParty[i].movePpInfos.Count; j++)
                {
                    _playerParty[i].movePpInfos[j].Restore();
                }
            }
        }

        public static Party GetParty()
        {
            return _playerParty;
        }
    }   
}