namespace PokemonGame.ScriptableObjects
{
    using System;
    using General;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [CreateAssetMenu(fileName = "New Ability", menuName = "Pokemon Game/New Ability")]
    public class Ability : ScriptableObject
    {
        public new string name;
    
        public List<AbilityEffectTrigger> triggers;
    }

    [Serializable]
    public class AbilityEffectTrigger
    {
        public EffectTrigger trigger;
        public UnityEvent<AbilityEventArgs> effectEvent;
    }
    
    public class AbilityEventArgs : EventArgs
    {
        public AbilityEventArgs(Battler battler)
        {
            this.Battler = battler;
        }
        
        public Battler Battler;
    }
}