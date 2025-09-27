using System;
using System.Linq;
using PokemonGame.Game;
using PokemonGame.Networking;
using Riptide;

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

        [Header("Battle Components:")]
        [SerializeField] private BattleUIManager uiManager;
        [SerializeField] private PlayerBattleController playerBattleController;
        
        [Space]
        [Header("Assignments")] 
        [SerializeField] private TextAsset battlerUsedText;
        [SerializeField] private ParticleSystem spawnEffect;
        [SerializeField] private float shrinkEffectDelay;

        [Space]
        [Header("Readouts")]
        public int playerOneBattlerIndex;
        public int playerTwoBattlerIndex;
        [SerializeField] public TurnStatus currentTurn = TurnStatus.Choosing;

        public bool localPlayerOne;
        
        public BattleParty partyOne;
        public BattleParty partyTwo;
        
        [SerializeField] private EnemyAI enemyAI;
        
        [HideInInspector] public int currentDisplayBattlerIndex;
        [HideInInspector] public int playerTwoDisplayBattlerIndex;
        
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

        public Battler PlayerOneBattler => playerOneCurrentBattler;
        
        /// <summary>
        /// the playerTwos current battler
        /// </summary>
        private Battler playerTwoCurrentBattler => partyTwo[playerTwoBattlerIndex];

        public Battler PlayerTwoBattler => playerTwoCurrentBattler;
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
        public TurnItem playerOneAction;
        // the action the player has chosen to perform
        public TurnItem playerTwoAction;
        
        /// <summary>
        /// the current turn item
        /// </summary>
        public TurnItem currentTurnItem;
        
        // events
        private EventHandler<OnLevelUpEventArgs> _playerOneBattlerLeveledUp = null;
        private EventHandler<EvolutionData> _playerOneBattlerEvolved = null;

        public bool onlineBattle;
        
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

            if (!onlineBattle)
            {
                // adds current battler to list of participating battlers
                playerOneBattlersThatParticipated.Add(playerOneCurrentBattler);
            }
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyOpen"));
        }

        private void LoadStartingVariables()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Loads relevant info like the playerTwo and player party
            onlineBattle = SceneLoader.GetVariable<bool>("online");
            trainerBattle = SceneLoader.GetVariable<bool>("trainerBattle");
            if (trainerBattle)
            {
                enemyAI = SceneLoader.GetVariable<EnemyAI>("enemyAI");
                _playerTwoName = SceneLoader.GetVariable<string>("opponentName");
            }

            if (onlineBattle)
            {
                localPlayerOne = BattleNetworkManager.Instance.IsHost;
                List<NetworkPlayer> players = BattleNetworkManager.Instance.Players.Values.ToList();
                partyOne = new BattleParty(players[0].Party);
                partyTwo = new BattleParty(players[1].Party);
            }
            else
            {
                partyOne = new BattleParty(SceneLoader.GetVariable<Party>("partyOne"));
                partyTwo = new BattleParty(SceneLoader.GetVariable<Party>("partyTwo"));
                localPlayerOne = true;
            }
        }

        private void HookEvents()
        {
            DialogueManager.instance.DialogueStarted += OnDialogueStarted;
            DialogueManager.instance.DialogueEnded += DialogueEnded;
            partyOne.PartyAllDefeated += PlayerOnePartyAllDefeated;
            partyTwo.PartyAllDefeated += PlayerTwoPartyAllDefeated;

            if (!onlineBattle)
            {
                _playerOneBattlerLeveledUp = (s, e) => BattlerLeveledUp(partyOne.party.Find(x => x == (Battler)s), e);
                _playerOneBattlerEvolved = (s, e) => BattlerEvolved(partyOne.party.Find(x => x == (Battler)s), e.evolution);
            }
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnLevelUp += _playerOneBattlerLeveledUp;
            }
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnCanEvolve += _playerOneBattlerEvolved;
            }
        }

        private void OnDisable()
        {
            partyOne.PartyAllDefeated -= PlayerOnePartyAllDefeated;
            partyTwo.PartyAllDefeated -= PlayerTwoPartyAllDefeated;
            DialogueManager.instance.DialogueEnded -= DialogueEnded;
            
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnLevelUp -= _playerOneBattlerLeveledUp;
            }
            for (int i = 0; i < partyOne.Count; i++)
            {
                partyOne[i].OnCanEvolve -= _playerOneBattlerEvolved;
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

        private void BattlerLevelUpEvent(string newBattlerName, int newLevel)
        {
            QueDialogue($"{newBattlerName} reached level {newLevel}!", DialogueBoxType.Narration, "leveledUp");
        }

        private void BattlerLeveledUp(Battler battlerThatLeveled, OnLevelUpEventArgs args)
        {
            Debug.Log("Queuing player level up");
            
            InsertTurnItem(TurnItemType.PlayerOneLevelUp, new List<object>()
            {
                battlerThatLeveled,
                args
            });
        }

        private void BattlerEvolved(Battler battlerThatEvolved, BattlerTemplate newTemplate)
        {
            QueDialogue($"{battlerThatEvolved.name} evolved into a {newTemplate.name}!", DialogueBoxType.Narration, "evolved");
        }
        
        private IEnumerator ShowMove(MoveMethodEventArgs args)
        {
            if (args.missed)
            {
                yield return null;
            }
            
            args.move.MoveMethod(args);
            args.movePPData.MoveWasUsed();
            
            DialogueMoveEffectiveness(args);
            
            CheckFaintedBattlers();
            
            yield return new WaitForSeconds(1);
            
            StartDialogue();
            
            if (args.effectiveIndex == 0 && !args.crit)
            {
                TurnQueueItemEnded();
            }
        }

        private void CheckFaintedBattlers()
        {
            if (playerOneCurrentBattler.isFainted)
            {
                PlayerOneBattlerFainted();
            }
            
            if (playerTwoCurrentBattler.isFainted)
            {
                PlayerTwoBattlerFainted();
            }
        }

        private void Update()
        {
            if (playerOneAction != null && playerTwoAction != null)
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
                if (trainerBattle && !onlineBattle)
                {
                    enemyAI.AIMethod(new AIMethodEventArgs(playerTwoCurrentBattler, partyTwo,
                        ExternalBattleData.Construct(this)));
                }
                else if(!onlineBattle)
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

                if (IsNotOnlineHost())
                {
                    return;
                }
                
                QueueTurnItem(TurnItemType.StartDelay);
                
                if (playerOneAction.Type is not TurnItemType.PlayerOneMove)
                {
                    turnItemQueue.Add(playerOneAction);
                }
                
                if (playerTwoAction.Type is not TurnItemType.PlayerTwoMove)
                {
                    turnItemQueue.Add(playerTwoAction);
                }
                
                QueueTurnItem(TurnItemType.StartOfTurnStatusEffects);
                QueueMoves();
                QueueTurnItem(TurnItemType.EndOfTurnStatusEffects);
            }
            
            if (IsNotOnlineHost())
            {
                return;
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
            if (onlineBattle)
            {
                _acknowledgedPlayers = new List<ushort>();
            }
            _currentlyRunningQueueItem = true;

            currentTurnItem = turnItemQueue[0];
            turnItemQueue.RemoveAt(0);

            Debug.Log($"Starting turn item: {currentTurnItem.Type}");

            switch (currentTurnItem.Type)
            {
                case TurnItemType.StartDelay:
                    StartCoroutine(TurnStartDelay());
                    break;
                case TurnItemType.PlayerOneMove:
                    DoPlayerOneMove(playerOneCurrentBattler.moves[(int)playerOneAction.Variables[0]]);
                    break;
                case TurnItemType.PlayerTwoMove:
                    DoPlayerTwoMove(playerTwoCurrentBattler.moves[(int)playerTwoAction.Variables[0]]);
                    break;
                case TurnItemType.EndBattlePlayerOneWin:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnEndBattle(true);
                    else
                        BeginEndBattleDialogue(true);
                    BeginEndBattleDialogue(true);
                    break;
                case TurnItemType.EndBattlePlayerTwoWin:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnEndBattle(false);
                    else
                        BeginEndBattleDialogue(false);
                    break;
                case TurnItemType.PlayerOneSwap:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerSwap(true, (int)currentTurnItem.Variables[^1]);
                    else
                        PlayerOneSwappedBattler((int)currentTurnItem.Variables[^1]);
                    break;
                case TurnItemType.PlayerOneSwapBecauseFainted:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerSwapBecauseFainted(true);
                    else
                        BeginSwapPlayerOneBattler();
                    break;
                case TurnItemType.PlayerOneItem:
                    PlayerOneUseItemEvent((Item)currentTurnItem.Variables[0], (int)currentTurnItem.Variables[1], (bool)currentTurnItem.Variables[2]);
                    break;
                case TurnItemType.PlayerTwoItem:
                    PlayerTwoUseItemEvent((Item)currentTurnItem.Variables[0], (int)currentTurnItem.Variables[1], (bool)currentTurnItem.Variables[2]);
                    break;
                case TurnItemType.PlayerOneLevelUp:
                    BattlerLevelUpEvent(((Battler)currentTurnItem.Variables[0]).name, ((OnLevelUpEventArgs)currentTurnItem.Variables[1]).newLevel);
                    break;
                case TurnItemType.PlayerTwoSwap:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerSwap(false, (int)currentTurnItem.Variables[^1]);
                    else
                        PlayerTwoSwappedBattler((int)currentTurnItem.Variables[^1]);
                    break;
                case TurnItemType.PlayerTwoSwapBecauseFainted:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerSwapBecauseFainted(false);
                    else
                        BeginSwapPlayerTwoBattler();
                    break;
                case TurnItemType.StartOfTurnStatusEffects:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnStartOfTurnStatusEffects();
                    else
                        RunStartOfTurnStatusEffects();
                    break;
                case TurnItemType.EndOfTurnStatusEffects:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnEndOfTurnStatusEffects();
                    else
                        RunEndOfTurnStatusEffects();
                    break;
                case TurnItemType.PlayerTwoParalysed:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerParalysed(false);
                    else
                        PlayerTwoParalysed();
                    break;
                case TurnItemType.PlayerTwoAsleep:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerAsleep(false);
                    else
                        PlayerTwoAsleep();
                    break;
                case TurnItemType.PlayerOneParalysed:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerParalysed(true);
                    else
                        PlayerOneParalysed();
                    break;
                case TurnItemType.PlayerOneAsleep:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerAsleep(true);
                    else
                        PlayerOneAsleep();
                    break;
                case TurnItemType.CatchAttempt:
                    CatchAttempt();
                    break;
                case TurnItemType.Run:
                    RunAwayDialogue();
                    break;
                case TurnItemType.PlayerMissed:
                    if (onlineBattle)
                        BattleNetworkManager.Instance.ServerSendTurnPlayerMissed();
                    else
                        MoveMissed();
                    break;
            }

            uiManager.UpdatePlayerOneBattlerDetails();
            uiManager.UpdatePlayerTwoBattlerDetails();
        }

        private bool CurrentlyEndingTheBattle()
        {
            return currentTurnItem?.Type is TurnItemType.EndBattlePlayerTwoWin or TurnItemType.EndBattlePlayerOneWin or TurnItemType.Run;
        }

        private IEnumerator TurnStartDelay()
        {
            yield return new WaitForSeconds(1);
            EndTurnItem();
        }

        public void QueueTurnItem(TurnItemType type, List<object> variables)
        {
            TurnItem item = new TurnItem(type, variables);
            turnItemQueue.Add(item);
        }

        private void QueueTurnItem(TurnItemType type)
        {
            if (IsNotOnlineHost())
            {
                return;
            }
            TurnItem item = new TurnItem(type);
            turnItemQueue.Add(item);
        }

        private void InsertTurnItem(TurnItemType type)
        {
            if (IsNotOnlineHost())
            {
                return;
            }
            TurnItem item = new TurnItem(type);
            turnItemQueue.Insert(0, item);
        }

        private void InsertTurnItem(TurnItemType type, List<object> variables)
        {
            if (IsNotOnlineHost())
            {
                return;
            }
            TurnItem item = new TurnItem(type, variables);
            turnItemQueue.Insert(0, item);
        }
        
        public void TurnQueueItemEnded()
        {
            if (onlineBattle)
            {
                ClientSendTurnItemEnd();
            }
            else
            {
                EndTurnItem();
            }
        }

        private void ClientSendTurnItemEnd()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerMessageId.TurnItemEnd);
            BattleNetworkManager.Instance.Client.Send(message);
        }

        private List<ushort> _acknowledgedPlayers = new List<ushort>();

        public void ServerTurnItemEnd(ushort id)
        {
            if (!_acknowledgedPlayers.Contains(id))
            {
                _acknowledgedPlayers.Add(id);
            }
            
            if (_acknowledgedPlayers.Count >= BattleNetworkManager.Instance.MaxPlayerCount)
            {
                EndTurnItem();
            }
        }
        
        private void EndTurnItem()
        {
            Debug.Log("ending turn item");
            _currentlyRunningQueueItem = false;
        }
        
        public void EndTurnShowing()
        {
            playerOneAction = null;
            playerTwoAction = null;
            currentTurn = TurnStatus.Ending;
            
            if (IsOnlineHost())
            {
                BattleNetworkManager.Instance.ServerSendTurnSequenceEnded();
            }
        }

        private void DialogueMoveUsed(MoveMethodEventArgs e, bool player)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("battlerUsed", e.attacker.name);
            variables.Add("moveUsed", e.move.name);
            variables.Add("battlerHit", e.target.name);

            if (player)
            {
                QueDialogue(battlerUsedText, DialogueBoxType.Event, "moveUsed", true, variables);
            }
            else
            {
                QueDialogue(battlerUsedText, DialogueBoxType.Event, "moveUsed", true, variables);
            }
            
            ForceStopNextQueued();
        }

        private void DialogueMoveEffectiveness(MoveMethodEventArgs e)
        {
            switch (e.effectiveIndex)
            {
                case 1:
                    QueDialogue("Its Not Very Effective...", DialogueBoxType.Event, "generalFinishing");
                    break;
                case 2:
                    QueDialogue("Its Super Effective!", DialogueBoxType.Event, "generalFinishing");
                    break;
                case 3:
                    QueDialogue($"{e.target.name} is immune!", DialogueBoxType.Event, "generalFinishing");
                    break;
            }

            if (e.crit)
            {
                QueDialogue("A Critical Hit!", DialogueBoxType.Event, "generalFinishing");
            }
        }

        private void TurnEnding()
        {
            hasDoneChoosingUpdate = false;
            hasSetupShowing = false;
            
            EndTurnEnding();
        }

        private void EndTurnEnding()
        {
            currentTurn = TurnStatus.Choosing;
        }

        public void PlayerOneChooseMove(int moveID)
        {
            playerOneAction = new TurnItem(TurnItemType.PlayerOneMove, new List<object>()
            {
                moveID,
            });
        }

        public void PlayerTwoChooseMove(int moveID)
        {
            playerTwoAction = new TurnItem(TurnItemType.PlayerTwoMove, new List<object>()
            {
                moveID,
            });
        }

        public void RemoveTurnItemType(TurnItemType turnItemToRemove)
        {
            turnItemQueue.RemoveAll(item => item.Type == turnItemToRemove);
        }

        private void DialogueEnded(object sender, DialogueEndedEventArgs args)
        {
            switch (args.id)
            {
                case "run":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "playerOneSwap":
                    PlayerOneSwappedBattler((int)currentTurnItem.Variables[^1]);
                    break;
                case "playerTwoSwap":
                    PlayerTwoSwappedBattler((int)currentTurnItem.Variables[^1]);
                    break;
                case "playerDefeated":
                    StartCoroutine(ExitBattleLoss());
                    break;
                case "playerTwoDefeated":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "leveledUp":
                    playerBattleController.ShowBattlerLeveled((OnLevelUpEventArgs)currentTurnItem.Variables[1]);
                    break;
                case "moveUsed":
                    StartCoroutine(ShowMove((MoveMethodEventArgs)currentTurnItem.Variables[^1]));
                    break;
                case "playerTwoFainted":
                    Debug.Log("finished fainted dialogue");
                    if (!args.moreToGo || !DialogueManager.instance.dialogueIsPlaying)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "playerOneFainted":
                    Debug.Log("finished fainted dialogue");
                    if (!args.moreToGo || !DialogueManager.instance.dialogueIsPlaying)
                    {
                        TurnQueueItemEnded();
                    }
                    break;
                case "generalFinishing":
                    if (!args.moreToGo || !DialogueManager.instance.dialogueIsPlaying)
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
                case "playerOneFainted":
                    uiManager.ShrinkPlayerOneBattler();
                    break;
                case "playerTwoFainted":
                    uiManager.ShrinkPlayerTwoBattler();
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
        
        public void PlayerOneUseItem(Item item, int battlerToUseOn, bool useOnUserParty)
        {
            playerOneAction = new TurnItem(TurnItemType.PlayerOneItem, new List<object>()
            {
                item,
                battlerToUseOn,
                useOnUserParty
            });
            uiManager.Back();
        }
        
        public void PlayerTwoUseItem(Item item, int battlerToUseOn, bool useOnUserParty)
        {
            playerTwoAction = new TurnItem(TurnItemType.PlayerTwoItem, new List<object>()
            {
                item,
                battlerToUseOn,
                useOnUserParty
            });
            uiManager.Back();
        }
        
        public void PlayerOnePickedPokeBall(PokeBall ball)
        {
            playerOneAction = new TurnItem(TurnItemType.CatchAttempt, new List<object>()
            {
                ball
            });
            Bag.Used(ball);
        }

        private void CatchAttempt()
        {
            QueDialogue($"Threw a pokeball at {playerTwoCurrentBattler.name}!", DialogueBoxType.Event);

            if (ExperienceCalculator.Captured(playerTwoCurrentBattler, playerOneCurrentBattler, (PokeBall)playerOneAction.Variables[0]))
            {
                uiManager.ShrinkPlayerTwoBattler(true);
                QueDialogue($"Caught {playerTwoCurrentBattler.name}!", DialogueBoxType.Event, "generalFinishing");
                PartyManager.AddBattler(playerTwoCurrentBattler);
                InsertTurnItem(TurnItemType.EndBattlePlayerOneWin);
            }
            else
            {
                QueDialogue($"Failed to catch {playerTwoCurrentBattler.name}!", DialogueBoxType.Event, "generalFinishing");
            }
        }

        private void PlayerOneUseItemEvent(Item itemToUse, int battlerToUseOn, bool useOnUserParty)
        {
            Battler battlerBeingUsedOn = useOnUserParty ? partyOne[battlerToUseOn] : partyTwo[battlerToUseOn];
            
            ItemMethodEventArgs e = new ItemMethodEventArgs(battlerBeingUsedOn, itemToUse);
            
            itemToUse.ItemMethod(e);
            
            Bag.Used(itemToUse);
            
            QueDialogue($"Player One used {itemToUse.name} on {battlerBeingUsedOn.name}!", DialogueBoxType.Event, "generalFinishing");

            if (!e.success)
            {
                QueDialogue("But it failed!", DialogueBoxType.Event, "generalFinishing");
            }
        }

        private void PlayerTwoUseItemEvent(Item itemToUse, int battlerToUseOn, bool useOnUserParty)
        {
            Battler battlerBeingUsedOn = useOnUserParty ? partyTwo[battlerToUseOn] : partyOne[battlerToUseOn];
            
            ItemMethodEventArgs e = new ItemMethodEventArgs(battlerBeingUsedOn, itemToUse);
            
            itemToUse.ItemMethod(e);
            
            Bag.Used(itemToUse);
            
            QueDialogue($"Player Two used {itemToUse.name} on {battlerBeingUsedOn.name}!", DialogueBoxType.Event, "generalFinishing");

            if (!e.success)
            {
                QueDialogue("But it failed!", DialogueBoxType.Event, "generalFinishing");
            }
        }

        public void PlayerOneChooseToSwap(int newBattlerIndex)
        {
            if (_currentlyRunningQueueItem) // swapping mid turn showing aka after a battler faints
            {
                currentTurnItem.Variables.Add(newBattlerIndex);
                if (!onlineBattle)
                {
                    AddParticipatedBattler(partyOne[newBattlerIndex]);
                }
                QueDialogue($"Player One sent out {partyOne[newBattlerIndex].name}", DialogueBoxType.Event, "playerOneSwap");
            
                if (IsOnlineHost())
                {
                    BattleNetworkManager.Instance.ServerSendTurnPlayerSwap(true, newBattlerIndex, true);
                }
            }
            else // player chose to swap as their move
            {
                playerOneAction = new TurnItem(TurnItemType.PlayerOneSwap, new List<object>()
                {
                    newBattlerIndex
                });
            }
        }
        
        public void PlayerTwoChooseToSwap(int newBattlerIndex)
        {
            if (_currentlyRunningQueueItem) // swapping mid turn showing aka after a battler faints
            {
                currentTurnItem.Variables.Add(newBattlerIndex);
                QueDialogue($"Player Two sent out {partyTwo[newBattlerIndex].name}", DialogueBoxType.Event, "playerTwoSwap");
                
                if (IsOnlineHost())
                {
                    BattleNetworkManager.Instance.ServerSendTurnPlayerSwap(false, newBattlerIndex, true);
                }
            }
            else // player chose to swap as their move
            {
                playerTwoAction = new TurnItem(TurnItemType.PlayerTwoSwap, new List<object>()
                {
                    newBattlerIndex
                });
            }
        }

        public void BeginSwapPlayerOneBattler()
        {
            if (localPlayerOne)
            {
                uiManager.SwitchBattlerBecauseOfDeath();
            }
        }

        public void BeginSwapPlayerTwoBattler()
        {
            if (!onlineBattle)
            {
                AISwitchEventArgs e =
                    new AISwitchEventArgs(playerTwoBattlerIndex, partyTwo, ExternalBattleData.Construct(this));
            
                enemyAI.AISwitchMethod(e);
                    
                PlayerTwoChooseToSwap(e.newBattlerIndex);
            }
            else if (!localPlayerOne)
            {
                uiManager.SwitchBattlerBecauseOfDeath();
            }
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

        public void PlayerOneSwappedBattler(int playerOneSwapIndex)
        {
            ChangePlayerOneBattlerIndex(playerOneSwapIndex);
            
            if (!onlineBattle)
            {
                AddParticipatedBattler(partyOne[playerOneSwapIndex]);
            }

            uiManager.UpdatePlayerOneBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"Go ahead {partyOne[playerOneSwapIndex].name}!", DialogueBoxType.Event, "generalFinishing", true);
        }

        public void PlayerTwoSwappedBattler(int playerTwoSwapIndex)
        {
            ChangePlayerTwoBattlerIndex(playerTwoSwapIndex);
            
            uiManager.UpdatePlayerTwoBattlerDetails();
            uiManager.ForceHealthSet();
            
            QueDialogue($"Go ahead {partyTwo[playerTwoSwapIndex].name}!", DialogueBoxType.Event, "generalFinishing", true);
        }

        public void PlayerOneParalysed()
        {
            QueDialogue($"{playerOneCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        public void PlayerTwoParalysed()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Paralysed! It is unable to move!", DialogueBoxType.Event);
        }

        public void PlayerOneAsleep()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        public void PlayerTwoAsleep()
        {
            QueDialogue($"The playerTwo {playerTwoCurrentBattler.name} is Asleep", DialogueBoxType.Event);
        }

        public void MoveMissed()
        {
            QueDialogue($"But it missed!", DialogueBoxType.Event, "generalFinishing");
        }

        public void AddParticipatedBattler(Battler battlerToParticipate)
        {
            if (!onlineBattle)
            {
                if (!playerOneBattlersThatParticipated.Contains(battlerToParticipate))
                {
                    playerOneBattlersThatParticipated.Add(battlerToParticipate);
                }
            }
        }

        public void DoPlayerOneMove(Move playerOneMoveToDo)
        {
            bool able = true;
            bool missed = false;
            
            if (playerOneCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    InsertTurnItem(TurnItemType.PlayerOneParalysed);
                    able = false;
                }
            }else if (playerOneCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                InsertTurnItem(TurnItemType.PlayerOneAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            // actually has like a usable accuracy
            if (playerOneMoveToDo.accuracy != 0 && !Mathf.Approximately(playerOneMoveToDo.accuracy, 1))
            {
                missed = Random.Range(1, 101) > playerOneMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            int moveToDoIndex = GetIndexOfMovePlayerOne(playerOneMoveToDo);
            
            MoveMethodEventArgs e = new MoveMethodEventArgs(playerOneCurrentBattler, playerTwoCurrentBattler, playerOneMoveToDo, 
                playerOneCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));

            e.damageDealt = MovesMethods.CalculateDamage(playerOneMoveToDo, PlayerOneBattler, PlayerTwoBattler,
                out e.effectiveIndex, out e.crit);

            e.missed = missed;
            
            if (onlineBattle)
            {
                BattleNetworkManager.Instance.ServerSendTurnPlayerMove(true, e);
            }
            PlayAuthoritativeMove(e, true);

            if (missed)
            {
                Debug.Log("Missed");
                InsertTurnItem(TurnItemType.PlayerMissed);
            }
        }
        
        public void DoPlayerTwoMove(Move playerTwoMoveToDo)
        {
            bool able = true;
            bool missed = false;
            
            if (playerTwoCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    InsertTurnItem(TurnItemType.PlayerTwoParalysed);
                    able = false;
                }
            }else if (playerTwoCurrentBattler.statusEffect == Registry.GetStatusEffect("Asleep"))
            {
                InsertTurnItem(TurnItemType.PlayerTwoAsleep);
                able = false;
            }

            if (!able)
            {
                return;
            }

            // actually has like a usable accuracy
            if (playerTwoMoveToDo.accuracy != 0 && !Mathf.Approximately(playerTwoMoveToDo.accuracy, 1))
            {
                missed = Random.Range(1, 101) > playerTwoMoveToDo.accuracy * 100;
            }
            
            uiManager.ShowHealthUI(true);
            
            int moveToDoIndex = GetIndexOfMovePlayerTwo(playerTwoMoveToDo);
            
            MoveMethodEventArgs e = new MoveMethodEventArgs(playerTwoCurrentBattler, playerOneCurrentBattler,
                playerTwoMoveToDo, playerTwoCurrentBattler.movePpInfos[moveToDoIndex], ExternalBattleData.Construct(this));
            
            e.damageDealt = MovesMethods.CalculateDamage(playerTwoMoveToDo, PlayerTwoBattler, PlayerOneBattler,
                out e.effectiveIndex, out e.crit);
            
            e.missed = missed;
            
            if (onlineBattle)
            {
                BattleNetworkManager.Instance.ServerSendTurnPlayerMove(false, e);
            }
            PlayAuthoritativeMove(e, false);
            
            if (missed)
            {
                Debug.Log("Missed");
                InsertTurnItem(TurnItemType.PlayerMissed);
            }
        }

        public void PlayAuthoritativeMove(MoveMethodEventArgs e, bool player)
        {
            if (IsNotOnlineHost())
            {
                if (player)
                {
                    currentTurnItem = new TurnItem(TurnItemType.PlayerOneMove);
                }
                else
                {
                    currentTurnItem = new TurnItem(TurnItemType.PlayerTwoMove);
                }
            }
            
            DialogueMoveUsed(e, player);
            currentTurnItem.Variables.Add(e);
        }

        private void QueueMoves()
        {
            if (playerOneAction.Type is not TurnItemType.PlayerOneMove && playerTwoAction.Type is TurnItemType.PlayerTwoMove)
            {
                AddPlayerTwoMoveToQueue();
                // dont add the player move to queue because they are doing something else

                return;
            }
            
            if (playerOneAction.Type is TurnItemType.PlayerOneMove && playerTwoAction.Type is not TurnItemType.PlayerTwoMove)
            {
                AddPlayerOneMoveToQueue();
                // dont add the player move to queue because they are doing something else

                return;
            }
            
            if (playerOneAction.Type is not TurnItemType.PlayerOneMove && playerTwoAction.Type is not TurnItemType.PlayerTwoMove)
            {
                // dont add the players move to queue because they are doing something else
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

            Move playerOneMoveToDo = playerOneCurrentBattler.moves[(int)playerOneAction.Variables[0]];
            Move playerTwoMoveToDo = playerTwoCurrentBattler.moves[(int)playerTwoAction.Variables[0]];
            
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
            turnItemQueue.Add(playerOneAction);
        }

        private void AddPlayerTwoMoveToQueue()
        {
            turnItemQueue.Add(playerTwoAction);
        }

        private void PlayerOneBattlerFainted()
        {
            Debug.Log("Player One Fainted");
            QueDialogue($"{playerOneCurrentBattler.name} Fainted!", DialogueBoxType.Event, "playerOneFainted");
            
            partyOne.CheckDefeatedStatus();
            
            if (!partyOne.defeated)
            {
                InsertTurnItem(TurnItemType.PlayerOneSwapBecauseFainted);
            }
            
            turnItemQueue.RemoveAll(item => item.Type == TurnItemType.PlayerOneMove);
            
            if (!onlineBattle)
            {
                playerOneBattlersThatParticipated.Remove(playerOneCurrentBattler);
            }
        }
        
        public void PlayerTwoBattlerFainted()
        {
            if (!onlineBattle)
            {
                int exp = ExperienceCalculator.GetExperienceFromDefeatingBattler(playerTwoCurrentBattler, playerOneCurrentBattler, true,
                    playerOneBattlersThatParticipated.Count);
                
                foreach (Battler battler in playerOneBattlersThatParticipated)
                {
                    if (!battler.isFainted)
                    {
                        QueDialogue($"{battler.name} gained {exp} experience points", DialogueBoxType.Event, "generalFinishing");
                        battler.GainExp(exp);
                    }
                }
            }
            
            Debug.Log("Player Two Fainted");
            QueDialogue($"{playerTwoCurrentBattler.name} Fainted!", DialogueBoxType.Event, "playerTwoFainted");
            
            partyTwo.CheckDefeatedStatus();
            
            if (!partyTwo.defeated)
            {
                InsertTurnItem(TurnItemType.PlayerTwoSwapBecauseFainted);
            }
            turnItemQueue.RemoveAll(item => item.Type == TurnItemType.PlayerTwoMove);
            
            if (!onlineBattle)
            {
                playerOneCurrentBattler.EVs.maxHealth += playerTwoCurrentBattler.source.yields.maxHealth;
                playerOneCurrentBattler.EVs.attack += playerTwoCurrentBattler.source.yields.attack;
                playerOneCurrentBattler.EVs.defense += playerTwoCurrentBattler.source.yields.defense;
                playerOneCurrentBattler.EVs.specialAttack += playerTwoCurrentBattler.source.yields.specialAttack;
                playerOneCurrentBattler.EVs.specialDefense += playerTwoCurrentBattler.source.yields.specialDefense;
                playerOneCurrentBattler.EVs.speed += playerTwoCurrentBattler.source.yields.speed;
            }
        }
        
        public void RunFromBattle()
        {
            playerOneAction = new TurnItem(TurnItemType.Run);
        }

        private void RunAwayDialogue()
        {
            QueDialogue("Running Away!", DialogueBoxType.Event, "run");
        }

        public int GetIndexOfMovePlayerTwo(Move move)
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
        
        public int GetIndexOfMovePlayerOne(Move move)
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
        
        public void RunEndOfTurnStatusEffects()
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
        
        public void RunStartOfTurnStatusEffects()
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
                QueueTurnItem(TurnItemType.EndBattlePlayerOneWin);
            }
            else
            {
                QueueTurnItem(TurnItemType.EndBattlePlayerTwoWin);
            }
        }
        
        public void BeginEndBattleDialogue(bool isDefeated)
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

        private bool IsNotOnlineHost()
        {
            if (onlineBattle)
            {
                return !BattleNetworkManager.Instance.IsHost;
            }

            return false;
        }

        private bool IsOnlineHost()
        {
            if (onlineBattle)
            {
                return BattleNetworkManager.Instance.IsHost;
            }

            return false;
        }
    }
}