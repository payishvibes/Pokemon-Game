using System;
using System.Collections.Generic;
using PokemonGame.ScriptableObjects;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PokemonGame.Battle
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;   

    public class BattleUIManager : MonoBehaviour
    {
        [SerializeField] private float healthUpdateSpeed = 0.8f;

        [Space]
        [SerializeField] private GameObject playerUIHolder;
        [SerializeField] private GameObject controlUIHolder;
        [SerializeField] private GameObject moveButtons;
        [SerializeField] private GameObject healthDisplays;
        [SerializeField] private GameObject changeBattlerDisplay;
        [SerializeField] private GameObject useItemOnBattlerDisplay;
        [SerializeField] private GameObject useItemDisplay;
        [SerializeField] private GameObject miscButtons;
        [SerializeField] private GameObject backButton;
        [SerializeField] private GameObject useItemBackButton;
        [SerializeField] private GameObject runButton;
        [SerializeField] private TextMeshProUGUI[] battlerDisplays;
        [SerializeField] private TextMeshProUGUI[] itemBattlerDisplays;
        [SerializeField] private SpriteRenderer currentBattlerRenderer;
        [SerializeField] private SpriteRenderer opponentBattlerRenderer;
        [SerializeField] private Image currentBattlerHealthDisplay;
        [SerializeField] private RectTransform currentBattlerSideBorder;
        [SerializeField] private RectTransform currentBattlerSideBorderLimitLeft;
        [SerializeField] private RectTransform currentBattlerSideBorderLimitRight;
        [SerializeField] private Image opponentHealthDisplay;
        [SerializeField] private RectTransform opponentSideBorder;
        [SerializeField] private RectTransform opponentSideBorderLimitLeft;
        [SerializeField] private RectTransform opponentSideBorderLimitRight;
        [SerializeField] private TextMeshProUGUI[] moveTexts;
        [SerializeField] private TextMeshProUGUI[] movePpTexts;
        [SerializeField] private TextMeshProUGUI currentBattlerNameDisplay;
        [SerializeField] private TextMeshProUGUI currentBattlerLevelDisplay;
        [SerializeField] private TextMeshProUGUI opponentBattlerNameDisplay;
        [SerializeField] private TextMeshProUGUI opponentBattlerLevelDisplay;
        [SerializeField] private BattleBagMenu battleBagMenu;
        [SerializeField] private float shrinkEffectSpeed = 1;
        [SerializeField] private Button swapBattlerButton;
        [SerializeField] private Button defaultItemButton;

        [SerializeField] private List<Item> faintedRequiredItems = new List<Item>();

        public Transform playerBattler => currentBattlerRenderer.transform;
        public Transform opponentBattler => opponentBattlerRenderer.transform;
        
        public Battle battle;

        private Item _playerItemToUse;

        private Vector3 _initialPlayerScale;
        private Vector3 _initialOpponentScale;

        [SerializeField] private Vector3 targetPlayerBattlerScale;
        [SerializeField] private Vector3 targetOpponentBattlerScale;

        private List<Button> moveButtonsButtons = new List<Button>();

        private void Awake()
        {
            EventSystem.current.SetSelectedGameObject(moveTexts[0].transform.parent.gameObject);
            
            _initialPlayerScale = currentBattlerRenderer.transform.localScale;
            _initialOpponentScale = opponentBattlerRenderer.transform.localScale;
            
            targetPlayerBattlerScale = _initialPlayerScale;
            targetOpponentBattlerScale = _initialOpponentScale;
            
            currentBattlerRenderer.transform.localScale = Vector3.zero;
            opponentBattlerRenderer.transform.localScale = Vector3.zero;

            foreach (var moveText in moveTexts)
            {
                moveButtonsButtons.Add(moveText.transform.parent.GetComponent<Button>());
            }
        }

        private void Start()
        {
            UpdateBattlerSprites();
            UpdateBattlerMoveDisplays();
            UpdateBattlerTexts();
            UpdateBattlerButtons();

            runButton.SetActive(!Battle.Singleton.trainerBattle);
        }

        private void Update()
        {
            UpdateHealthDisplays();

            currentBattlerRenderer.transform.localScale = Vector3.Lerp(currentBattlerRenderer.transform.localScale,
                targetPlayerBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
            // 1.45

            currentBattlerRenderer.transform.localPosition = Vector3.Lerp(new Vector3(0, 2.3f, 4.76f), new Vector3(0, 1.45f, 4.76f),
                1 - currentBattlerRenderer.transform.localScale.x / 3f);

            opponentBattlerRenderer.transform.localScale = Vector3.Lerp(opponentBattlerRenderer.transform.localScale,
                targetOpponentBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
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
                Button buttonParent = battlerDisplays[i].transform.parent.GetComponent<Button>();
                
                if (!battle.playerParty[i])
                {
                    buttonParent.gameObject.SetActive(false);
                }
                else
                {
                    buttonParent.gameObject.SetActive(true);
                    battlerDisplays[i].text = battle.playerParty[i].name;
                    // battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = !battle.playerParty[i].isFainted;
                    battlerDisplays[i].color = Color.black;
                    Color newColour = buttonParent.targetGraphic.color;
                    newColour.a = 1;

                    if (i == battle.currentBattlerIndex)
                    {
                        battlerDisplays[i].text = battle.playerParty[i].name + " is selected";
                        // battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = false;
                        battlerDisplays[i].color = Color.blue;
                        newColour.a = .5f;
                    }

                    if (battle.playerParty[i].isFainted)
                    {
                        battlerDisplays[i].text = battle.playerParty[i].name + " is fainted";
                        // battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = false;
                        battlerDisplays[i].color = Color.red;
                        newColour.a = .5f;
                    }
                    buttonParent.targetGraphic.color = newColour;
                    
                }
            }
        }

        public void UpdateItemBattlerButtons()
        {
            foreach (var text in itemBattlerDisplays)
            {
                text.transform.parent.gameObject.SetActive(false);
            }

            int unUsable = 0;
            int total = 0;

            for (int i = 0; i < battle.playerParty.Count; i++)
            {
                if (!battle.playerParty[i])
                {
                    itemBattlerDisplays[i].transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    total++;
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

                    if (!itemBattlerDisplays[i].transform.parent.GetComponent<Button>().interactable)
                    {
                        unUsable++;
                    }
                    
                    itemBattlerDisplays[i].color = Color.black;
                }
            }

            if (unUsable == total && useItemOnBattlerDisplay.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(useItemBackButton);
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
            targetPlayerBattlerScale = Vector3.zero;
            if (instant)
            {
                currentBattlerRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandPlayerBattler()
        {
            targetPlayerBattlerScale = _initialPlayerScale;
        }

        public void ShrinkOpponentBattler(bool instant = false)
        {
            targetOpponentBattlerScale = Vector3.zero;
            if (instant)
            {
                opponentBattlerRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandOpponentBattler()
        {
            targetOpponentBattlerScale = _initialOpponentScale;
        }

        public void ShowControlUI(bool show)
        {
            controlUIHolder.SetActive(show);
            if (show)
            {
                EventSystem.current.SetSelectedGameObject(moveTexts[0].transform.parent.gameObject);
            }
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
            EventSystem.current.SetSelectedGameObject(battlerDisplays[0].transform.parent.gameObject);
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
            EventSystem.current.SetSelectedGameObject(itemBattlerDisplays[0].transform.parent.gameObject);
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
            EventSystem.current.SetSelectedGameObject(defaultItemButton.gameObject);
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
            EventSystem.current.SetSelectedGameObject(battlerDisplays[0].transform.parent.gameObject);
        }

        public void ChangeBattler(int partyID)
        {
            if (battle.playerParty[partyID].isFainted || partyID == battle.currentBattlerIndex)
            {
                return;
            }
            
            playerUIHolder.SetActive(false);
            healthDisplays.SetActive(true);
            moveButtons.SetActive(true);
            miscButtons.SetActive(true);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            
            Back();
            battle.ChooseToSwap(partyID);
            
            battle.AddParticipatedBattler(battle.playerParty[partyID]);
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
            EventSystem.current.SetSelectedGameObject(moveTexts[0].transform.parent.gameObject);
            miscButtons.SetActive(true);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            UpdateBattlerButtons();
        }

        private void OnEscapePressed(InputAction.CallbackContext context)
        {
            if (!moveButtons.activeSelf)
            {
                Back();
            }
        }

        private void UpdateBattlerTexts()
        {
            currentBattlerNameDisplay.text = battle.playerParty[battle.currentBattlerIndex].name;
            currentBattlerLevelDisplay.text = "Lv. " + battle.playerParty[battle.currentBattlerIndex].level;
            opponentBattlerNameDisplay.text = battle.opponentParty[battle.opponentBattlerIndex].name;
            opponentBattlerLevelDisplay.text = "Lv. " + battle.opponentParty[battle.opponentBattlerIndex].level;
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

        public void ForceHealthSet()
        {
            float t = healthUpdateSpeed;

            float opponentTarget = battle.opponentParty[battle.opponentBattlerIndex].currentHealth /
                                   (float)battle.opponentParty[battle.opponentBattlerIndex].stats.maxHealth;

            float playerTarget = battle.playerParty[battle.currentBattlerIndex].currentHealth /
                                 (float)battle.playerParty[battle.currentBattlerIndex].stats.maxHealth;

            opponentHealthDisplay.transform.localScale = new Vector3(opponentTarget, 1, 1);
            opponentSideBorder.position = Vector3.Lerp(opponentSideBorderLimitRight.position,
                opponentSideBorderLimitLeft.position, opponentTarget);

            currentBattlerHealthDisplay.transform.localScale = new Vector3(playerTarget, 1, 1);
            currentBattlerSideBorder.position = Vector3.Lerp(currentBattlerSideBorderLimitLeft.position,
                currentBattlerSideBorderLimitRight.position, playerTarget);
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
                    moveTexts[i].transform.parent.GetComponent<Image>().color =
                        battle.playerParty[battle.currentBattlerIndex].moves[i].type.color;
                    movePpTexts[i].text = $"{currentPP}/{maxPP}";
                    if (currentPP <= 0)
                    {
                        moveButtonsButtons[i].interactable = false;
                    }
                    
                    moveTexts[i].transform.parent.GetChild(0).GetComponentInChildren<Image>().sprite = battle.playerParty[battle.currentBattlerIndex].moves[i].type.sprite;
                }
            }
            
            SetMoveButtonUINavigations();
        }

        private void SetMoveButtonUINavigations()
        {
            List<Move> moves = battle.playerParty[battle.currentBattlerIndex].moves;
            
            for (int i = 0; i < moves.Count; i++)
            {
                Button moveButton = moveButtonsButtons[i];

                Navigation nav = moveButton.navigation;

                // top
                if (i == 0)
                {
                    nav.selectOnUp = moveButtonsButtons[moves.Count - 1];
                }
                else
                {
                    nav.selectOnUp = moveButtonsButtons[i - 1];
                }

                // bottom
                if (i == moves.Count - 1)
                {
                    nav.selectOnDown = moveButtonsButtons[0];
                }
                else
                {
                    nav.selectOnDown = moveButtonsButtons[i + 1];
                }

                nav.selectOnRight = swapBattlerButton;

                moveButton.navigation = nav;
            }
        }

        private void UpdateHealthDisplays()
        {
            float t = healthUpdateSpeed * Time.deltaTime;

            float opponentTarget = battle.opponentParty[battle.opponentBattlerIndex].currentHealth /
                                   (float)battle.opponentParty[battle.opponentBattlerIndex].stats.maxHealth;
            float opponentCurrent = opponentHealthDisplay.transform.localScale.x;
            
            float opponentDifference = opponentCurrent - opponentTarget;
            float absDifference = opponentDifference > 0 ? opponentDifference : -opponentDifference;

            if (absDifference < t)
            {
                opponentCurrent = opponentTarget;
            }
            else
            {
                if (opponentDifference > 0)
                {
                    // need to decrease
                    opponentCurrent -= t;
                }
                else
                {
                    // need to increase
                    opponentCurrent += t;
                }
            }

            float playerTarget = battle.playerParty[battle.currentBattlerIndex].currentHealth /
                                 (float)battle.playerParty[battle.currentBattlerIndex].stats.maxHealth;
            float playerCurrent = currentBattlerHealthDisplay.transform.localScale.x;
            
            float playerDifference = playerCurrent - playerTarget;
            float absPlayerDifference = playerDifference > 0 ? playerDifference : -playerDifference;
            
            if (absPlayerDifference < t)
            {
                playerCurrent = playerTarget;
            }
            else
            {
                if (playerDifference > 0)
                {
                    // need to decrease
                    playerCurrent -= t;
                }
                else
                {
                    // need to increase
                    playerCurrent += t;
                }
            }

            opponentHealthDisplay.transform.localScale = new Vector3(opponentCurrent, 1, 1);
            opponentSideBorder.position = Vector3.Lerp(opponentSideBorderLimitRight.position,
                opponentSideBorderLimitLeft.position, opponentCurrent);

            currentBattlerHealthDisplay.transform.localScale = new Vector3(playerCurrent, 1, 1);
            currentBattlerSideBorder.position = Vector3.Lerp(currentBattlerSideBorderLimitLeft.position,
                currentBattlerSideBorderLimitRight.position, playerCurrent);
        }

        private void OnEnable()
        {
            InputSystem.actions.FindAction("Escape").performed += OnEscapePressed;
        }

        private void OnDisable()
        {
            InputSystem.actions.FindAction("Escape").performed -= OnEscapePressed;
        }
    }
}