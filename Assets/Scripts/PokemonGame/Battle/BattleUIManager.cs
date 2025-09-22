using System;
using System.Collections.Generic;
using PokemonGame.Game.Party;
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
        [SerializeField] private GameObject itemButton;
        [SerializeField] private TextMeshProUGUI[] battlerDisplays;
        [SerializeField] private TextMeshProUGUI[] itemBattlerDisplays;
        [SerializeField] private SpriteRenderer playerOneRenderer;
        [SerializeField] private SpriteRenderer playerTwoBattlerRenderer;
        [SerializeField] private Image playerOneHealthDisplay;
        [SerializeField] private RectTransform playerOneSideBorder;
        [SerializeField] private RectTransform playerOneSideBorderLimitLeft;
        [SerializeField] private RectTransform playerOneSideBorderLimitRight;
        [SerializeField] private Image playerTwoHealthDisplay;
        [SerializeField] private RectTransform playerTwoSideBorder;
        [SerializeField] private RectTransform playerTwoSideBorderLimitLeft;
        [SerializeField] private RectTransform playerTwoSideBorderLimitRight;
        [SerializeField] private TextMeshProUGUI[] moveTexts;
        [SerializeField] private TextMeshProUGUI[] movePpTexts;
        [SerializeField] private TextMeshProUGUI playerOneNameDisplay;
        [SerializeField] private TextMeshProUGUI playerOneLevelDisplay;
        [SerializeField] private TextMeshProUGUI playerTwoBattlerNameDisplay;
        [SerializeField] private TextMeshProUGUI playerTwoBattlerLevelDisplay;
        [SerializeField] private BattleBagMenu battleBagMenu;
        [SerializeField] private float shrinkEffectSpeed = 1;
        [SerializeField] private Button swapBattlerButton;
        [SerializeField] private Button defaultItemButton;

        [SerializeField] private List<Item> faintedRequiredItems = new List<Item>();

        public Transform playerBattler => playerOneRenderer.transform;
        public Transform playerTwoBattler => playerTwoBattlerRenderer.transform;
        
        public Battle battle;
        public PlayerBattleController playerBattleController;

        private Item _playerItemToUse;

        private Vector3 _initialPlayerOneScale;
        private Vector3 _initialPlayerTwoScale;

        [SerializeField] private Vector3 targetPlayerOneBattlerScale;
        [SerializeField] private Vector3 targetPlayerTwoBattlerScale;

        private List<Button> moveButtonsButtons = new List<Button>();

        private void Awake()
        {
            EventSystem.current.SetSelectedGameObject(moveTexts[0].transform.parent.gameObject);
            
            _initialPlayerOneScale = playerOneRenderer.transform.localScale;
            _initialPlayerTwoScale = playerTwoBattlerRenderer.transform.localScale;
            
            targetPlayerOneBattlerScale = _initialPlayerOneScale;
            targetPlayerTwoBattlerScale = _initialPlayerTwoScale;
            
            playerOneRenderer.transform.localScale = Vector3.zero;
            playerTwoBattlerRenderer.transform.localScale = Vector3.zero;

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

            runButton.SetActive(!Battle.Singleton.trainerBattle && !Battle.Singleton.onlineBattle);
            itemButton.SetActive(!Battle.Singleton.onlineBattle);
        }

        private void Update()
        {
            UpdateHealthDisplays();

            playerOneRenderer.transform.localScale = Vector3.Lerp(playerOneRenderer.transform.localScale,
                targetPlayerOneBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
            // 1.45

            playerOneRenderer.transform.localPosition = Vector3.Lerp(new Vector3(0, 2.3f, 4.76f), new Vector3(0, 1.45f, 4.76f),
                1 - playerOneRenderer.transform.localScale.x / 3f);

            playerTwoBattlerRenderer.transform.localScale = Vector3.Lerp(playerTwoBattlerRenderer.transform.localScale,
                targetPlayerTwoBattlerScale, shrinkEffectSpeed * Time.deltaTime);
            
            playerTwoBattlerRenderer.transform.localPosition = Vector3.Lerp(new Vector3(0, 2.3f, -4.76f), new Vector3(0, 1.45f, -4.76f),
                1 - playerTwoBattlerRenderer.transform.localScale.x / 3f);
        }

        private void UpdateBattlerButtons()
        {
            UpdateItemBattlerButtons();
            
            foreach (var text in battlerDisplays)
            {
                text.transform.parent.gameObject.SetActive(false);
            }

            BattleParty party = battle.localPlayerOne ? battle.partyOne : battle.partyTwo;

            for (int i = 0; i < party.Count; i++)
            {
                Button buttonParent = battlerDisplays[i].transform.parent.GetComponent<Button>();
                
                if (!party[i])
                {
                    buttonParent.gameObject.SetActive(false);
                }
                else
                {
                    buttonParent.gameObject.SetActive(true);
                    battlerDisplays[i].text = party[i].name;
                    // battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = !battle.partyOne[i].isFainted;
                    battlerDisplays[i].color = Color.black;
                    Color newColour = buttonParent.targetGraphic.color;
                    newColour.a = 1;

                    int currentBattlerIndex = battle.localPlayerOne
                        ? battle.playerOneBattlerIndex
                        : battle.playerTwoBattlerIndex;

                    if (i == currentBattlerIndex)
                    {
                        battlerDisplays[i].text = party[i].name + " is selected";
                        // battlerDisplays[i].transform.parent.GetComponent<Button>().interactable = false;
                        battlerDisplays[i].color = Color.blue;
                        newColour.a = .5f;
                    }

                    if (party[i].isFainted)
                    {
                        battlerDisplays[i].text = party[i].name + " is fainted";
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

            for (int i = 0; i < battle.partyOne.Count; i++)
            {
                if (!battle.partyOne[i])
                {
                    itemBattlerDisplays[i].transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    total++;
                    itemBattlerDisplays[i].transform.parent.gameObject.SetActive(true);
                    itemBattlerDisplays[i].text = battle.partyOne[i].name;
                    itemBattlerDisplays[i].transform.parent.GetComponent<Button>().interactable = !battle.partyOne[i].isFainted;

                    if (_playerItemToUse)
                    {
                        if (faintedRequiredItems.Contains(_playerItemToUse))
                        {
                            itemBattlerDisplays[i].transform.parent.GetComponent<Button>().interactable =
                                battle.partyOne[i].isFainted;
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

        public void ShrinkPlayerOneBattler(bool instant = false)
        {
            targetPlayerOneBattlerScale = Vector3.zero;
            if (instant)
            {
                playerOneRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandPlayerOneBattler()
        {
            targetPlayerOneBattlerScale = _initialPlayerOneScale;
        }

        public void ShrinkPlayerTwoBattler(bool instant = false)
        {
            targetPlayerTwoBattlerScale = Vector3.zero;
            if (instant)
            {
                playerTwoBattlerRenderer.transform.localScale = Vector3.zero;
            }
        }

        public void ExpandPlayerTwoBattler()
        {
            targetPlayerTwoBattlerScale = _initialPlayerTwoScale;
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

        public void ChangeBattler(int battlerID)
        {
            if (battle.localPlayerOne)
            {
                if (battle.partyOne[battlerID].isFainted || battlerID == battle.playerOneBattlerIndex)
                {
                    return;
                }
            }
            else
            {
                if (battle.partyTwo[battlerID].isFainted || battlerID == battle.playerTwoBattlerIndex)
                {
                    return;
                }
            }
            
            // playerUIHolder.SetActive(false);
            healthDisplays.SetActive(true);
            controlUIHolder.SetActive(false);
            changeBattlerDisplay.SetActive(false);
            useItemOnBattlerDisplay.SetActive(false);
            useItemDisplay.SetActive(false);
            
            Back();
            playerBattleController.PlayerChooseToSwap(battlerID);
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
            
            playerBattleController.PlayerUseItem(_playerItemToUse, partyID, true);
            
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
            if (battle.localPlayerOne)
            {
                playerOneNameDisplay.text = battle.PlayerOneBattler.name;
                playerOneLevelDisplay.text = "Lv. " + battle.PlayerOneBattler.level;
                playerTwoBattlerNameDisplay.text = battle.PlayerTwoBattler.name;
                playerTwoBattlerLevelDisplay.text = "Lv. " + battle.PlayerTwoBattler.level;
            }
            else
            {
                playerOneNameDisplay.text = battle.PlayerTwoBattler.name;
                playerOneLevelDisplay.text = "Lv. " + battle.PlayerTwoBattler.level;
                playerTwoBattlerNameDisplay.text = battle.PlayerOneBattler.name;
                playerTwoBattlerLevelDisplay.text = "Lv. " + battle.PlayerOneBattler.level;
            }
        }

        private void UpdateBattlerSprites()
        {
            playerOneRenderer.sprite = battle.partyOne[battle.currentDisplayBattlerIndex].GetSpriteFront();
            playerTwoBattlerRenderer.sprite = battle.partyTwo[battle.playerTwoDisplayBattlerIndex].GetSpriteFront();
        }

        public void UpdatePlayerOneBattlerDetails()
        {
            UpdateBattlerButtons();
            UpdateBattlerSprites();
            UpdateBattlerMoveDisplays();
            UpdateBattlerTexts();
        }

        public void ForceHealthSet()
        {
            float t = healthUpdateSpeed;

            float playerTwoTarget = battle.PlayerTwoBattler.currentHealth /
                                   (float)battle.PlayerTwoBattler.stats.maxHealth;

            float playerTarget = battle.PlayerOneBattler.currentHealth /
                                 (float)battle.PlayerOneBattler.stats.maxHealth;

            if (battle.localPlayerOne)
            {
                playerTwoHealthDisplay.transform.localScale = new Vector3(playerTwoTarget, 1, 1);
                playerTwoSideBorder.position = Vector3.Lerp(playerTwoSideBorderLimitRight.position,
                    playerTwoSideBorderLimitLeft.position, playerTwoTarget);

                playerOneHealthDisplay.transform.localScale = new Vector3(playerTarget, 1, 1);
                playerOneSideBorder.position = Vector3.Lerp(playerOneSideBorderLimitLeft.position,
                    playerOneSideBorderLimitRight.position, playerTarget);
            }
            else
            {
                playerTwoHealthDisplay.transform.localScale = new Vector3(playerTarget, 1, 1);
                playerTwoSideBorder.position = Vector3.Lerp(playerTwoSideBorderLimitRight.position,
                    playerTwoSideBorderLimitLeft.position, playerTarget);

                playerOneHealthDisplay.transform.localScale = new Vector3(playerTwoTarget, 1, 1);
                playerOneSideBorder.position = Vector3.Lerp(playerOneSideBorderLimitLeft.position,
                    playerOneSideBorderLimitRight.position, playerTwoTarget);
            }
        }

        public void UpdatePlayerTwoBattlerDetails()
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
            
            List<Move> moves = battle.localPlayerOne ? battle.PlayerOneBattler.moves : battle.PlayerTwoBattler.moves;
            List<MovePPData> movePpInfos = battle.localPlayerOne ? battle.PlayerOneBattler.movePpInfos : battle.PlayerTwoBattler.movePpInfos;
            
            for (var i = 0; i < moves.Count; i++)
            {
                if (moves[i])
                {
                    int currentPP = movePpInfos[i].CurrentPP;
                    int maxPP = movePpInfos[i].MaxPP;
                    
                    moveTexts[i].transform.parent.gameObject.SetActive(true);
                    moveTexts[i].text = moves[i].name;
                    moveTexts[i].transform.parent.GetComponent<Image>().color =
                        moves[i].type.color;
                    movePpTexts[i].text = $"{currentPP}/{maxPP}";
                    if (currentPP <= 0)
                    {
                        moveButtonsButtons[i].interactable = false;
                    }
                    
                    moveTexts[i].transform.parent.GetChild(0).GetComponentInChildren<Image>().sprite = moves[i].type.sprite;
                }
            }
            
            SetMoveButtonUINavigations();
        }

        private void SetMoveButtonUINavigations()
        {
            List<Move> moves = battle.localPlayerOne ? battle.PlayerOneBattler.moves : battle.PlayerTwoBattler.moves;
            
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

            float playerTwoTarget = battle.PlayerTwoBattler.currentHealth /
                                   (float)battle.PlayerTwoBattler.stats.maxHealth;
            float playerTwoCurrent = playerTwoHealthDisplay.transform.localScale.x;
            
            float playerTwoDifference = playerTwoCurrent - playerTwoTarget;
            float absDifference = playerTwoDifference > 0 ? playerTwoDifference : -playerTwoDifference;

            if (absDifference < t)
            {
                playerTwoCurrent = playerTwoTarget;
            }
            else
            {
                if (playerTwoDifference > 0)
                {
                    // need to decrease
                    playerTwoCurrent -= t;
                }
                else
                {
                    // need to increase
                    playerTwoCurrent += t;
                }
            }

            float playerTarget = battle.PlayerOneBattler.currentHealth /
                                 (float)battle.PlayerOneBattler.stats.maxHealth;
            float playerCurrent = playerOneHealthDisplay.transform.localScale.x;
            
            float playerDifference = playerCurrent - playerTarget;
            float absPlayerOneDifference = playerDifference > 0 ? playerDifference : -playerDifference;
            
            if (absPlayerOneDifference < t)
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

            if (battle.localPlayerOne)
            {
                playerTwoHealthDisplay.transform.localScale = new Vector3(playerTwoCurrent, 1, 1);
                playerTwoSideBorder.position = Vector3.Lerp(playerTwoSideBorderLimitRight.position,
                    playerTwoSideBorderLimitLeft.position, playerTwoCurrent);

                playerOneHealthDisplay.transform.localScale = new Vector3(playerCurrent, 1, 1);
                playerOneSideBorder.position = Vector3.Lerp(playerOneSideBorderLimitLeft.position,
                    playerOneSideBorderLimitRight.position, playerCurrent);
            }
            else
            {
                playerTwoHealthDisplay.transform.localScale = new Vector3(playerCurrent, 1, 1);
                playerTwoSideBorder.position = Vector3.Lerp(playerTwoSideBorderLimitRight.position,
                    playerTwoSideBorderLimitLeft.position, playerCurrent);

                playerOneHealthDisplay.transform.localScale = new Vector3(playerTwoCurrent, 1, 1);
                playerOneSideBorder.position = Vector3.Lerp(playerOneSideBorderLimitLeft.position,
                    playerOneSideBorderLimitRight.position, playerTwoCurrent);
            }
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