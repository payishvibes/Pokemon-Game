using System.Collections.Generic;
using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.Game
{
    public class BagMenu : MonoBehaviour
    {
        [SerializeField] private GameObject itemDisplayHolder;
        [SerializeField] private GameObject itemDisplayGameObject;
        
        private ItemType _currentSortingType;
        
        private void Start()
        {
            UpdateBagUI();
        }
        
        /// <summary>
        /// Changes the item type that the player wants to look at
        /// </summary>
        /// <param name="newType">The index of the type you want to filter</param>
        public void ChangeCurrentSortingItem(int newType)
        {
            _currentSortingType = (ItemType)newType;
            UpdateBagUI();
        }

        private void UpdateBagUI()
        { 
            foreach (ItemDisplay child in itemDisplayHolder.transform.GetComponentsInChildren<ItemDisplay>()) {
                Destroy(child.gameObject);
            }
            
            List<BagItemData> sortedItems = new List<BagItemData>();
            foreach (BagItemData item in Bag.GetItems().Values)
            {
                if (item.item.type == _currentSortingType)
                {
                    sortedItems.Add(item);
                }
            }
            
            foreach (BagItemData itemToShow in sortedItems)
            {
                ItemDisplay display = Instantiate(itemDisplayGameObject, Vector3.zero, Quaternion.identity,
                    itemDisplayHolder.transform).GetComponent<ItemDisplay>();
                display.NameText.text = $"{itemToShow.item.name}";
                display.AmountText.text = $"x{itemToShow.amount}";
                display.TextureImage.sprite = itemToShow.item.sprite;
                display.DescriptionText.text = itemToShow.item.description;
            }
        }
    }
}


public class BagItemData
{
    public readonly Item item;
    public int amount;

    public BagItemData(Item item)
    {
        this.item = item;
        amount = 1;
    }
}