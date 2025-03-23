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

        [SerializeField] private float shrinkEffectDelay;

        [Space]
        [Header("Main Readouts")]
        public int currentBattlerIndex;

        public int opponentBattlerIndex;

        [HideInInspector] public int currentDisplayBattlerIndex;
        [HideInInspector] public int opponentDisplayBattlerIndex;

        [Space]
        [Header("Other Readouts")]
        [SerializeField] public TurnStatus currentTurn = TurnStatus.Choosing;
        
        public BattleParty playerParty;
        
        public BattleParty opponentParty;
        
        [SerializeField] private EnemyAI enemyAI;
        
        [SerializeField] private Move playerMoveToDo;
        private int playerMoveToDoIndex;
        
        public Move enemyMoveToDo;
        
        [SerializeField] private bool playerHasChosenMove;
        
        [SerializeField] private bool hasDoneChoosingUpdate;
        
        [SerializeField] private bool hasSetupShowing;

        private Battler playerCurrentBattler => playerParty[currentBattlerIndex];

        private Battler opponentCurrentBattler => opponentParty[opponentBattlerIndex];

        public List<TurnItem> turnItemQueue = new List<TurnItem>();
        [SerializeField] private bool _currentlyRunningQueueItem = false;

        public List<Battler> battlersThatParticipated;

        public bool trainerBattle;

        private string _opponentName;

        private Vector3 _playerPos;
        private Quaternion _playerRotation;

        private bool _availableToEndTurnShowing;
        private bool _waitingToEndTurnEnding;

        private int _playerSwapIndex;
        
        public TurnItem playerAction;

        private TurnItem currentTurnItem;
        
        EventHandler<BattlerTookDamageArgs> opponentBattlerDefeated = null;
        EventHandler<int> playerBattlerLeveledUp = null;
        EventHandler<EvolutionData> playerBattlerEvolved = null;

        EventHandler<BattlerTookDamageArgs> playerBattlerDefeated = null;
        
        private Item _playerItemToUse;
        private int _battlerToUseItemOn;
        private bool _useItemOnPlayerParty;
        
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            //Loads relevant info like the opponent and player party
            trainerBattle = SceneLoader.GetVariable<bool>("trainerBattle");
            playerParty = new BattleParty(SceneLoader.GetVariable<Party>("playerParty"));
            opponentParty = new BattleParty(SceneLoader.GetVariable<Party>("opponentParty"));
            _playerPos = SceneLoader.GetVariable<Vector3>("playerPosition");
            _playerRotation = SceneLoader.GetVariable<Quaternion>("playerRotation");
            if (trainerBattle)
            {
                enemyAI = SceneLoader.GetVariable<EnemyAI>("enemyAI");
                _opponentName = SceneLoader.GetVariable<string>("opponentName");
            }

            ChangePlayerBattlerIndex(0, true);
            ChangeOpponentBattlerIndex(0, true);

            DialogueManager.instance.DialogueEnded += DialogueEnded;
            DialogueManager.instance.DialogueStarted += OnDialogueStarted;
            playerParty.PartyAllDefeated += PlayerPartyAllDefeated;
            opponentParty.PartyAllDefeated += OpponentPartyAllDefeated;

            opponentBattlerDefeated = (s, e) => BattlerFainted(e, opponentParty.party.Find(x => x == e.damaged));
            
            playerBattlerLeveledUp = (s, e) => BattlerLeveledUp(playerParty.party.Find(x => x == (Battler)s), e);
            playerBattlerEvolved = (s, e) => BattlerEvolved(playerParty.party.Find(x => x == (Battler)s), e.evolution);

            for (int i = 0; i < opponentParty.Count; i++)
            {
                opponentParty[i].OnFainted += opponentBattlerDefeated;
            }

            playerBattlerDefeated = (s, e) =>
            {
                if (playerCurrentBattler.isFainted)
                {
                    PlayerBattlerDied();
                }
            };
            
            for (int i = 0; i < playerParty.Count; i++)
            {
                playerParty[i].OnFainted += playerBattlerDefeated;
            }
            
            for (int i = 0; i < playerParty.Count; i++)
            {
                playerParty[i].OnLevelUp += playerBattlerLeveledUp;
            }
            
            for (int i = 0; i < playerParty.Count; i++)
            {
                playerParty[i].OnCanEvolve += playerBattlerEvolved;
            }
            
            // adds current battler to list of participating battlers
            battlersThatParticipated.Add(playerCurrentBattler);
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyOpen"));
        }

        private void OnDisable()
        {
            playerParty.PartyAllDefeated -= PlayerPartyAllDefeated;
            opponentParty.PartyAllDefeated -= OpponentPartyAllDefeated;
            DialogueManager.instance.DialogueEnded -= DialogueEnded;
            
            for (int i = 0; i < opponentParty.Count; i++)
            {
                opponentParty[i].OnFainted -= opponentBattlerDefeated;
            }
            for (int i = 0; i < playerParty.Count; i++)
            {
                playerParty[i].OnLevelUp -= playerBattlerLeveledUp;
            }
            for (int i = 0; i < playerParty.Count; i++)
            {
                playerParty[i].OnCanEvolve -= playerBattlerEvolved;
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

        private void BattlerLeveledUp(Battler battlerThatLeveled, int newLevel)
        {
            QueDialogue($"{battlerThatLeveled.name} reached level {newLevel}!", "leveledUp");
        }

        private void BattlerEvolved(Battler battlerThatEvolved, BattlerTemplate newTemplate)
        {
            QueDialogue($"{battlerThatEvolved.name} evolved into a {newTemplate.name}!", "evolved");
        }

        public void BattlerFainted(EventArgs e, Battler defeated)
        {
            uiManager.ShrinkOpponentBattler();
            
            turnItemQueue.Add(TurnItem.OpponentSwapBecauseFainted);
            turnItemQueue.Remove(TurnItem.OpponentMove);
            
            QueDialogue($"{defeated.name} Fainted!");

            int exp = ExperienceCalculator.GetExperienceFromDefeatingBattler(defeated, playerCurrentBattler, true,
                battlersThatParticipated.Count);

            foreach (Battler battler in battlersThatParticipated)
            {
                if (!battler.isFainted)
                {
                    QueDialogue($"{battler.name} gained {exp} experience points");
                    battler.GainExp(exp);
                }
            }

            playerCurrentBattler.EVs[0] += defeated.source.yields[1];
            playerCurrentBattler.EVs[1] += defeated.source.yields[2];
            playerCurrentBattler.EVs[2] += defeated.source.yields[3];
            playerCurrentBattler.EVs[3] += defeated.source.yields[4];
            playerCurrentBattler.EVs[4] += defeated.source.yields[5];
            playerCurrentBattler.EVs[5] += defeated.source.yields[6];
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
                    if (!hasDoneChoosingUpdate)
                    {
                        uiManager.ShowControlUI(true);
                        uiManager.ShowUI(true);
                        uiManager.UpdateBattlerMoveDisplays();
                        if (trainerBattle)
                        {
                            enemyAI.AIMethod(new AIMethodEventArgs(opponentCurrentBattler, opponentParty,
                                ExternalBattleData.Construct(this)));
                        }
                        else
                        {
                            EnemyAIMethods.WildPokemon(new AIMethodEventArgs(opponentCurrentBattler, opponentParty,
                                ExternalBattleData.Construct(this)));
                        }
                        hasDoneChoosingUpdate = true;
                    }
                    break;
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
                    _currentlyRunningQueueItem = true;
                    
                    currentTurnItem = turnItemQueue[0];
                    turnItemQueue.RemoveAt(0);
                    
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
                            PlayerUseItem(_battlerToUseItemOn, _useItemOnPlayerParty);
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
                        case TurnItem.PlayerParalysed:
                            PlayerParalysed();
                            break;
                        case TurnItem.CatchAttempt:
                            CatchAttempt();
                            break;
                        case TurnItem.Run:
                            RunRunAwayDialogue();
                            break;
                    }
                    
                    playerParty.CheckDefeatedStatus();
                    opponentParty.CheckDefeatedStatus();
                    
                    uiManager.UpdatePlayerBattlerDetails();
                    uiManager.UpdateOpponentBattlerDetails();
                }
                else if (!CurrentlyEndingTheBattle())
                {
                    EndTurnShowing();
                }
            }
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

        private void TurnQueueItemEnded()
        {
            _currentlyRunningQueueItem = false;
        }

        private void EndTurnShowing()
        {
            playerHasChosenMove = false;
            currentTurn = TurnStatus.Ending;
        }

        private void DialogueMoveUsed(string battlerUsed, string moveUsed, string battlerHit)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();
            variables.Add("battlerUsed", battlerUsed);
            variables.Add("moveUsed", moveUsed);
            variables.Add("battlerHit", battlerHit);
            
            QueDialogue(battlerUsedText, "moveUsed", true, variables);
        }

        private void TurnEnding()
        {
            if (!_waitingToEndTurnEnding)
            {
                hasDoneChoosingUpdate = false;
                hasSetupShowing = false;
                playerHasChosenMove = false;
                
                enemyMoveToDo = null;
                playerMoveToDo = null;

                _waitingToEndTurnEnding = true;
                EndTurnEnding();
            }
        }

        private void EndTurnEnding()
        {
            _waitingToEndTurnEnding = false;
            currentTurn = TurnStatus.Choosing;
        }

        //Public method used by the move UI buttons
        public void ChooseMove(int moveID)
        {
            playerMoveToDo = playerCurrentBattler.moves[moveID];
            playerMoveToDoIndex = moveID;
            playerHasChosenMove = true;
            playerAction = TurnItem.PlayerMove;
        }

        private void DialogueEnded(object sender, DialogueEndedEventArgs args)
        {
            bool swappedDialogue = false;

            switch (args.id)
            {
                case "run":
                    StartCoroutine(ExitBattleWin());
                    break;
                case "swap":
                    swappedDialogue = true;
                    PlayerSwappedBattler();
                    break;
                case "playerDefeated":
                    StartCoroutine(ExitBattleLoss());
                    break;
                case "opponentDefeated":
                    StartCoroutine(ExitBattleWin());
                    break;
            }
            
            if (_currentlyRunningQueueItem && !args.moreToGo && !swappedDialogue && !CurrentlyEndingTheBattle())
            {
                TurnQueueItemEnded();
            }
        }

        private void OnDialogueStarted(object sender, DialogueStartedEventArgs e)
        {
            switch (e.id)
            {
                case "evolved":
                    EvolutionEffect(e);
                    break;
            }
        }

        private void EvolutionEffect(DialogueStartedEventArgs e)
        {
            Battler evolvedBattler = playerCurrentBattler;
            foreach (var playerBattler in playerParty.party)
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

        public void UseItem(int battlerToUseOn, bool useOnPlayerParty)
        {
            playerAction = TurnItem.PlayerItem;
            playerHasChosenMove = true;
            _battlerToUseItemOn = battlerToUseOn;
            _useItemOnPlayerParty = useOnPlayerParty;
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
                if (_playerItemToUse.playerParty)
                {
                    UseItem(_playerItemToUse.targetIndex, true);
                }
                else
                {
                    UseItem(_playerItemToUse.targetIndex, false);
                }
            }
            else
            {
                uiManager.OpenUseItemOnBattler(_playerItemToUse);
                uiManager.UpdateItemBattlerButtons();
            }
        }

        private void CatchAttempt()
        {
            QueDialogue($"Threw a pokeball at {opponentCurrentBattler.name}!");

            if (ExperienceCalculator.Captured(opponentCurrentBattler, playerCurrentBattler, (PokeBall)_playerItemToUse))
            {
                uiManager.ShrinkOpponentBattler(true);
                QueDialogue($"Caught {opponentCurrentBattler.name}!");
                PartyManager.AddBattler(opponentCurrentBattler);
                turnItemQueue.Insert(0, TurnItem.EndBattlePlayerWin);
            }
            else
            {
                QueDialogue($"Failed to catch {opponentCurrentBattler.name}!");
            }
        }

        private void PlayerUseItem(int battlerToUseOn, bool useOnPlayerParty)
        {
            Battler battleBeingUsedOn = useOnPlayerParty ? playerParty[battlerToUseOn] : opponentParty[battlerToUseOn];
            
            ItemMethodEventArgs e = new ItemMethodEventArgs(battleBeingUsedOn, _playerItemToUse);
            
            _playerItemToUse.ItemMethod(e);
            
            Bag.Used(_playerItemToUse);
            
            QueDialogue($"You used {_playerItemToUse.name} on {battleBeingUsedOn.name}!");

            if (!e.success)
            {
                QueDialogue("But it failed!");
            }
        }

        public void ChooseToSwap(int newBattlerIndex)
        {
            if (_currentlyRunningQueueItem) // swapping mid turn showing aka after a battler faints
            {
                _playerSwapIndex = newBattlerIndex;
                QueDialogue($"You sent out {playerParty[newBattlerIndex].name}", "swap", true);
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
            
            AddParticipatedBattler(playerParty[_playerSwapIndex]);

            uiManager.UpdatePlayerBattlerDetails();
            
            QueDialogue($"Go ahead {playerParty[_playerSwapIndex].name}!", "sentOut", true);
        }

        private void OpponentSwitchBattler()
        {
            AISwitchEventArgs e =
                new AISwitchEventArgs(opponentBattlerIndex, opponentParty, ExternalBattleData.Construct(this));
            
            enemyAI.AISwitchMethod(e);

            ChangeOpponentBattlerIndex(e.newBattlerIndex, true);
            
            uiManager.UpdateOpponentBattlerDetails();
            
            QueDialogue($"Opponent sent out {opponentParty[e.newBattlerIndex].name}!", "opponentSentOut", true);
        }

        private void PlayerParalysed()
        {
            QueDialogue($"{playerCurrentBattler.name} is Paralysed! It is unable to move!");
        }

        private void OpponentParalysed()
        {
            QueDialogue($"The opponent {opponentCurrentBattler.name} is Paralysed! It is unable to move!");
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
            //You can add any animation calls for attacking here
            
            MoveMethodEventArgs e = new MoveMethodEventArgs(playerCurrentBattler, opponentCurrentBattler,
                playerMoveToDoIndex, playerMoveToDo, ExternalBattleData.Construct(this));
            
            DialogueMoveUsed(playerCurrentBattler.name, playerMoveToDo.name, opponentCurrentBattler.name);
            
            playerMoveToDo.MoveMethod(e);
            
            opponentCurrentBattler.TakeDamage(e.damageDealt, new BattlerDamageSource(playerCurrentBattler));
        }

        private void QueueMoves()
        {
            if (playerAction is TurnItem.CatchAttempt or TurnItem.PlayerSwap or TurnItem.PlayerItem)
            {
                AddOpponentMoveToQueue();
                // dont add the player move to queue because they are doing something else

                return;
            }

            float playerAdjustedSpeed = playerCurrentBattler.speed;
            float opponentAdjustedSpeed = opponentCurrentBattler.speed;

            if (playerCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                playerAdjustedSpeed /= 2;
            }
            
            if (opponentCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                opponentAdjustedSpeed /= 2;
            }
            
            
            if(playerAdjustedSpeed > opponentAdjustedSpeed)
            {
                //Player is faster
                AddPlayerMoveToQueue();
                AddOpponentMoveToQueue();
            }
            else
            {
                //Enemy is faster
                AddOpponentMoveToQueue();
                AddPlayerMoveToQueue();
            }
        }
        
        private void AddPlayerMoveToQueue()
        {
            if (playerCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Add(TurnItem.PlayerParalysed);
                }
                else
                {
                    turnItemQueue.Add(TurnItem.PlayerMove);
                }
            }
            else
            {
                turnItemQueue.Add(TurnItem.PlayerMove);
            }
        }

        private void AddOpponentMoveToQueue()
        {
            if (opponentCurrentBattler.statusEffect == Registry.GetStatusEffect("Paralysed"))
            {
                if (Random.Range(0, 4) == 0)
                {
                    turnItemQueue.Add(TurnItem.OpponentParalysed);
                }
                else
                {
                    turnItemQueue.Add(TurnItem.OpponentMove);
                }
            }
            else
            {
                turnItemQueue.Add(TurnItem.OpponentMove);
            }
        }

        private void DoEnemyMove()
        {
            //You can add any animation calls for attacking here

            int moveToDoIndex = GetIndexOfMoveOnCurrentEnemy(enemyMoveToDo);

            MoveMethodEventArgs e = new MoveMethodEventArgs(opponentCurrentBattler, playerCurrentBattler, moveToDoIndex,
                enemyMoveToDo, ExternalBattleData.Construct(this));
            
            DialogueMoveUsed(opponentCurrentBattler.name, enemyMoveToDo.name, playerCurrentBattler.name);
            
            enemyMoveToDo.MoveMethod(e);

            if (enemyMoveToDo.category != MoveCategory.Special)
            {
                playerCurrentBattler.TakeDamage(e.damageDealt, new BattlerDamageSource(opponentCurrentBattler));
            }
        }

        private void PlayerBattlerDied()
        {
            turnItemQueue.RemoveAll(item => item == TurnItem.PlayerMove);
            
            turnItemQueue.Add(TurnItem.PlayerSwapBecauseFainted);

            battlersThatParticipated.Remove(playerCurrentBattler);
        }

        public void RunFromBattle()
        {
            playerHasChosenMove = true;
            playerAction = TurnItem.Run;
        }

        private void RunRunAwayDialogue()
        {
            QueDialogue("Running Away!", "run");
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
                { "playerParty", playerParty },
                { "trainerName", _opponentName },
                { "playerPos", _playerPos },
                { "playerRotation", _playerRotation },
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
                { "playerParty", playerParty },
                { "trainerName", _opponentName },
                { "playerPos", _playerPos },
                { "playerRotation", _playerRotation },
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
                turnItemQueue.Clear();
                turnItemQueue.Add(TurnItem.EndBattlePlayerWin);
            }
            else
            {
                turnItemQueue.Clear();
                turnItemQueue.Add(TurnItem.EndBattlePlayerWin);
            }
        }
        
        private void BeginEndBattleDialogue(bool isDefeated)
        {
            if (isDefeated)
            {
                if (trainerBattle)
                {
                    QueDialogue("All opponent Pokemon defeated!", "opponentDefeated", true);
                }
                else
                {
                    StartCoroutine(ExitBattleWin());
                }
            }
            else
            {
                QueDialogue("All your Pokemon fainted, running!", "playerDefeated", true);
            }

            //TurnQueueItemEnded();
            turnItemQueue.Clear();
        }
    }
}