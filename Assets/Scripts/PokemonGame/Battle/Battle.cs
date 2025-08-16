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
        public int currentBattlerIndex;
        public int opponentBattlerIndex;
        [SerializeField] public TurnStatus currentTurn = TurnStatus.Choosing;
        public BattleParty partyOne;
        public BattleParty partyTwo;
        [SerializeField] private EnemyAI enemyAI;
        [SerializeField] private Move playerMoveToDo;
        
        
        [HideInInspector] public int currentDisplayBattlerIndex;
        [HideInInspector] public int opponentDisplayBattlerIndex;
        
        /// <summary>
        /// the move the enemy intends to use
        /// </summary>
        public Move enemyMoveToDo;
        
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
        private Battler playerCurrentBattler => partyOne[currentBattlerIndex];
        
        /// <summary>
        /// the opponents current battler
        /// </summary>
        private Battler opponentCurrentBattler => partyTwo[opponentBattlerIndex];

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
        public List<Battler> battlersThatParticipated;
        
        // is this battle a trainer or wild battle
        public bool trainerBattle;
        
        private string _opponentName;
        
        // the action the player has chosen to perform
        public TurnItem playerAction;
        
        /// <summary>
        /// the current turn item
        /// </summary>
        public TurnItem currentTurnItem;
        
        // events
        private EventHandler<BattlerTookDamageArgs> _opponentBattlerDefeated = null;
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
                    ChangePlayerBattlerIndex(i, true);
                    break;
                }
            }

            ChangeOpponentBattlerIndex(0, true);
            
            HookEvents();
            
            // adds current battler to list of participating battlers
            battlersThatParticipated.Add(playerCurrentBattler);
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyOpen"));
        }

        private void LoadStartingVariables()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Loads relevant info like the opponent and player party
            trainerBattle = SceneLoader.GetVariable<bool>("trainerBattle");
            partyOne = new BattleParty(SceneLoader.GetVariable<Party>("partyOne"));
            partyTwo = new BattleParty(SceneLoader.GetVariable<Party>("partyTwo"));
            if (trainerBattle)
            {
                enemyAI = SceneLoader.GetVariable<EnemyAI>("enemyAI");
                _opponentName = SceneLoader.GetVariable<string>("opponentName");
            }
        }

        private void HookEvents()
        {
            DialogueManager.instance.DialogueEnded += DialogueEnded;
            DialogueManager.instance.DialogueStarted += OnDialogueStarted;
            partyOne.PartyAllDefeated += PlayerPartyAllDefeated;
            partyTwo.PartyAllDefeated += OpponentPartyAllDefeated;

            _opponentBattlerDefeated = (s, e) => BattlerFainted(e, partyTwo.party.Find(x => x == e.damaged));
            
            _playerBattlerLeveledUp = (s, e) => BattlerLeveledUp(partyOne.party.Find(x => x == (Battler)s), e);
            _playerBattlerEvolved = (s, e) => BattlerEvolved(partyOne.party.Find(x => x == (Battler)s), e.evolution);

            for (int i = 0; i < partyTwo.Count; i++)
            {
                partyTwo[i].OnFainted += _opponentBattlerDefeated;
            }

            _playerBattlerDefeated = (s, e) =>
            {
                if (playerCurrentBattler.isFainted)
                {
                    PlayerBattlerDied();
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
            partyOne.PartyAllDefeated -= PlayerPartyAllDefeated;
            partyTwo.PartyAllDefeated -= OpponentPartyAllDefeated;
            DialogueManager.instance.DialogueEnded -= DialogueEnded;
            
            for (int i = 0; i < partyTwo.Count; i++)
            {
                partyTwo[i].OnFainted -= _opponentBattlerDefeated;
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

        private void PlayerPartyAllDefeated(object sender, EventArgs e)
        {
            SomeoneDefeated(false);
        }

        private void OpponentPartyAllDefeated(object sender, EventArgs e)
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
            turnItemQueue.Insert(0, TurnItem.PlayerLevelUp);
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

        private IEnumerator ShowPlayerMove()
        {
            if (_currentArgs.missed)
            {
                yield return null;
            }
            
            _displayingAttackAnimation = true;
            if (playerMoveToDo.category != MoveCategory.Status && !_currentArgs.missed)
            {
                opponentCurrentBattler.TakeDamage(_currentArgs.damageDealt, new BattlerDamageSource(playerCurrentBattler));
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

        private IEnumerator ShowOpponentMove()
        {
            if (_currentArgs.missed)
            {
                yield return null;
            }
            
            _displayingAttackAnimation = true;
            if (enemyMoveToDo.category != MoveCategory.Status && !_currentArgs.missed)
            {
                playerCurrentBattler.TakeDamage(_currentArgs.damageDealt, new BattlerDamageSource(opponentCurrentBattler));
            }

            MoveMethodEventArgs args = _currentArgs;
            
            Debug.Log("about to start the waiting period for the opponent move");
            
            yield return new WaitForSeconds(1);
            
            _displayingAttackAnimation = false;
            StartDialogue();
            
            // make sure there will be no more dialogue regarding the opponentsturn
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
            uiManager.ShrinkOpponentBattler();
            
            QueDialogue($"Enemy {defeated.name} Fainted!", DialogueBoxType.Event);

            int exp = ExperienceCalculator.GetExperienceFromDefeatingBattler(defeated, playerCurrentBattler, true,
                battlersThatParticipated.Count);

            foreach (Battler battler in battlersThatParticipated)
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
                turnItemQueue.Add(TurnItem.OpponentSwapBecauseFainted);
            }
            turnItemQueue.Remove(TurnItem.OpponentMove);

            playerCurrentBattler.EVs.maxHealth += defeated.source.yields.maxHealth;
            playerCurrentBattler.EVs.attack += defeated.source.yields.attack;
            playerCurrentBattler.EVs.defense += defeated.source.yields.defense;
            playerCurrentBattler.EVs.specialAttack += defeated.source.yields.specialAttack;
            playerCurrentBattler.EVs.specialDefense += defeated.source.yields.specialDefense;
            playerCurrentBattler.EVs.speed += defeated.source.yields.speed;
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
                    enemyAI.AIMethod(new AIMethodEventArgs(opponentCurrentBattler, partyTwo,
                        ExternalBattleData.Construct(this)));
                }
                else
                {
                    EnemyAIMethods.WildPokemon(new AIMethodEventArgs(opponentCurrentBattler, partyTwo,
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
                
                if (playerAction is not TurnItem.PlayerMove)
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
                case TurnItem.PlayerMove:
                    DoPlayerMove();
                    break;
                case TurnItem.OpponentMove:
                    DoEnemyMove();
                    break;
                case TurnItem.EndBattlePlayerWin:
                    BeginEndBattleDialogue(true);
                    break;
                case TurnItem.EndBattleOpponentWin:
                    BeginEndBattleDialogue(false);
                    break;
                case TurnItem.PlayerSwapBecauseFainted:
                    BeginSwapPlayerBattler();
                    break;
                case TurnItem.PlayerSwap:
                    PlayerSwappedBattler();
                    break;
                case TurnItem.PlayerItem:
                    PlayerUseItem(_battlerToUseItemOn, _useItemOnPartyOne);
                    break;
                case TurnItem.PlayerLevelUp:
                    BattlerLevelUpEvent();
                    break;
                case TurnItem.OpponentSwap:
                    OpponentSwitchBattler();
                    break;
                case TurnItem.OpponentSwapBecauseFainted:
                    OpponentSwitchBattler();
                    break;
                case TurnItem.StartOfTurnStatusEffects:
                    RunStartOfTurnStatusEffects();
                    break;
                case TurnItem.EndOfTurnStatusEffects:
                    RunEndOfTurnStatusEffects();
                    break;
                case TurnItem.OpponentParalysed:
                    OpponentParalysed();
                    break;
                case TurnItem.OpponentAsleep:
                    OpponentAsleep();
                    break;
                case TurnItem.PlayerParalysed:
                    PlayerParalysed();
                    break;
                case TurnItem.PlayerAsleep:
                    PlayerAsleep();
                    break;
                case TurnItem.CatchAttempt:
                    CatchAttempt();
                    break;
                case TurnItem.Run:
                    RunRunAwayDialogue();
                    break;
                case TurnItem.PlayerMissed:
                    MoveMissed();
                    break;
                case TurnItem.OpponentMissed:
                    MoveMissed();
                    break;
            }

            uiManager.UpdatePlayerBattlerDetails();
            uiManager.UpdateOpponentBattlerDetails();
        }

        private bool CurrentlyEndingTheBattle()
        {
            return currentTurnItem is TurnItem.EndBattleOpponentWin or TurnItem.EndBattlePlayerWin or TurnItem.Run;
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
                QueDialogue(battlerUsedText, DialogueBoxType.Event, "opponentMoveUsed", true, variables);
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
                
            enemyMoveToDo = null;
            playerMoveToDo = null;
            
            EndTurnEnding();
        }

        private void EndTurnEnding()
        {
            currentTurn = TurnStatus.Choosing;
        }

        //Public method used by the move UI buttons
        public void ChooseMove(int moveID)
        {
            playerMoveToDo = playerCurrentBattler.moves[moveID];
            playerHasChosenMove = true;
            playerAction = TurnItem.PlayerMove;
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
                    PlayerSwappedBattler();
                    break;
                case "playerDefeated":
                    StartCoroutine(ExitBattleLoss());
                    break;
                case "opponentDefeated":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "leveledUp":
                    ShowBattlerLeveled();
                    break;
                case "playerMoveUsed":
                    StartCoroutine(ShowPlayerMove());
                    break;
                case "opponentMoveUsed":
                    StartCoroutine(ShowOpponentMove());
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
                    uiManager.ShrinkPlayerBattler();
                    break;
            }
        }

        private void EvolutionEffect(DialogueStartedEventArgs e)
        {
            Battler evolvedBattler = playerCurrentBattler;
            foreach (var playerBattler in partyOne.party)
            {
                if (e.text.Contains(playerBattler.name))
                {
                    evolvedBattler = playerBattler;
                }
            }

            if (evolvedBattler == playerCurrentBattler)
            {
                uiManager.ShrinkPlayerBattler();
                StartCoroutine(DelayEvolution(evolvedBattler));
            }
            else
            {
                evolvedBattler.EvolutionApproved();
            }
        }

        public void UseItem(int battlerToUseOn, bool useOnPlayerOne)
        {
            playerAction = TurnItem.PlayerItem;
            playerHasChosenMove = true;
            _battlerToUseItemOn = battlerToUseOn;
            _useItemOnPartyOne = useOnPlayerOne;
            uiManager.Back();
        }

        public void PlayerPickedPokeBall(PokeBall ball)
        {
            playerAction = TurnItem.CatchAttempt;
            playerHasChosenMove = true;
            _playerItemToUse = ball;
            Bag.Used(ball);
        }

        public void PlayerPickedItemToUse(Item item)
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
            QueDialogue($"Threw a pokeball at {opponentCurrentBattler.name}!", DialogueBoxType.Event);

            if (ExperienceCalculator.Captured(opponentCurrentBattler, playerCurrentBattler, (PokeBall)_playerItemToUse))
            {
                uiManager.ShrinkOpponentBattler(true);
                QueDialogue($"Caught {opponentCurrentBattler.name}!", DialogueBoxType.Event);
                PartyManager.AddBattler(opponentCurrentBattler);
                turnItemQueue.Insert(0, TurnItem.EndBattlePlayerWin);
            }
            else
            {
                QueDialogue($"Failed to catch {opponentCurrentBattler.name}!", DialogueBoxType.Event);
            }
        }

        private void PlayerUseItem(int battlerToUseOn, bool useOnPlayerParty)
        {
            Battler battleBeingUsedOn = useOnPlayerParty ? partyOne[battlerToUseOn] : partyTwo[battlerToUseOn];
            
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
                playerAction = TurnItem.PlayerSwap;
            }
        }

        private void BeginSwapPlayerBattler()
        {
            QueDialogue($"{playerCurrentBattler.name} Fainted!", DialogueBoxType.Event, "playerFainted");
        }

        private void ShowSwapPlayerBattler()
        {
            uiManager.ShrinkPlayerBattler();
            uiManager.SwitchBattlerBecauseOfDeath();
        }

        private void ChangePlayerBattlerIndex(int index, bool skipShrink = false)
        {
            if (!skipShrink)
            {
                uiManager.ShrinkPlayerBattler();
                currentBattlerIndex = index;
                StartCoroutine(DelayChangeBattlerIndex(index));
            }
            else
            {
                currentBattlerIndex = index;
                currentDisplayBattlerIndex = index;
                Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                    uiManager.playerBattler);
                uiManager.ExpandPlayerBattler();
                uiManager.UpdatePlayerBattlerDetails();
            }
        }

        private IEnumerator DelayChangeBattlerIndex(int index)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            currentDisplayBattlerIndex = index;
            Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.playerBattler);
            uiManager.ExpandPlayerBattler();
            uiManager.UpdatePlayerBattlerDetails();
        }

        private IEnumerator DelayEvolution(Battler battlerToEvolve)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            battlerToEvolve.EvolutionApproved();
            Instantiate(spawnEffect, uiManager.playerBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.playerBattler);
            uiManager.ExpandPlayerBattler();
            uiManager.UpdatePlayerBattlerDetails();
        }

        private void ChangeOpponentBattlerIndex(int index, bool skipShrink = false)
        {
            if (!skipShrink)
            {
                uiManager.ShrinkOpponentBattler();
                opponentBattlerIndex = index;
                StartCoroutine(DelayChangeOpponentBattlerIndex(index));
            }
            else
            {
                opponentBattlerIndex = index;
                opponentDisplayBattlerIndex = index;
                Instantiate(spawnEffect, uiManager.opponentBattler.transform.position, spawnEffect.transform.rotation,
                    uiManager.opponentBattler);
                uiManager.ExpandOpponentBattler();
                uiManager.UpdateOpponentBattlerDetails();
            }
        }

        private IEnumerator DelayChangeOpponentBattlerIndex(int index)
        {
            yield return new WaitForSeconds(shrinkEffectDelay);
            opponentDisplayBattlerIndex = index;
            Instantiate(spawnEffect, uiManager.opponentBattler.transform.position, spawnEffect.transform.rotation,
                uiManager.opponentBattler);
            uiManager.ExpandOpponentBattler();
            uiManager.UpdateOpponentBattlerDetails();
        }

        private void PlayerSwappedBattler()
        {
            ChangePlayerBattlerIndex(_playerSwapIndex);
            
            AddParticipatedBattler(partyOne[_playerSwapIndex]);

            uiManager.UpdatePlayerBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"Go ahead {partyOne[_playerSwapIndex].name}!", DialogueBoxType.Event, "sentOut", true);
        }

        private void OpponentSwitchBattler()
        {
            AISwitchEventArgs e =
                new AISwitchEventArgs(opponentBattlerIndex, partyTwo, ExternalBattleData.Construct(this));
            
            enemyAI.AISwitchMethod(e);

            ChangeOpponentBattlerIndex(e.newBattlerIndex, true);
            
            uiManager.UpdateOpponentBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"Opponent sent out {partyTwo[e.newBattlerIndex].name}!", DialogueBoxType.Event, "sentOut", true);
        }

        private void PlayerParalysed()
        {
            QueDialogue($"{playerCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        private void OpponentParalysed()
        {
            QueDialogue($"The opponent {opponentCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        private void PlayerAsleep()
        {
            QueDialogue($"The opponent {opponentCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        private void OpponentAsleep()
        {
            QueDialogue($"The opponent {opponentCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        private void MoveMissed()
        {
            QueDialogue($"But it missed!", DialogueBoxType.Event, "moveMissed");
        }

        public void AddParticipatedBattler(Battler battlerToParticipate)
        {
            if (!battlersThatParticipated.Contains(battlerToParticipate))
            {
                battlersThatParticipated.Add(battlerToParticipate);
            }
        }

        private void DoPlayerMove()
        {
            bool able = true;
            bool missed = false;
            
            if (playerCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Insert(0, TurnItem.PlayerParalysed);
                    able = false;
                }
            }else if (playerCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                turnItemQueue.Insert(0, TurnItem.PlayerAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            if (playerMoveToDo.accuracy != 0 && !Mathf.Approximately(playerMoveToDo.accuracy, 1))
            {
                // actually has like a usable accuracy
                missed = Random.Range(1, 101) > playerMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            //You can add any animation calls for attacking here
            
            int moveToDoIndex = GetIndexOfMoveOnCurrentPlayer(playerMoveToDo);
            
            MoveMethodEventArgs e = new MoveMethodEventArgs(playerCurrentBattler, opponentCurrentBattler, playerMoveToDo, 
                playerCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));

            DialogueMoveUsed(e, true);
            _currentArgs = e;
            
            if (missed)
            {
                turnItemQueue.Insert(0, TurnItem.PlayerMissed);
                e.missed = true;
                return;
            }
            
            playerMoveToDo.MoveMethod(e);

            DialogueMoveEffectiveness(e);
        }

        private void QueueMoves()
        {
            if (playerAction is TurnItem.CatchAttempt or TurnItem.PlayerSwap or TurnItem.PlayerItem)
            {
                AddOpponentMoveToQueue();
                // dont add the player move to queue because they are doing something else

                return;
            }

            float playerAdjustedSpeed = playerCurrentBattler.stats.speed;
            float opponentAdjustedSpeed = opponentCurrentBattler.stats.speed;

            if (playerCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                playerAdjustedSpeed /= 2;
            }
            
            if (opponentCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                opponentAdjustedSpeed /= 2;
            }
            
            if (playerMoveToDo.priority == enemyMoveToDo.priority)
            {
                if(playerAdjustedSpeed > opponentAdjustedSpeed)
                {
                    //Player BATTLER is faster
                    AddPlayerMoveToQueue();
                    AddOpponentMoveToQueue();
                }
                else
                {
                    //Enemy BATTLER is faster
                    AddOpponentMoveToQueue();
                    AddPlayerMoveToQueue();
                }
            }
            else
            {
                if(playerMoveToDo.priority > enemyMoveToDo.priority)
                {
                    //Player MOVE is faster
                    AddPlayerMoveToQueue();
                    AddOpponentMoveToQueue();
                }
                else
                {
                    //Enemy MOVE is faster
                    AddOpponentMoveToQueue();
                    AddPlayerMoveToQueue();
                }
            }
        }
        
        private void AddPlayerMoveToQueue()
        {
            turnItemQueue.Add(TurnItem.PlayerMove);
        }

        private void AddOpponentMoveToQueue()
        {
            turnItemQueue.Add(TurnItem.OpponentMove);
        }

        private void DoEnemyMove()
        {
            bool able = true;
            bool missed = false;
            
            if (opponentCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Insert(0, TurnItem.OpponentParalysed);
                    able = false;
                }
            }else if (opponentCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                turnItemQueue.Insert(0, TurnItem.OpponentAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            if (enemyMoveToDo.accuracy != 0 && !Mathf.Approximately(enemyMoveToDo.accuracy, 1))
            {
                // actually has like a usable accuracy
                missed = Random.Range(1, 101) > enemyMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            //You can add any animation calls for attacking here

            int moveToDoIndex = GetIndexOfMoveOnCurrentEnemy(enemyMoveToDo);

            MoveMethodEventArgs e = new MoveMethodEventArgs(opponentCurrentBattler, playerCurrentBattler,
                enemyMoveToDo, opponentCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));
            
            DialogueMoveUsed(e, false);
            _currentArgs = e;
            
            if (missed)
            {
                turnItemQueue.Insert(0, TurnItem.OpponentMissed);
                e.missed = true;
                return;
            }
            
            enemyMoveToDo.MoveMethod(e);
            
            DialogueMoveEffectiveness(e);
        }

        private void PlayerBattlerDied()
        {
            turnItemQueue.RemoveAll(item => item == TurnItem.PlayerMove);

            partyOne.CheckDefeatedStatus();

            if (!partyOne.defeated)
            {
                turnItemQueue.Add(TurnItem.PlayerSwapBecauseFainted);
            }
            
            battlersThatParticipated.Remove(playerCurrentBattler);
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
            for (int i = 0; i < opponentCurrentBattler.moves.Count; i++)
            {
                if (opponentCurrentBattler.moves[i] == move)
                {
                    return i;
                }
            }

            Debug.LogWarning($"Could not find move {move.name} on the current opponent battler");
            return -1;
        }
        
        private int GetIndexOfMoveOnCurrentPlayer(Move move)
        {
            for (int i = 0; i < playerCurrentBattler.moves.Count; i++)
            {
                if (playerCurrentBattler.moves[i] == move)
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
            
            if (!playerCurrentBattler.isFainted)
            {
                foreach (var trigger in playerCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.EndOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            if (!opponentCurrentBattler.isFainted)
            {
                foreach (var trigger in opponentCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.EndOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(opponentCurrentBattler));
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

            if (!playerCurrentBattler.isFainted)
            {
                foreach (var trigger in playerCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.StartOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(playerCurrentBattler));
                        anyEffects = true;
                    }
                }
            }
            
            if (!opponentCurrentBattler.isFainted)
            {
                foreach (var trigger in opponentCurrentBattler.statusEffect.triggers)
                {
                    if (trigger.trigger == StatusEffectCaller.StartOfTurn)
                    {
                        trigger.EffectEvent.Invoke(new StatusEffectEventArgs(opponentCurrentBattler));
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
                { "trainerName", _opponentName },
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
                { "trainerName", _opponentName },
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
                turnItemQueue.Add(TurnItem.EndBattlePlayerWin);
            }
            else
            {
                turnItemQueue.Add(TurnItem.EndBattleOpponentWin);
            }
        }
        
        private void BeginEndBattleDialogue(bool isDefeated)
        {
            if (isDefeated)
            {
                if (trainerBattle)
                {
                    QueDialogue("All opponent Pokemon defeated!", DialogueBoxType.Event, "opponentDefeated", true);
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