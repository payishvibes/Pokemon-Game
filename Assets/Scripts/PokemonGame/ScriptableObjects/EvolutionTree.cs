namespace PokemonGame.ScriptableObjects
{
    using System;
    using System.Collections.Generic;
    using General;
    
    [Serializable]
    public class Evolution
    {
        public List<EvolutionData> possibleEvolutions;
    }

    [Serializable]
    public class EvolutionData
    {
        public EvolutionTriggerType triggerType;
    
        [ConditionalHide("triggerType", 0)]
        public int level;
        [ConditionalHide("triggerType", 1)]
        public Item item;
    
        public BattlerTemplate evolution;
    }

    public enum EvolutionTriggerType
    {
        Level,
        Item,
    }

    public enum ExperienceGroup
    {
        MediumFast,
        Erratic,
        Fluctuating,
        MediumSlow,
        Fast,
        Slow
    }
}