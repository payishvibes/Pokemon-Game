using Riptide;
using PokemonGame.General;
using PokemonGame.Networking;
using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.Battle
{
    public class PlayerBattleController : MonoBehaviour
    {
        public static PlayerBattleController Instance;

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private Battle battle;
        [SerializeField] private BattleUIManager uiManager;

        // temporary stored variables for level up screen
        private GameObject _currentLevelUpObj;
        [SerializeField] private LevelUpDisplay levelUpDisplayPrefab;

        public void ShowBattlerLeveled(OnLevelUpEventArgs args)
        {
            LevelUpDisplay display = Instantiate(levelUpDisplayPrefab, FindFirstObjectByType<Canvas>().transform);
            _currentLevelUpObj = display.gameObject;
            display.Init(args.oldStats, args.newStats);
        }
        
        public void FinishedViewingLevelUpScreen()
        {
            Destroy(_currentLevelUpObj);
            battle.TurnQueueItemEnded();
        }

        //Public method used by the move UI buttons
        public void ChooseMove(int moveID)
        {
            if (battle.localPlayerOne)
            {
                battle.PlayerOneChooseMove(moveID);
            }
            else
            {
                ClientSendPlayerMoveSelected(moveID);
            }
        }
        
        public void StartPickingBattlerToUseItemOn(Item item)
        {
            if (item.lockedTarget)
            {
                battle.PlayerOneUseItem(item, item.targetIndex, item.userParty);
            }
            else
            {
                uiManager.OpenUseItemOnBattler(item);
                uiManager.UpdateItemBattlerButtons();
            }
        }

        public void PlayerUseItem(Item item, int targetIndex, bool userParty)
        {
            battle.PlayerOneUseItem(item, targetIndex, userParty);
        }

        public void PlayerPickedPokeBall(PokeBall ball)
        {
            battle.PlayerOnePickedPokeBall(ball);
        }

        public void PlayerChooseToSwap(int battlerIndex)
        {
            if (battle.localPlayerOne)
            {
                battle.PlayerOneChooseToSwap(battlerIndex);
            }
            else
            {
                ClientSendPlayerSwapSelected(battlerIndex);
            }
        }

        private void ClientSendPlayerMoveSelected(int moveIndex)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerMessageId.MoveSelected);
            message.AddInt(moveIndex);

            BattleNetworkManager.Instance.Client.Send(message);
        }

        private void ClientSendPlayerSwapSelected(int battlerIndex)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerMessageId.MoveSelected);
            message.AddInt(battlerIndex);

            BattleNetworkManager.Instance.Client.Send(message);
        }
    }
}