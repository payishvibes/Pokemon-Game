using System;
using PokemonGame.General;
using UnityEngine.Events;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;
    
    [CreateAssetMenu(fileName = "New Item", menuName = "Pokemon Game/New Item")]
    public class Item : ScriptableObject
    {
        public new string name;
        public Sprite sprite;
        public ItemType type;
        [TextArea] public string description;
        public int cost;
        public bool lockedTarget;
        [ConditionalHide("lockedTarget", 1)] public bool playerParty;
        [ConditionalHide("lockedTarget", 1)] public int targetIndex;
    
        public UnityEvent<ItemMethodEventArgs> ItemMethodEvent;

        public void ItemMethod(ItemMethodEventArgs e)
        {
            ItemMethodEvent?.Invoke(e);
        }
    }
    
    /// <summary>
    /// Arguments that can be given to a MoveMethod to give it additional information
    /// </summary>
    public class ItemMethodEventArgs : EventArgs
    {
        public Battler target;
        public Item item;
        public bool success;

        public ItemMethodEventArgs(Battler target, Item item)
        {
            this.target = target;
            this.item = item;
        }
    }

    public enum ItemType : int
    {
        Items,
        Medicine,
        TM,
        Berries,
        KeyItems,
        ZCrystals,
        PokeBalls
    }
}