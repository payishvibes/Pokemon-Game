using System;
using System.Collections.Generic;
using PokemonGame.ScriptableObjects;

namespace PokemonGame.Battle
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;   

    public class BattleUIManager : MonoBehaviour
    {
        [SerializeField] private float healthUpdateSpeed = 0.8f;

        [Space] 
        [SerializeField] private LevelUpDisplay levelUpDisplay;
        [SerializeField] private GameObject playerUIHolder;
        [SerializeField] private GameObject controlUIHolder;
        [SerializeField] private GameObject moveButtons;
        [SerializeField] private GameObject healthDisplays;
        [SerializeField] private GameObject changeBattlerDisplay;
        [SerializeField] private GameObject useItemOnBattlerDisplay;
        [SerializeField] private GameObject useItemDisplay;
        [SerializeField] private GameObject miscButtons;
        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject runButton;
        [SerializeField] private TextMeshProUGUI[] battlerDisplays;
        [SerializeField] private TextMeshProUGUI[] itemBattlerDisplays;
        [SerializeField] private SpriteRenderer currentBattlerRenderer;
        [SerializeField] private SpriteRenderer opponentBattlerRenderer;
        [SerializeField] private Slider currentBattlerHealthDisplay;
        [SerializeField] private Slider opponentHealthDisplay;
        [SerializeField] private TextMeshProUGUI[] moveTexts;
        [SerializeField] private TextMeshProUGUI[] movePpTexts;
        [SerializeField] private TextMeshProUGUI currentBattlerNameDisplay;
        [SerializeField] private TextMeshProUGUI opponentBattlerNameDisplay;
        [SerializeField] private BattleBagMenu battleBagMenu;
        [SerializeField] private float shrinkEffectSpeed = 1;

        [SerializeField] private List<Item> faintedRequiredItems = new List<Item>();

        public Transform playerBattler => currentBattlerRenderer.transform;
        public Transform opponentBattler => opponentBattlerRenderer.transform;
        
        public Battle battle;

        private Item _playerItemToUse;

        private Vector3 _initialPlayerScale;
        private Vector3 _initialOpponentScale;

        [SerializeField] private Vector3 _targetPlayerBattlerScale;
        [SerializeField] private Vector3 _targetOpponentBattlerScale;

        private void Awake()
        {
            _initialPlayerScale = currentBattlerRenderer.transform.localScale;
            _initialOpponentScale = opponentBattlerRenderer.transform.localScale;
            
            _targetPlayerBattlerScale = _initialPlayerScale;
            _targetOpponentBattlerScale = _initialOpponentScale;
            
            currentBattlerRenderer.transform.localScale = Vector3.zero;
            opponentBattlerRenderer.transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            UpdateBattlerSprites();
            UpdateBattlerMoveDisplays();
            UpdateBattlerTexts();
            UpdateBattlerButtons();
            
            opponentHealthDisplay.maxValue = battle.opponentParty[battle.opponentBattlerIndex].stats.maxHealth;
            opponentHealthDisplay.value = battle.opponentParty[battle.opponentBattlerIndex].currentHealth;

            currentBattlerHealthDisplay.maxValue = battle.playerParty[battle.currentBattlerIndex].stats.maxHealth;
            currentBattlerHealthDisplay.value = battle.playerParty[battle.currentBattlerIndex].currentHealth;

            runButton.SetActive(!Battle.Singleton.trainerBattle);
        }

        private void Update()
        {
            UpdateHealthDisplays();

            currentBattlerRenderer.transform.localScale = Vector3.Lerp(currentBattlerRenderer.transform.localScale,
                _targetPlayerBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
            // 1.45

            currentBattlerRenderer.transform.localPosition = Vector3.Lerp(new Vector3(0, 2.3f, 4.76f), new Vector3(0, 1.45f, 4.76f),
                1 - currentBattlerRenderer.transform.localScale.x / 3f);

            opponentBattlerRenderer.transform.localScale = Vector3.Lerp(opponentBattlerRenderer.transform.localScale,
                _targetOpponentBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
            opponentBattlerRenderer.transform.localPosition = Vector3.Lerp(new Vector3(0, 2.3f, -4.76f), new Vector3(0, 1.45f, -4.76f),
                1 - opponentBattlerRenderer.transform.localScale.x / 3f);
        }

        private void UpdateBattlerButtons()
        {
            UpdateItemBattlerButtons();
            
            foreach (var text in battlerDisplays)
            {
                text.transform.parent.gameObject.SetActive(false);
            }

            for (int i = 0; i < battle.playerParty.Count; i++)
            {
                if (!battle.playerParty[i])
                {
                    battlerDisplays[i].transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    battlerDisplays[i].transform.parent.gameObject.SetActive(true);
                    battlerDisplays[i].text = battle.playerParty[i].name;
                    battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = !battle.playerParty[i].isFainted;
                    battlerDisplays[i].color = Color.black;

                    if (i == battle.currentBattlerIndex)
                    {
                        battlerDisplays[i].text = battle.playerParty[i].name + " is selected";
                        battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = false;
                        battlerDisplays[i].color = Color.blue;
                    }

                    if (battle.playerParty[i].isFainted)
                    {
                        battlerDisplays[i].text = battle.playerParty[i].name + " is fainted";
                        battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = false;
                        battlerDisplays[i].color = Color.red;
                    }
                }
            }
        }

        public void UpdateItemBattlerButtons()
        {
            foreach (var text in itemBattlerDisplays)
            {
                text.transform.parent.gameObject.SetActive(false);
            }

            for (int i = 0; i < battle.playerParty.Count; i++)
            {
                if (!battle.playerParty[i])
                {
                    itemBattlerDisplays[i].transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    itemBattlerDisplays[i].transform.parent.gameObject.SetActive(true);
                    itemBattlerDisplays[i].text = battle.playerParty[i].name;
                    itemBattlerDisplays[i].transform.parent.GetComponent<Button>().interactable = !battle.playerParty[i].isFainted;

                    if (_playerItemToUse)
                    {
                        if (faintedRequiredItems.Contains(_playerItemToUse))
                        {
                            itemBattlerDisplays[i].transform.parent.GetComponent<Button>().interactable =
                                battle.playerParty[i].isFainted;
                        }
                    }
                    
                    itemBattlerDisplays[i].color = Color.black;
                }
            }
        }

        public void ShowUI(bool show)
        {
            playerUIHolder.SetActive(show);
            ShowControlUI(show);
            healthDisplays.SetActive(show);
        }

        public void ShowHealthUI(bool show)
        {
            healthDisplays.SetActive(show);
        }

        public void ShrinkPlayerBattler(bool instant = false)
        {
            _targetPlayerBattlerScale = Vector3.zero;
            if (instant)
            {
                currentBattlerRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandPlayerBattler()
        {
            _targetPlayerBattlerScale = _initialPlayerScale;
        }

        public void ShrinkOpponentBattler(bool instant = false)
        {
            _targetOpponentBattlerScale = Vector3.zero;
            if (instant)
            {
                opponentBattlerRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandOpponentBattler()
        {
            _targetOpponentBattlerScale = _initialOpponentScale;
        }

        public void ShowControlUI(bool show)
        {
            controlUIHolder.SetActive(show);
        }

        public void SwitchBattler()
        {
            playerUIHolder.SetActive(true);
            healthDisplays.SetActive(false);
            moveButtons.SetActive(false);
            miscButtons.SetActive(false);
            changeBattlerDisplay.SetActive(true);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            backButton.SetActive(true);
            UpdateBattlerButtons();
        }

        public void OpenUseItemOnBattler(Item itemToUse)
        {
            playerUIHolder.SetActive(true);
            healthDisplays.SetActive(false);
            moveButtons.SetActive(false);
            miscButtons.SetActive(false);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(true);
            useItemDisplay.SetActive(false);
            backButton.SetActive(true);
            UpdateBattlerButtons();
            _playerItemToUse = itemToUse;
        }

        public void UseItem()
        {
            playerUIHolder.SetActive(true);
            healthDisplays.SetActive(false);
            moveButtons.SetActive(false);
            miscButtons.SetActive(false);
            useItemDisplay.SetActive(true);
            backButton.SetActive(true);
            UpdateBattlerButtons();
            battleBagMenu.UpdateBagUI();
        }

        public void SwitchBattlerBecauseOfDeath()
        {
            playerUIHolder.SetActive(true);
            controlUIHolder.SetActive(true);
            healthDisplays.SetActive(false);
            moveButtons.SetActive(false);
            miscButtons.SetActive(false);
            changeBattlerDisplay.SetActive(true);
            useItemOnBattlerDisplay.SetActive(false);
            backButton.SetActive(false);
            useItemDisplay.SetActive(false);
            UpdateBattlerButtons();
        }

        public void ChangeBattler(int partyID)
        {
            playerUIHolder.SetActive(false);
            healthDisplays.SetActive(true);
            moveButtons.SetActive(true);
            miscButtons.SetActive(true);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            
            battle.ChooseToSwap(partyID);
            
            battle.AddParticipatedBattler(battle.playerParty[partyID]);
            Back();
        }

        public void UseItemOnBattler(int partyID)
        {
            playerUIHolder.SetActive(true);
            healthDisplays.SetActive(true);
            controlUIHolder.SetActive(false);
            moveButtons.SetActive(true);
            miscButtons.SetActive(true);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            
            battle.UseItem(partyID, true);
            
            battle.AddParticipatedBattler(battle.playerParty[partyID]);
            Back();
        }

        public void Back()
        {
            healthDisplays.SetActive(true);
            moveButtons.SetActive(true);
            miscButtons.SetActive(true);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            UpdateBattlerButtons();
        }

        private void UpdateBattlerTexts()
        {
            currentBattlerNameDisplay.text = battle.playerParty[battle.currentBattlerIndex].name;
            opponentBattlerNameDisplay.text = battle.opponentParty[battle.opponentBattlerIndex].name;
            
            currentBattlerNameDisplay.color = Color.white;
            if (battle.playerParty[battle.currentBattlerIndex].isFainted)
            {
                currentBattlerNameDisplay.text = battle.playerParty[battle.currentBattlerIndex].name + " (FAINTED)";
                currentBattlerNameDisplay.color = Color.red;
            }
            
            opponentBattlerNameDisplay.color = Color.white;
            if (battle.opponentParty[battle.opponentBattlerIndex].isFainted)
            {
                opponentBattlerNameDisplay.text = battle.opponentParty[battle.opponentBattlerIndex].name + " (FAINTED)";
                opponentBattlerNameDisplay.color = Color.red;
            }
        }

        private void UpdateBattlerSprites()
        {
            currentBattlerRenderer.sprite = battle.playerParty[battle.currentDisplayBattlerIndex].GetSpriteFront();
            opponentBattlerRenderer.sprite = battle.opponentParty[battle.opponentDisplayBattlerIndex].GetSpriteFront();
        }

        public void UpdatePlayerBattlerDetails()
        {
            UpdateBattlerButtons();
            UpdateBattlerSprites();
            UpdateBattlerMoveDisplays();
            UpdateBattlerTexts();
        }

        public void UpdateOpponentBattlerDetails()
        {
            UpdateBattlerSprites();
            UpdateBattlerTexts();
        }

        public void UpdateBattlerMoveDisplays()
        {
            foreach (var text in moveTexts)
            {
                text.transform.parent.gameObject.SetActive(false);
            }
            
            for (var i = 0; i < battle.playerParty[battle.currentBattlerIndex].moves.Count; i++)
            {
                if (battle.playerParty[battle.currentBattlerIndex].moves[i])
                {
                    int currentPP = battle.playerParty[battle.currentBattlerIndex].movePpInfos[i].CurrentPP;
                    int maxPP = battle.playerParty[battle.currentBattlerIndex].movePpInfos[i].MaxPP;
                    
                    moveTexts[i].transform.parent.gameObject.SetActive(true);
                    moveTexts[i].text = battle.playerParty[battle.currentBattlerIndex].moves[i].name;
                    movePpTexts[i].text = $"{currentPP}/{maxPP}";
                    if (currentPP <= 0)
                    {
                        moveTexts[i].transform.parent.GetComponent<Button>().interactable = false;
                    }
                }
            }
        }

        private void UpdateHealthDisplays()
        {
            float t = healthUpdateSpeed;
            
            opponentHealthDisplay.maxValue = battle.opponentParty[battle.opponentBattlerIndex].stats.maxHealth;
            opponentHealthDisplay.value = Mathf.Lerp(opponentHealthDisplay.value, battle.opponentParty[battle.opponentBattlerIndex].currentHealth, t);

            currentBattlerHealthDisplay.maxValue = battle.playerParty[battle.currentBattlerIndex].stats.maxHealth;
            currentBattlerHealthDisplay.value = Mathf.Lerp(currentBattlerHealthDisplay.value, battle.playerParty[battle.currentBattlerIndex].currentHealth, t);
        }
    }
}