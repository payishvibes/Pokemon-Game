using System.Collections.Generic;
using PokemonGame.General;

namespace PokemonGame.Global
{
    using ScriptableObjects;
    using UnityEngine;
    
    public static class Registry
    {
        /// <summary>
        /// Gets an item from the items folder using the name you supply
        /// </summary>
        /// <param name="itemName">The name of the item</param>
        /// <returns>The item that was found</returns>
        public static Item GetItem(string itemName)
        {
            return (Item)Get(itemName, "Item");
        }
        
        /// <summary>
        /// Gets an ai from the ais folder using the name you supply
        /// </summary>
        /// <param name="aiName">The name of the ai</param>
        /// <returns>The AI that was found</returns>
        public static EnemyAI GetAI(string aiName)
        {
            return (EnemyAI)Get(aiName, "AI");
        }
        
        /// <summary>
        /// Gets a move from the moves folder using the name you supply
        /// </summary>
        /// <param name="moveName">The name of the move</param>
        /// <returns>The move that was found</returns>
        public static Move GetMove(string moveName)
        {
            return (Move)Get(moveName, "Move");
        }
        
        /// <summary>
        /// Gets a status effect from the status effects folder using the name you supply
        /// </summary>
        /// <param name="effectName">The name of the status effect</param>
        /// <returns>The status effect that was found</returns>
        public static StatusEffect GetStatusEffect(string effectName)
        {
            return (StatusEffect)Get(effectName, "Status Effect");
        }
        
        /// <summary>
        /// Gets a type from the type folder using the name you supply
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <returns>The type that was found</returns>
        public static Type GetType(string typeName)
        {
            return (Type)Get(typeName, "Type");
        }
        
        /// <summary>
        /// Gets a battler from the battlers folder using the name you supply
        /// </summary>
        /// <param name="battlerName">The name of the battler</param>
        /// <returns>The battler that was found</returns>
        public static Battler GetBattler(string battlerName)
        {
            return (Battler)Get(battlerName, "Battler");
        }
        
        /// <summary>
        /// Gets a battler from the battler templates folder using the name you supply
        /// </summary>
        /// <param name="battlerTemplateName">The name of the battler template</param>
        /// <returns>The battler template that was found</returns>
        public static BattlerTemplate GetBattlerTemplate(string battlerTemplateName)
        {
            return (BattlerTemplate)Get(battlerTemplateName, "Battler Template");
        }
        
        //TODO: make a cache for this or something
        /// <summary>
        /// Gets something from the registry using the name you provide
        /// </summary>
        /// <param name="name">The name of what you want to get</param>
        /// <param name="fileToSearch">The name of the file inside 'Resources/Pokemon Game' that contains the type of scriptable object you are looking for</param>
        /// <returns></returns>
        public static ScriptableObject Get(string name, string fileToSearch)
        {
            ScriptableObject[] objs = Resources.LoadAll<ScriptableObject>($"Pokemon Game/{fileToSearch}");
            foreach (var obj in objs)
            {
                if (obj.name.ToLower() == name.ToLower())
                {
                    return obj;
                }
            }
    
            Debug.LogWarning($"Could not find {fileToSearch}: {name}, returning null");
            return null;
        }
    }
}