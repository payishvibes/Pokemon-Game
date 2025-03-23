using System.Collections.Generic;
using PokemonGame.Dialogue;
using PokemonGame.Game;
using PokemonGame.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokemonGame.NPC
{
    public class PokeMartNPC : NPC
    {
        [Space]
        [Header("Buy")]
        [SerializeField] private GameObject buyUI;
        [SerializeField] private GameObject selectAmountToBuyUI;
        [SerializeField] private Transform buttonHolder;
        [SerializeField] private TextMeshProUGUI amountDisplay;
        [SerializeField] private TextMeshProUGUI moneyDisplay;
        [SerializeField] private Button backToBuyButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private TextAsset confirmationDialogue;
        
        [Space]
        [Header("Sell")]
        [SerializeField] private GameObject sellUI;
        [SerializeField] private PlayerMovement player;
        
        [Space]
        [Header("Other stuff")]
        [SerializeField] private ItemDisplay itemDisplay;

        [SerializeField] private List<Item> stock;

        private int _currentlySelectedItem;

        private int _amountToBuy;
        
        [SerializeField] private TextAsset startPokeMartDialogue;

        private int _choice;

        private bool _waitingToGoBackToBuy = false;
        private bool _waitingForBuyConfirmation = false;

        protected override void OnPlayerInteracted()
        {
            QueDialogue(startPokeMartDialogue);
            base.OnPlayerInteracted();
        }

        protected override void OverrideOnEnable()
        {
            DialogueManager.instance.DialogueChoice += OnDialogueChoice;
            DialogueManager.instance.DialogueEnded += OnDialogueEnded;
        }

        private void FixedUpdate()
        {
            amountDisplay.text = _amountToBuy.ToString();
            moneyDisplay.text = '$'+Bag.GetCurrentMoney().ToString();
        }

        private void OnDialogueEnded(object sender, DialogueEndedEventArgs e)
        {
            if (_waitingToGoBackToBuy)
            {
                backToBuyButton.interactable = true;
                buyButton.interactable = true;
                _waitingToGoBackToBuy = false;
            }
            else if (_waitingForBuyConfirmation)
            {
                if (_choice == 0)
                {
                    // yes
                    Bag.SpentMoney(stock[_currentlySelectedItem].cost * _amountToBuy);
                    Bag.Add(stock[_currentlySelectedItem], _amountToBuy);
                    BackToBuy();
                }
                else
                {
                    // no
                    BackToBuy();
                }

                _waitingForBuyConfirmation = false;
            }
            else if (e.trigger == this && _choice != -1)
            {
                if (_choice == 0)
                {
                    // Shop
                    OpenBuyMenu();
                }
                else if (_choice == 1)
                {
                    // Sell
                    OpenSellMenu();
                }
            }
            _choice = -1;
        }

        private void OpenBuyMenu()
        {
            Debug.Log("We want to shop");
            buyUI.SetActive(true);
            sellUI.SetActive(false);
            selectAmountToBuyUI.SetActive(false);
            RefreshBuyMenuItems();
            buttonHolder.parent.parent.GetComponent<ScrollRect>().enabled = true;
            if (buttonHolder.childCount > 0)
            {
                for (int i = 0; i < buttonHolder.childCount; i++)
                {
                    buttonHolder.GetChild(i).GetComponentInChildren<Button>().interactable = true;
                }
            }
            buttonHolder.parent.parent.GetComponent<ScrollRect>().enabled = true;
        }

        private void RefreshBuyMenuItems()
        {
            if (buttonHolder.childCount > 0)
            {
                for (int i = 0; i < buttonHolder.childCount; i++)
                {
                    Destroy(buttonHolder.GetChild(i).gameObject);
                }
            }

            for (int i = 0; i < stock.Count; i++)
            {
                int index = i;
                
                ItemDisplay display = Instantiate(itemDisplay, buttonHolder);

                display.NameText.text = $"{stock[i].name}: ${stock[i].cost}";
                display.DescriptionText.text = stock[i].description;
                display.TextureImage.sprite = stock[i].sprite;
                display.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    SelectItemToBuy(index);
                });
                
            }
        }

        public void SelectItemToBuy(int index)
        {
            _currentlySelectedItem = index;
            selectAmountToBuyUI.SetActive(true);

            _amountToBuy = 1;
            
            if (buttonHolder.childCount > 0)
            {
                for (int i = 0; i < buttonHolder.childCount; i++)
                {
                    buttonHolder.GetChild(i).GetComponentInChildren<Button>().interactable = false;
                }
            }
            buttonHolder.parent.parent.GetComponent<ScrollRect>().enabled = false;
        }

        public void AddToAmount(int amount)
        {
            _amountToBuy += amount;

            if (_amountToBuy > 99)
            {
                _amountToBuy = 0;
            }

            if (_amountToBuy <= 0)
            {
                _amountToBuy = 99;
            }
        }

        public void Buy()
        {
            int totalCost = stock[_currentlySelectedItem].cost * _amountToBuy;

            if (Bag.CanAfford(totalCost))
            {
                _waitingForBuyConfirmation = true;

                Dictionary<string, string> variables = new Dictionary<string, string>();
                variables.Add("amountToBuy", _amountToBuy.ToString());
                variables.Add("itemToBuy", stock[_currentlySelectedItem].name);
                variables.Add("totalCost", totalCost.ToString());
                
                QueDialogue(confirmationDialogue, "buyConfirm", true, variables);
            }
            else
            {
                QueDialogue("Not enough money to buy this");
                backToBuyButton.interactable = false;
                buyButton.interactable = false;
                _waitingToGoBackToBuy = true;
            }
        }

        private void OpenSellMenu()
        {
            Debug.Log("We want to sell");
            sellUI.SetActive(true);
            buyUI.SetActive(false);
            selectAmountToBuyUI.SetActive(false);
        }

        protected override void OverrideOnDisable()
        {
            DialogueManager.instance.DialogueChoice -= OnDialogueChoice;
        }

        private void OnDialogueChoice(object sender, DialogueChoiceEventArgs e)
        {
            if (e.trigger == this)
            {
                _choice = e.choiceIndex;
            }
        }

        public void BackToBuy()
        {
            OpenBuyMenu();
        }

        public void CloseShop()
        {
            buyUI.SetActive(false);
            sellUI.SetActive(false);
            selectAmountToBuyUI.SetActive(false);
            buttonHolder.parent.parent.GetComponent<ScrollRect>().enabled = true;
            QueDialogue("Thank you for visiting!");
            Player.interacting = false;
        }
    }
}