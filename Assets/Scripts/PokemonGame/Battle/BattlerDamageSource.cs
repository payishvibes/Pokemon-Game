using System.Collections.Generic;
using PokemonGame.General;
using PokemonGame.ScriptableObjects;

namespace PokemonGame.Battle
{
    public class BattlerDamageSource : DamageSource
    {
        public Battler sourceBattler;

        public BattlerDamageSource(Battler sourceBattler)
        {
            this.sourceBattler = sourceBattler;
        }
    }

    public class StatusEffectDamageSource : DamageSource
    {
        public StatusEffect statusEffect;

        public StatusEffectDamageSource(StatusEffect statusEffect)
        {
            this.statusEffect = statusEffect;
        }
    }
}