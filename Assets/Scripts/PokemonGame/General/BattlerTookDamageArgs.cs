using System;
using PokemonGame.ScriptableObjects;

namespace PokemonGame.General
{
    public class BattlerTookDamageArgs : EventArgs
    {
        public DamageSource source;
        public Battler damaged;

        public BattlerTookDamageArgs(DamageSource source, Battler damaged)
        {
            this.source = source;
            this.damaged = damaged;
        }
    }
}