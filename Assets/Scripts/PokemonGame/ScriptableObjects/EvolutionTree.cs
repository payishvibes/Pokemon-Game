using PokeApiNet;
using UnityEngine;

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
        [ConditionalHide("useItem")] 
        public Item useItem;
        [ConditionalHide("heldItem")] 
        public Item heldItem;

        [ConditionalHide("gender")] 
        public Gender? gender;
        [ConditionalHide("knownMove")] 
        public Move knownMove;
        [ConditionalHide("knownMoveType")] 
        public Type knownMoveType;
        // public something location;
        [ConditionalHide("minAffection", -1, true)] 
        public int minAffection;
        [ConditionalHide("minBeauty", -1, true)] 
        public int minBeauty;
        [ConditionalHide("minHappiness", -1, true)] 
        public int minHappiness;
        [ConditionalHide("needRain")] 
        public bool needRain;
        public string timeOfDay;
        public string tradeSpecies;
    
        public BattlerTemplate evolution;
    }

    public enum EvolutionTriggerType
    {
        Level,
        UseItem,
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