using System;
using System.Collections.Generic;
using System.Linq;
using PokemonGame.ScriptableObjects;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PokemonGame.Game
{
    public class BagMenu : MonoBehaviour
    {
        [SerializeField] private GameObject itemDisplayHolder;
        [SerializeField] private GameObject itemDisplayGameObject;
        [SerializeField] private GameObject defaultCategoryButton;
        [SerializeField] private List<GameObject> sortingButtons;
        
        public ItemType _currentSortingType;
        
        private void Start()
        {
            UpdateBagUI();
            EventSystem.current.SetSelectedGameObject(defaultCategoryButton);
        }

        private GameObject _lastSelectedUIObj;
        private ItemType _lastSelectedType;

        private void Update()
        {
            // just switched to a new obj
            if (_lastSelectedUIObj != EventSystem.current.currentSelectedGameObject)
            {
                // switched from a item button back to a category button
                if (!sortingButtons.Contains(_lastSelectedUIObj) &&
                    sortingButtons.Contains(EventSystem.current.currentSelectedGameObject))
                {
                    if (sortingButtons[(int)_lastSelectedType] != EventSystem.current.currentSelectedGameObject)
                    {
                        Debug.Log("wrong category");
                        // swaped back to the wrong one
                        EventSystem.current.SetSelectedGameObject(sortingButtons[(int)_lastSelectedType]);
                    }
                }
            }
            
            _lastSelectedUIObj = EventSystem.current.currentSelectedGameObject;
            _lastSelectedType = _currentSortingType;
        }

        /// <summary>
        /// Changes the item type that the player wants to look at
        /// </summary>
        /// <param name="newType">The index of the type you want to filter</param>
        public void ChangeCurrentSortingItem(int newType)
        {
            _currentSortingType = (ItemType)newType;
            Debug.Log("switching");
            UpdateBagUI();
        }

        public void SelectTopItem()
        {
            List<ItemDisplay> items = itemDisplayHolder.transform.GetComponentsInChildren<ItemDisplay>().ToList();
            
            if (items.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(items[0].gameObject);
            }
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

        private void OnEnable()
        {
            InputSystem.actions.FindAction("Escape").performed += OnEscapePressed;
        }

        private void OnEscapePressed(InputAction.CallbackContext obj)
        {
            if (!sortingButtons.Contains(EventSystem.current.currentSelectedGameObject))
            {
                // on item button
                EventSystem.current.SetSelectedGameObject(sortingButtons[(int)_lastSelectedType]);
            }
            else
            {
                OptionsMenu.instance.CloseCurrentMenu();
            }
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Escape").performed -= OnEscapePressed;
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