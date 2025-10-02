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
    
        [ConditionalHide("minLevel", -1, true)] 
        public int minLevel;
        [ConditionalHideObject("useItem", null, true)]
        public Item useItem;
        [ConditionalHideObject("heldItem", null, true)]
        public Item heldItem;

        [ConditionalHideObject("gender", null, true)]
        public Gender? gender;
        [ConditionalHideObject("knownMove", null, true)]
        public Move knownMove;
        [ConditionalHideObject("knownMoveType", null, true)]
        public Type knownMoveType;
        // public something location;
        [ConditionalHide("minAffection", -1, true)] 
        public int minAffection;
        [ConditionalHide("minBeauty", -1, true)] 
        public int minBeauty;
        [ConditionalHide("minHappiness", -1, true)] 
        public int minHappiness;
        [ConditionalHide("needRain", true)] 
        public bool needRain;
        [ConditionalHide("timeOfDay", "", true)] 
        public string timeOfDay;
        [ConditionalHide("tradeSpecies", "", true)] 
        public string tradeSpecies;
    
        public BattlerTemplate evolution;
    }

    public enum EvolutionTriggerType
    {
        Level,
        UseItem,
        Trade
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