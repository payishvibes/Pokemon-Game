using System;
using PokemonGame.Game;

namespace PokemonGame.Battle
{
    using System.Collections.Generic;
    using System.Collections;
    using Game.Party;
    using General;
    using Global;
    using ScriptableObjects;
    using UnityEngine;
    using Dialogue;

    public enum TurnStatus
    {
        Choosing,
        Showing,
        Ending
    }

    /// <summary>
    /// The main class that manages battles
    /// </summary>
    public class Battle : DialogueTrigger
    {
        private static Battle _singleton;
        public static Battle Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(Battle)} instance already exists, destroying duplicate!");
                }
            }
        }

        private void Awake()
        {
            Singleton = this;
        }

        [Header("UI:")]
        [SerializeField] private BattleUIManager uiManager;
        
        [Space]
        [Header("Assignments")] 
        [SerializeField] private TextAsset battlerUsedText;
        [SerializeField] private ParticleSystem spawnEffect;
        [SerializeField] private LevelUpDisplay levelUpDisplayPrefab;
        [SerializeField] private float shrinkEffectDelay;

        [Space]
        [Header("Readouts")]
        public int playerOneBattlerIndex;
        public int playerTwoBattlerIndex;
        [SerializeField] public TurnStatus currentTurn = TurnStatus.Choosing;
        public BattleParty partyOne;
        public BattleParty partyTwo;
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private Move playerOneMoveToDo;
        
        
        [HideInInspector] public int currentDisplayBattlerIndex;
        [HideInInspector] public int playerTwoDisplayBattlerIndex;
        
        /// <summary>
        /// the move the enemy intends to use
        /// </summary>
        public Move playerTwoMoveToDo;
        
        /// <summary>
        /// making sure we don't run the inital chosen logic more than once
        /// </summary>
        private bool playerHasChosenMove;
        
        /// <summary>
        /// making sure we don't run the inital choosing logic more than once
        /// </summary>
        private bool hasDoneChoosingUpdate;
        
        /// <summary>
        /// making sure we don't run the inital showing logic more than once
        /// </summary>
        private bool hasSetupShowing;
        
        /// <summary>
        /// the players current battler
        /// </summary>
        private Battler playerOneCurrentBattler => partyOne[playerOneBattlerIndex];
        
        /// <summary>
        /// the playerTwos current battler
        /// </summary>
        private Battler playerTwoCurrentBattler => partyTwo[playerTwoBattlerIndex];

        /// <summary>
        /// the active queue of turn items
        /// </summary>
        public List<TurnItem> turnItemQueue = new List<TurnItem>();
        
        /// <summary>
        /// are we currently running a turn item or are we waiting
        /// </summary>
        private bool _currentlyRunningQueueItem = false;
        
        /// <summary>
        /// the list of battlers that participated on the players team during this battle
        /// </summary>
        public List<Battler> playerOneBattlersThatParticipated;
        
        // is this battle a trainer or wild battle
        public bool trainerBattle;
        
        private string _playerTwoName;
        
        // the action the player has chosen to perform
        public TurnItem playerAction;
        
        /// <summary>
        /// the current turn item
        /// </summary>
        public TurnItem currentTurnItem;
        
        // events
        private EventHandler<BattlerTookDamageArgs> _playerTwoBattlerDefeated = null;
        private EventHandler<OnLevelUpEventArgs> _playerBattlerLeveledUp = null;
        private EventHandler<EvolutionData> _playerBattlerEvolved = null;
        private EventHandler<BattlerTookDamageArgs> _playerBattlerDefeated = null;
        
        // temporary stored variables for using items
        private Item _playerItemToUse;
        private int _battlerToUseItemOn;
        private bool _useItemOnPartyOne;
        
        // temporary stored variables for level up screen
        private BattlerStats _oldLevelUpStats;
        private BattlerStats _newLevelUpStats;
        private int _newLevel;
        private string _newBattlerName;
        private GameObject _currentLevelUpObj;
        
        /// <summary>
        /// the index of the battler the player has chosen to swap to
        /// </summary>
        private int _playerSwapIndex;

        private bool _displayingAttackAnimation;
        
        private void Start()
        {
            LoadStartingVariables();
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                if (!partyOne[i].isFainted)
                {
                    ChangePlayerOneBattlerIndex(i, true);
                    break;
                }
            }

            ChangePlayerTwoBattlerIndex(0, true);
            
            HookEvents();
            
            // adds current battler to list of participating battlers
            playerOneBattlersThatParticipated.Add(playerOneCurrentBattler);
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyOpen"));
        }

        private void LoadStartingVariables()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Loads relevant info like the playerTwo and player party
            trainerBattle = SceneLoader.GetVariable<bool>("trainerBattle");
            partyOne = new BattleParty(SceneLoader.GetVariable<Party>("partyOne"));
            partyTwo = new BattleParty(SceneLoader.GetVariable<Party>("partyTwo"));
            if (trainerBattle)
            {
                enemyAI = SceneLoader.GetVariable<EnemyAI>("enemyAI");
                _playerTwoName = SceneLoader.GetVariable<string>("playerTwoName");
            }
        }

        private void HookEvents()
        {
            DialogueManager.instance.DialogueEnded += DialogueEnded;
            DialogueManager.instance.DialogueStarted += OnDialogueStarted;
            partyOne.PartyAllDefeated += PlayerOnePartyAllDefeated;
            partyTwo.PartyAllDefeated += PlayerTwoPartyAllDefeated;

            _playerTwoBattlerDefeated = (s, e) => BattlerFainted(e, partyTwo.party.Find(x => x == e.damaged));
            
            _playerBattlerLeveledUp = (s, e) => BattlerLeveledUp(partyOne.party.Find(x => x == (Battler)s), e);
            _playerBattlerEvolved = (s, e) => BattlerEvolved(partyOne.party.Find(x => x == (Battler)s), e.evolution);

            for (int i = 0; i < partyTwo.Count; i++)
            {
                partyTwo[i].OnFainted += _playerTwoBattlerDefeated;
            }

            _playerBattlerDefeated = (s, e) =>
            {
                if (playerOneCurrentBattler.isFainted)
                {
                    PlayerOneBattlerDied();
                }
            };
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnFainted += _playerBattlerDefeated;
            }
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnLevelUp += _playerBattlerLeveledUp;
            }
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnCanEvolve += _playerBattlerEvolved;
            }
        }

        private void OnDisable()
        {
            partyOne.PartyAllDefeated -= PlayerOnePartyAllDefeated;
            partyTwo.PartyAllDefeated -= PlayerTwoPartyAllDefeated;
            DialogueManager.instance.DialogueEnded -= DialogueEnded;
            
            for (int i = 0; i < partyTwo.Count; i++)
            {
                partyTwo[i].OnFainted -= _playerTwoBattlerDefeated;
            }
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnLevelUp -= _playerBattlerLeveledUp;
            }
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnCanEvolve -= _playerBattlerEvolved;
            }
        }

        private void PlayerOnePartyAllDefeated(object sender, EventArgs e)
        {
            SomeoneDefeated(false);
        }

        private void PlayerTwoPartyAllDefeated(object sender, EventArgs e)
        {
            SomeoneDefeated(true);
        }

        private void ClearTurnQueue()
        {
            turnItemQueue.Clear();
        }

        private void BattlerLevelUpEvent()
        {
            QueDialogue($"{_newBattlerName} reached level {_newLevel}!", DialogueBoxType.Narration, "leveledUp");
        }

        private void BattlerLeveledUp(Battler battlerThatLeveled, OnLevelUpEventArgs args)
        {
            _oldLevelUpStats = args.oldStats;
            _newLevelUpStats = args.newStats;
            _newBattlerName = battlerThatLeveled.name;
            _newLevel = args.newLevel;
            Debug.Log("Queuing player level up");
            turnItemQueue.Insert(0, TurnItem.PlayerOneLevelUp);
        }

        private void BattlerEvolved(Battler battlerThatEvolved, BattlerTemplate newTemplate)
        {
            QueDialogue($"{battlerThatEvolved.name} evolved into a {newTemplate.name}!", DialogueBoxType.Narration, "evolved");
        }

        private void ShowBattlerLeveled()
        {
            LevelUpDisplay display = Instantiate(levelUpDisplayPrefab, FindFirstObjectByType<Canvas>().transform);
            _currentLevelUpObj = display.gameObject;
            display.Init(_oldLevelUpStats, _newLevelUpStats);
        }

        private MoveMethodEventArgs _currentArgs;

        private IEnumerator ShowPlayerOneMove()
        {
            if (_currentArgs.missed)
            {
                yield return null;
            }
            
            _displayingAttackAnimation = true;
            if (playerOneMoveToDo.category != MoveCategory.Status && !_currentArgs.missed)
            {
                playerTwoCurrentBattler.TakeDamage(_currentArgs.damageDealt, new BattlerDamageSource(playerOneCurrentBattler));
            }
            
            MoveMethodEventArgs args = _currentArgs;
            
            yield return new WaitForSeconds(1);
            
            _displayingAttackAnimation = false;
            StartDialogue();
            
            if (args.effectiveIndex == 0 && !args.crit)
            {
                Debug.Log("HEY IM ENDING THE PLAYERS TURN NOW");
                TurnQueueItemEnded();
            }
        }

        private IEnumerator ShowPlayerTwoMove()
        {
            if (_currentArgs.missed)
            {
                yield return null;
            }
            
            _displayingAttackAnimation = true;
            if (playerTwoMoveToDo.category != MoveCategory.Status && !_currentArgs.missed)
            {
                playerOneCurrentBattler.TakeDamage(_currentArgs.damageDealt, new BattlerDamageSource(playerTwoCurrentBattler));
            }

            MoveMethodEventArgs args = _currentArgs;
            
            Debug.Log("about to start the waiting period for the playerTwo move");
            
            yield return new WaitForSeconds(1);
            
            _displayingAttackAnimation = false;
            StartDialogue();
            
            // make sure there will be no more dialogue regarding the playerTwosturn
            if (args.effectiveIndex == 0 && !args.crit)
            {
                TurnQueueItemEnded();
            }
        }

        public void FinishedViewingLevelUpScreen()
        {
            Destroy(_currentLevelUpObj);
            TurnQueueItemEnded();
        }

        public void BattlerFainted(EventArgs e, Battler defeated)
        {
            uiManager.ShrinkPlayerTwoBattler();
            
            QueDialogue($"Enemy {defeated.name} Fainted!", DialogueBoxType.Event);

            int exp = ExperienceCalculator.GetExperienceFromDefeatingBattler(defeated, playerOneCurrentBattler, true,
                playerOneBattlersThatParticipated.Count);

            foreach (Battler battler in playerOneBattlersThatParticipated)
            {
                if (!battler.isFainted)
                {
                    QueDialogue($"{battler.name} gained {exp} experience points", DialogueBoxType.Event, "expGained");
                    battler.GainExp(exp);
                }
            }
            
            partyTwo.CheckDefeatedStatus();

            if (!partyTwo.defeated)
            {
                turnItemQueue.Add(TurnItem.PlayerTwoSwapBecauseFainted);
            }
            turnItemQueue.Remove(TurnItem.PlayerTwoMove);

            playerOneCurrentBattler.EVs.maxHealth += defeated.source.yields.maxHealth;
            playerOneCurrentBattler.EVs.attack += defeated.source.yields.attack;
            playerOneCurrentBattler.EVs.defense += defeated.source.yields.defense;
            playerOneCurrentBattler.EVs.specialAttack += defeated.source.yields.specialAttack;
            playerOneCurrentBattler.EVs.specialDefense += defeated.source.yields.specialDefense;
            playerOneCurrentBattler.EVs.speed += defeated.source.yields.speed;
        }
        
        private void Update()
        {
            if (playerHasChosenMove)
            {
                currentTurn = TurnStatus.Showing;
            }
            
            switch (currentTurn)
            {
                case TurnStatus.Ending:
                    TurnEnding();
                    break;
                case TurnStatus.Showing:
                    TurnShowing();
                    break;
                case TurnStatus.Choosing:
                    TurnChoosing();
                    break;
            }
        }

        private void TurnChoosing()
        {
            if (!hasDoneChoosingUpdate)
            {
                uiManager.ShowControlUI(true);
                uiManager.ShowUI(true);
                uiManager.UpdateBattlerMoveDisplays();
                if (trainerBattle)
                {
                    enemyAI.AIMethod(new AIMethodEventArgs(playerTwoCurrentBattler, partyTwo,
                        ExternalBattleData.Construct(this)));
                }
                else
                {
                    EnemyAIMethods.WildPokemon(new AIMethodEventArgs(playerTwoCurrentBattler, partyTwo,
                        ExternalBattleData.Construct(this)));
                }
                hasDoneChoosingUpdate = true;
            }
        }
        
        private void TurnShowing()
        {
            if (!hasSetupShowing)
            {
                hasSetupShowing = true;
                uiManager.ShowControlUI(false);
                turnItemQueue.Add(TurnItem.StartDelay);
                
                if (playerAction is not TurnItem.PlayerOneMove)
                {
                    turnItemQueue.Add(playerAction);
                }
                
                turnItemQueue.Add(TurnItem.StartOfTurnStatusEffects);
                QueueMoves();
                turnItemQueue.Add(TurnItem.EndOfTurnStatusEffects);
            }
            
            if (!_currentlyRunningQueueItem)
            {
                if (turnItemQueue.Count > 0 && !CurrentlyEndingTheBattle())
                {
                    NewTurnItem();
                }
                else if (!CurrentlyEndingTheBattle())
                {
                    EndTurnShowing();
                }
            }
        }

        private void NewTurnItem()
        {
            _currentlyRunningQueueItem = true;

            currentTurnItem = turnItemQueue[0];
            turnItemQueue.RemoveAt(0);

            Debug.Log($"Starting turn item: {currentTurnItem}");

            switch (currentTurnItem)
            {
                case TurnItem.StartDelay:
                    StartCoroutine(TurnStartDelay());
                    break;
                case TurnItem.PlayerOneMove:
                    DoPlayerOneOneMove();
                    break;
                case TurnItem.PlayerTwoMove:
                    DoPlayerTwoMove();
                    break;
                case TurnItem.EndBattlePlayerOneWin:
                    BeginEndBattleDialogue(true);
                    break;
                case TurnItem.EndBattlePlayerTwoWin:
                    BeginEndBattleDialogue(false);
                    break;
                case TurnItem.PlayerOneSwapBecauseFainted:
                    BeginSwapPlayerOneBattler();
                    break;
                case TurnItem.PlayerOneSwap:
                    PlayerOneOneSwappedBattler();
                    break;
                case TurnItem.PlayerOneItem:
                    PlayerOneOneUseItem(_battlerToUseItemOn, _useItemOnPartyOne);
                    break;
                case TurnItem.PlayerOneLevelUp:
                    BattlerLevelUpEvent();
                    break;
                case TurnItem.PlayerTwoSwap:
                    PlayerTwoSwitchBattler();
                    break;
                case TurnItem.PlayerTwoSwapBecauseFainted:
                    PlayerTwoSwitchBattler();
                    break;
                case TurnItem.StartOfTurnStatusEffects:
                    RunStartOfTurnStatusEffects();
                    break;
                case TurnItem.EndOfTurnStatusEffects:
                    RunEndOfTurnStatusEffects();
                    break;
                case TurnItem.PlayerTwoParalysed:
                    PlayerTwoParalysed();
                    break;
                case TurnItem.PlayerTwoAsleep:
                    PlayerTwoAsleep();
                    break;
                case TurnItem.PlayerOneParalysed:
                    PlayerOneParalysed();
                    break;
                case TurnItem.PlayerOneAsleep:
                    PlayerOneAsleep();
                    break;
                case TurnItem.CatchAttempt:
                    CatchAttempt();
                    break;
                case TurnItem.Run:
                    RunRunAwayDialogue();
                    break;
                case TurnItem.PlayerOneMissed:
                    MoveMissed();
                    break;
                case TurnItem.PlayerTwoMissed:
                    MoveMissed();
                    break;
            }

            uiManager.UpdatePlayerOneBattlerDetails();
            uiManager.UpdatePlayerTwoBattlerDetails();
        }

        private bool CurrentlyEndingTheBattle()
        {
            return currentTurnItem is TurnItem.EndBattlePlayerTwoWin or TurnItem.EndBattlePlayerOneWin or TurnItem.Run;
        }

        private IEnumerator TurnStartDelay()
        {
            yield return new WaitForSeconds(1);
            TurnQueueItemEnded();
        }

        public void TurnQueueItemEnded()
        {
            Debug.Log($"Ending turn item: {currentTurnItem}");
            _currentlyRunningQueueItem = false;
        }

        private void EndTurnShowing()
        {
            playerHasChosenMove = false;
            currentTurn = TurnStatus.Ending;
        }

        private void DialogueMoveUsed(MoveMethodEventArgs e, bool player)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("battlerUsed", e.attacker.name);
            variables.Add("moveUsed", e.move.name);
            variables.Add("battlerHit", e.target.name);

            if (player)
            {
                QueDialogue(battlerUsedText, DialogueBoxType.Event, "playerMoveUsed", true, variables);
            }
            else
            {
                QueDialogue(battlerUsedText, DialogueBoxType.Event, "playerTwoMoveUsed", true, variables);
            }
        }

        private void DialogueMoveEffectiveness(MoveMethodEventArgs e)
        {
            switch (e.effectiveIndex)
            {
                case 1:
                    QueDialogue("Its Not Very Effective...", DialogueBoxType.Event, "moveEffectiveness");
                    break;
                case 2:
                    QueDialogue("Its Super Effective!", DialogueBoxType.Event, "moveEffectiveness");
                    break;
                case 3:
                    QueDialogue($"{e.target.name} is immune!", DialogueBoxType.Event, "moveEffectiveness");
                    break;
            }

            if (e.crit)
            {
                QueDialogue("A Critical Hit!", DialogueBoxType.Event, "moveEffectiveness");
            }
        }

        private void TurnEnding()
        {
            hasDoneChoosingUpdate = false;
            hasSetupShowing = false;
            playerHasChosenMove = false;
                
            playerTwoMoveToDo = null;
            playerOneMoveToDo = null;
            
            EndTurnEnding();
        }

        private void EndTurnEnding()
        {
            currentTurn = TurnStatus.Choosing;
        }

        //Public method used by the move UI buttons
        public void ChooseMove(int moveID)
        {
            playerOneMoveToDo = playerOneCurrentBattler.moves[moveID];
            playerHasChosenMove = true;
            playerAction = TurnItem.PlayerOneMove;
        }

        public void RemoveTurnItem(TurnItem turnItemToRemove)
        {
            turnItemQueue.RemoveAll(item => item == turnItemToRemove);
        }

        private void DialogueEnded(object sender, DialogueEndedEventArgs args)
        {
            switch (args.id)
            {
                case "run":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "swap":
                    PlayerOneOneSwappedBattler();
                    break;
                case "playerDefeated":
                    StartCoroutine(ExitBattleLoss());
                    break;
                case "playerTwoDefeated":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "leveledUp":
                    ShowBattlerLeveled();
                    break;
                case "playerMoveUsed":
                    StartCoroutine(ShowPlayerOneMove());
                    break;
                case "playerTwoMoveUsed":
                    StartCoroutine(ShowPlayerTwoMove());
                    break;
                case "playerFainted":
                    uiManager.SwitchBattlerBecauseOfDeath();
                    break;
                case "moveEffectiveness":
                    if (!args.moreToGo)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "moveMissed":
                    if (!args.moreToGo)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "statusEffect":
                    if (!args.moreToGo)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "sentOut":
                    if (!args.moreToGo)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "expGained":
                    if (!args.moreToGo)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
            }
        }

        private void OnDialogueStarted(object sender, DialogueStartedEventArgs e)
        {
            switch (e.id)
            {
                case "evolved":
                    EvolutionEffect(e);
                    break;
                case "playerFainted":
                    uiManager.ShrinkPlayerOneBattler();
                    break;
            }
        }

        private void EvolutionEffect(DialogueStartedEventArgs e)
        {
            Battler evolvedBattler = playerOneCurrentBattler;
            foreach (var playerBattler in partyOne.party)
            {
                if (e.text.Contains(playerBattler.name))
                {
                    evolvedBattler = playerBattler;
                }
            }

            if (evolvedBattler == playerOneCurrentBattler)
            {
                uiManager.ShrinkPlayerOneBattler();
                StartCoroutine(DelayEvolution(evolvedBattler));
            }
            else
            {
                evolvedBattler.EvolutionApproved();
            }
        }

        public void UseItem(int battlerToUseOn, bool useOnPlayerOneOne)
        {
            playerAction = TurnItem.PlayerOneItem;
            playerHasChosenMove = true;
            _battlerToUseItemOn = battlerToUseOn;
            _useItemOnPartyOne = useOnPlayerOneOne;
            uiManager.Back();
        }

        public void PlayerOnePickedPokeBall(PokeBall ball)
        {
            playerAction = TurnItem.CatchAttempt;
            playerHasChosenMove = true;
            _playerItemToUse = ball;
            Bag.Used(ball);
        }

        public void PlayerOnePickedItemToUse(Item item)
        {
            _playerItemToUse = item;
        }

        public void StartPickingBattlerToUseItemOn()
        {
            if (_playerItemToUse.lockedTarget)
            {
                UseItem(_playerItemToUse.targetIndex, _playerItemToUse.userParty);
            }
            else
            {
                uiManager.OpenUseItemOnBattler(_playerItemToUse);
                uiManager.UpdateItemBattlerButtons();
            }
        }

        private void CatchAttempt()
        {
            QueDialogue($"Threw a pokeball at {playerTwoCurrentBattler.name}!", DialogueBoxType.Event);

            if (ExperienceCalculator.Captured(playerTwoCurrentBattler, playerOneCurrentBattler, (PokeBall)_playerItemToUse))
            {
                uiManager.ShrinkPlayerTwoBattler(true);
                QueDialogue($"Caught {playerTwoCurrentBattler.name}!", DialogueBoxType.Event);
                PartyManager.AddBattler(playerTwoCurrentBattler);
                turnItemQueue.Insert(0, TurnItem.EndBattlePlayerOneWin);
            }
            else
            {
                QueDialogue($"Failed to catch {playerTwoCurrentBattler.name}!", DialogueBoxType.Event);
            }
        }

        private void PlayerOneOneUseItem(int battlerToUseOn, bool useOnUserParty)
        {
            Battler battleBeingUsedOn = useOnUserParty ? partyOne[battlerToUseOn] : partyTwo[battlerToUseOn];
            
            ItemMethodEventArgs e = new ItemMethodEventArgs(battleBeingUsedOn, _playerItemToUse);
            
            _playerItemToUse.ItemMethod(e);
            
            Bag.Used(_playerItemToUse);
            
            QueDialogue($"You used {_playerItemToUse.name} on {battleBeingUsedOn.name}!", DialogueBoxType.Event);

            if (!e.success)
            {
                QueDialogue("But it failed!", DialogueBoxType.Event);
            }
        }

        public void ChooseToSwap(int newBattlerIndex)
        {
            if (_currentlyRunningQueueItem) // swapping mid turn showing aka after a battler faints
            {
                _playerSwapIndex = newBattlerIndex;
                QueDialogue($"You sent out {partyOne[newBattlerIndex].name}", DialogueBoxType.Event, "swap", true);
            }
            else // player chose to swap as their move
            {
                _playerSwapIndex = newBattlerIndex;
                playerHasChosenMove = true;
                playerAction = TurnItem.PlayerOneSwap;
            }
        }

        private void BeginSwapPlayerOneBattler()
        {
            QueDialogue($"{playerOneCurrentBattler.name} Fainted!", DialogueBoxType.Event, "playerFainted");
        }

        private void ShowSwapPlayerOneBattler()
        {
            uiManager.ShrinkPlayerOneBattler();
            uiManager.SwitchBattlerBecauseOfDeath();
        }

        private void ChangePlayerOneBattlerIndex(int index, bool skipShrink = false)
        {
            if (!skipShrink)
            {
                uiManager.ShrinkPlayerOneBattler();
                playerOneBattlerIndex = index;
                StartCoroutine(DelayChangeBattlerIndex(index));
            }
            else
            {
                playerOneBattlerIndex = index;
                currentDisplayBattlerIndex = index;
                Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                    uiManager.playerBattler);
                uiManager.ExpandPlayerOneBattler();
                uiManager.UpdatePlayerOneBattlerDetails();
            }
        }

        private IEnumerator DelayChangeBattlerIndex(int index)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            currentDisplayBattlerIndex = index;
            Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.playerBattler);
            uiManager.ExpandPlayerOneBattler();
            uiManager.UpdatePlayerOneBattlerDetails();
        }

        private IEnumerator DelayEvolution(Battler battlerToEvolve)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            battlerToEvolve.EvolutionApproved();
            Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.playerBattler);
            uiManager.ExpandPlayerOneBattler();
            uiManager.UpdatePlayerOneBattlerDetails();
        }

        private void ChangePlayerTwoBattlerIndex(int index, bool skipShrink = false)
        {
            if (!skipShrink)
            {
                uiManager.ShrinkPlayerTwoBattler();
                playerTwoBattlerIndex = index;
                StartCoroutine(DelayChangePlayerTwoBattlerIndex(index));
            }
            else
            {
                playerTwoBattlerIndex = index;
                playerTwoDisplayBattlerIndex = index;
                Instantiate(spawnEffect, uiManager.playerTwoBattler.transform.position, spawnEffect.transform.rotation,
                    uiManager.playerTwoBattler);
                uiManager.ExpandPlayerTwoBattler();
                uiManager.UpdatePlayerTwoBattlerDetails();
            }
        }

        private IEnumerator DelayChangePlayerTwoBattlerIndex(int index)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            playerTwoDisplayBattlerIndex = index;
            Instantiate(spawnEffect, uiManager.playerTwoBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.playerTwoBattler);
            uiManager.ExpandPlayerTwoBattler();
            uiManager.UpdatePlayerTwoBattlerDetails();
        }

        private void PlayerOneOneSwappedBattler()
        {
            ChangePlayerOneBattlerIndex(_playerSwapIndex);
            
            AddParticipatedBattler(partyOne[_playerSwapIndex]);

            uiManager.UpdatePlayerOneBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"Go ahead {partyOne[_playerSwapIndex].name}!", DialogueBoxType.Event, "sentOut", true);
        }

        private void PlayerTwoSwitchBattler()
        {
            AISwitchEventArgs e =
                new AISwitchEventArgs(playerTwoBattlerIndex, partyTwo, ExternalBattleData.Construct(this));
            
            enemyAI.AISwitchMethod(e);

            ChangePlayerTwoBattlerIndex(e.newBattlerIndex, true);
            
            uiManager.UpdatePlayerTwoBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"PlayerTwo sent out {partyTwo[e.newBattlerIndex].name}!", DialogueBoxType.Event, "sentOut", true);
        }

        private void PlayerOneParalysed()
        {
            QueDialogue($"{playerOneCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        private void PlayerTwoParalysed()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        private void PlayerOneAsleep()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        private void PlayerTwoAsleep()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        private void MoveMissed()
        {
            QueDialogue($"But it missed!", DialogueBoxType.Event, "moveMissed");
        }

        public void AddParticipatedBattler(Battler battlerToParticipate)
        {
            if (!playerOneBattlersThatParticipated.Contains(battlerToParticipate))
            {
                playerOneBattlersThatParticipated.Add(battlerToParticipate);
            }
        }

        private void DoPlayerOneOneMove()
        {
            bool able = true;
            bool missed = false;
            
            if (playerOneCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Insert(0, TurnItem.PlayerOneParalysed);
                    able = false;
                }
            }else if (playerOneCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                turnItemQueue.Insert(0, TurnItem.PlayerOneAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            if (playerOneMoveToDo.accuracy != 0 && !Mathf.Approximately(playerOneMoveToDo.accuracy, 1))
            {
                // actually has like a usable accuracy
                missed = Random.Range(1, 101) > playerOneMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            //You can add any animation calls for attacking here
            
            int moveToDoIndex = GetIndexOfMoveOnCurrentPlayerOne(playerOneMoveToDo);
            
            MoveMethodEventArgs e = new MoveMethodEventArgs(playerOneCurrentBattler, playerTwoCurrentBattler, playerOneMoveToDo, 
                playerOneCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));

            DialogueMoveUsed(e, true);
            _currentArgs = e;
            
            if (missed)
            {
                turnItemQueue.Insert(0, TurnItem.PlayerOneMissed);
                e.missed = true;
                return;
            }
            
            playerOneMoveToDo.MoveMethod(e);

            DialogueMoveEffectiveness(e);
        }

        private void QueueMoves()
        {
            if (playerAction is TurnItem.CatchAttempt or TurnItem.PlayerOneSwap or TurnItem.PlayerOneItem)
            {
                AddPlayerTwoMoveToQueue();
                // dont add the player move to queue because they are doing something else

                return;
            }

            float playerAdjustedSpeed = playerOneCurrentBattler.stats.speed;
            float playerTwoAdjustedSpeed = playerTwoCurrentBattler.stats.speed;

            if (playerOneCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                playerAdjustedSpeed /= 2;
            }
            
            if (playerTwoCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                playerTwoAdjustedSpeed /= 2;
            }
            
            if (playerOneMoveToDo.priority == playerTwoMoveToDo.priority)
            {
                if(playerAdjustedSpeed > playerTwoAdjustedSpeed)
                {
                    //PlayerOne BATTLER is faster
                    AddPlayerOneMoveToQueue();
                    AddPlayerTwoMoveToQueue();
                }
                else
                {
                    //Enemy BATTLER is faster
                    AddPlayerTwoMoveToQueue();
                    AddPlayerOneMoveToQueue();
                }
            }
            else
            {
                if(playerOneMoveToDo.priority > playerTwoMoveToDo.priority)
                {
                    //PlayerOne MOVE is faster
                    AddPlayerOneMoveToQueue();
                    AddPlayerTwoMoveToQueue();
                }
                else
                {
                    //Enemy MOVE is faster
                    AddPlayerTwoMoveToQueue();
                    AddPlayerOneMoveToQueue();
                }
            }
        }
        
        private void AddPlayerOneMoveToQueue()
        {
            turnItemQueue.Add(TurnItem.PlayerOneMove);
        }

        private void AddPlayerTwoMoveToQueue()
        {
            turnItemQueue.Add(TurnItem.PlayerTwoMove);
        }

        private void DoPlayerTwoMove()
        {
            bool able = true;
            bool missed = false;
            
            if (playerTwoCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Insert(0, TurnItem.PlayerTwoParalysed);
                    able = false;
                }
            }else if (playerTwoCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                turnItemQueue.Insert(0, TurnItem.PlayerTwoAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            if (playerTwoMoveToDo.accuracy != 0 && !Mathf.Approximately(playerTwoMoveToDo.accuracy, 1))
            {
                // actually has like a usable accuracy
                missed = Random.Range(1, 101) > playerTwoMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            //You can add any animation calls for attacking here

            int moveToDoIndex = GetIndexOfMoveOnCurrentEnemy(playerTwoMoveToDo);

            MoveMethodEventArgs e = new MoveMethodEventArgs(playerTwoCurrentBattler, playerOneCurrentBattler,
                playerTwoMoveToDo, playerTwoCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));
            
            DialogueMoveUsed(e, false);
            _currentArgs = e;
            
            if (missed)
            {
                turnItemQueue.Insert(0, TurnItem.PlayerTwoMissed);
                e.missed = true;
                return;
            }
            
            playerTwoMoveToDo.MoveMethod(e);
            
            DialogueMoveEffectiveness(e);
        }

        private void PlayerOneBattlerDied()
        {
            turnItemQueue.RemoveAll(item => item == TurnItem.PlayerOneMove);

            partyOne.CheckDefeatedStatus();

            if (!partyOne.defeated)
            {
                turnItemQueue.Add(TurnItem.PlayerOneSwapBecauseFainted);
            }
            
            playerOneBattlersThatParticipated.Remove(playerOneCurrentBattler);
        }

        public void RunFromBattle()
        {
            playerHasChosenMove = true;
            playerAction = TurnItem.Run;
        }

        private void RunRunAwayDialogue()
        {
            QueDialogue("Running Away!", DialogueBoxType.Event, "run");
        }

        private int GetIndexOfMoveOnCurrentEnemy(Move move)
        {
            for (int i = 0; i < playerTwoCurrentBattler.moves.Count; i++)
            {
                if (playerTwoCurrentBattler.moves[i] == move)
                {
                    return i;
                }
            }

            Debug.LogWarning($"Could not find move {move.name} on the current playerTwo battler");
            return -1;
        }
        
        private int GetIndexOfMoveOnCurrentPlayerOne(Move move)
        {
            for (int i = 0; i < playerOneCurrentBattler.moves.Count; i++)
            {
                if (playerOneCurrentBattler.moves[i] == move)
                {
                    return i;
                }
            }

            Debug.LogWarning($"Could not find move {move.name} on the current player battler");
            return -1;
        }
        
        private void RunEndOfTurnStatusEffects()
        {
            bool anyEffects = false;
            
            if (!playerOneCurrentBattler.isFainted)
            {
                foreach (var trigger in playerOneCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.EndOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerOneCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            if (!playerTwoCurrentBattler.isFainted)
            {
                foreach (var trigger in playerTwoCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.EndOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerTwoCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            partyOne.CheckDefeatedStatus();
            partyTwo.CheckDefeatedStatus();
            
            if (!anyEffects)
            {
                TurnQueueItemEnded();
            }
        }
        
        private void RunStartOfTurnStatusEffects()
        {
            bool anyEffects = false;

            if (!playerOneCurrentBattler.isFainted)
            {
                foreach (var trigger in playerOneCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.StartOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerOneCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            if (!playerTwoCurrentBattler.isFainted)
            {
                foreach (var trigger in playerTwoCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.StartOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerTwoCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            partyOne.CheckDefeatedStatus();
            partyTwo.CheckDefeatedStatus();

            if (!anyEffects)
            {
                TurnQueueItemEnded();
            }
        }

        private IEnumerator ExitBattleWin()
        {
            yield return new WaitForSeconds(0.5f);
            
            Dictionary<string, object> vars = new Dictionary<string, object>
            {
                { "partyOne", partyOne },
                { "trainerName", _playerTwoName },
                { "isDefeated", true },
                { "trainerBattle", trainerBattle}
            };
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyClose"));
            yield return new WaitForSeconds(0.4f);
            
            SceneLoader.LoadScene("Game", vars);
        }

        private IEnumerator ExitBattleLoss()
        {
            yield return new WaitForSeconds(0.5f);
            
            Dictionary<string, object> vars = new Dictionary<string, object>
            {
                { "partyOne", partyOne },
                { "trainerName", _playerTwoName },
                { "isDefeated", false },
                { "loaderName", "ForcedHealPoint" },
                { "trainerBattle", trainerBattle}
            };
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyClose"));
            yield return new WaitForSeconds(0.4f);
            
            SceneLoader.LoadScene("Poke Center", vars);
        }

        private void SomeoneDefeated(bool isDefeated)
        {
            if (isDefeated)
            {
                turnItemQueue.Add(TurnItem.EndBattlePlayerOneWin);
            }
            else
            {
                turnItemQueue.Add(TurnItem.EndBattlePlayerTwoWin);
            }
        }
        
        private void BeginEndBattleDialogue(bool isDefeated)
        {
            if (isDefeated)
            {
                if (trainerBattle)
                {
                    QueDialogue("All playerTwo Pokemon defeated!", DialogueBoxType.Event, "playerTwoDefeated", true);
                }
                else
                {
                    StartCoroutine(ExitBattleWin());
                }
            }
            else
            {
                QueDialogue("All your Pokemon fainted, running!", DialogueBoxType.Event, "playerDefeated", true);
            }
            
            turnItemQueue.Clear();
        }
    }
}