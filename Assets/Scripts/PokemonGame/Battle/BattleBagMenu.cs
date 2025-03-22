using System.Collections.Generic;
using PokemonGame.Game;
using PokemonGame.ScriptableObjects;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace PokemonGame.Battle
{
    public class BattleBagMenu : MonoBehaviour
    {
        [SerializeField] private GameObject itemDisplayHolder;
        [SerializeField] private GameObject itemDisplayGameObject;
        [SerializeField] private float useButtonXOffset;
        
        private ItemType _currentSortingType;

        private List<Item> _currentlyDisplayedItems = new List<Item>();
        
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

        public void UpdateBagUI()
        { 
            _currentlyDisplayedItems.Clear();
            
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

            for (int i = 0; i < sortedItems.Count; i++)
            {
                ItemDisplay display = Instantiate(itemDisplayGameObject, Vector3.zero, Quaternion.identity,
                    itemDisplayHolder.transform).GetComponent<ItemDisplay>();
                display.NameText.text = $"{sortedItems[i].item.name}";
                display.AmountText.text = $"x{sortedItems[i].amount}";
                display.TextureImage.sprite = sortedItems[i].item.sprite;
                display.DescriptionText.text = sortedItems[i].item.description;
                
                _currentlyDisplayedItems.Add(sortedItems[i].item);

                Button useButton = display.GetComponentInChildren<Button>();
                int index = i;
                
                if (Battle.Singleton.trainerBattle && sortedItems[i].item.type == ItemType.PokeBalls)
                {
                    useButton.interactable = false;
                }

                if (sortedItems[i].item.lockedTarget)
                {
                    if (sortedItems[i].item.type == ItemType.PokeBalls)
                    {
                        useButton.onClick.AddListener(() =>
                        {
                            Battle.Singleton.PlayerPickedPokeBall((PokeBall)sortedItems[index].item);
                        });
                    }
                    else
                    {
                        useButton.onClick.AddListener(() =>
                        {
                            Battle.Singleton.UseItem(sortedItems[index].item.targetIndex, sortedItems[index].item.playerParty);
                        });
                    }
                }
                else
                {
                    useButton.onClick.AddListener(() =>
                    {
                        Battle.Singleton.PlayerPickedItemToUse(_currentlyDisplayedItems[index]);
                        Battle.Singleton.StartPickingBattlerToUseItemOn();
                    });
                }
            }
        }
    }
}