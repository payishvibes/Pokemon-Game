using System;
using System.Collections.Generic;
using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.Game
{
    public static class Bag
    {
        private static Dictionary<Item, BagItemData> _items = new Dictionary<Item, BagItemData>();
        public static int balance = 0;

        public static event EventHandler<BagGotItemEventArgs> GotItem;

        /// <summary>
        /// Adds an item to the bag
        /// </summary>
        /// <param name="itemToAdd">The item to be added</param>
        /// <param name="amount">Amount of items to add</param>
        public static void Add(Item itemToAdd, int amount)
        {
            if (itemToAdd != null)
            {
                for (int i = 0; i < amount; i++)
                {
                    bool wasFound = false;
                    foreach (BagItemData itemData in _items.Values)
                    {
                        if (itemData.item == itemToAdd)
                        {
                            itemData.amount++;
                            wasFound = true;
                        }
                    }

                    if (!wasFound)
                    {
                        _items.Add(itemToAdd, new BagItemData(itemToAdd));
                    }
                }

                GotItem?.Invoke(null, new BagGotItemEventArgs(itemToAdd, amount));
            }
            else
            {
                Debug.LogWarning("Failed to add item to bag, item was null");
            }
        }

        public static void Used(Item itemUsed)
        {
            if (_items.TryGetValue(itemUsed, out BagItemData value))
            {
                value.amount -= 1;
                if (value.amount <= 0)
                {
                    _items.Remove(itemUsed);
                }
            }
        }

        public static bool CanAfford(int potentialCost)
        {
            int endBalance = balance - potentialCost;

            return endBalance >= 0;
        }

        public static void SpentMoney(int spent)
        {
            balance -= spent;

            if (balance <= 0)
            {
                balance = 0;
            }
        }

        public static void GainMoney(int gainedAmount)
        {
            balance += gainedAmount;
        }

        public static int GetCurrentMoney()
        {
            return balance;
        }

        public static Dictionary<Item, BagItemData> GetItems()
        {
            return _items;
        }
    }

    public class BagGotItemEventArgs : EventArgs
    {
        public Item item;
        public int amount;

        public BagGotItemEventArgs(Item item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }
    }
}