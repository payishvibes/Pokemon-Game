namespace PokemonGame.ScriptableObjects
{
    using System;
    using System.Collections.Generic;
    using General;
    using UnityEngine;

    [CreateAssetMenu(order = 6, fileName = "new Evolution Tree", menuName = "Pokemon Game/New Evolution Tree")]
    public class EvolutionTree : ScriptableObject
    {
        public ExperienceGroup expGroup;
        public List<Evolution> evolutions = new List<Evolution>();
    }

    [Serializable]
    public class Evolution
    {
        public BattlerTemplate initial;
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