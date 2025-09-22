using Riptide;

namespace PokemonGame.Networking
{
    using Battle;
    
    public class MessageHandlers
    {
        [MessageHandler((ushort)ClientToServerMessageId.ConnectionInfo)]
        private static void ServerConnectionInfo(ushort fromPlayerId, Message message)
        {
            BattleNetworkManager.Instance.ServerConnectionInfo(fromPlayerId, message.GetString(), message.GetInt());

        }

        [MessageHandler((ushort)ClientToServerMessageId.AllPartiesReceived)]
        private static void ServerAllPartiesReceived(ushort fromPlayerId, Message message)
        {
            BattleNetworkManager.Instance.ServerAllPartiesReceived();
        }

        [MessageHandler((ushort)ClientToServerMessageId.TurnItemEnd)]
        private static void ServerTurnItemEnd(ushort fromPlayerId, Message message)
        {
            Battle.Singleton.ServerTurnItemEnd();
        }

        [MessageHandler((ushort)ClientToServerMessageId.MoveSelected)]
        private static void ServerMoveSelected(ushort fromPlayerId, Message message)
        {
            Battle.Singleton.PlayerTwoChooseMove(message.GetInt());
        }

        [MessageHandler((ushort)ClientToServerMessageId.SwapSelected)]
        private static void ServerSwapSelected(ushort fromPlayerId, Message message)
        {
            Battle.Singleton.PlayerTwoChooseToSwap(message.GetInt());
        }
        
        [MessageHandler((ushort)ServerToClientMessageId.UpdatePlayerInfo)]
        private static void ClientConnectionInfo(Message message)
        {
            BattleNetworkManager.Instance.ClientUpdatePlayerInfo(message);
        }
        
        [MessageHandler((ushort)ServerToClientMessageId.PartyInfo)]
        private static void ClientPartyInfo(Message message)
        {
            BattleNetworkManager.Instance.ClientPartyInfo(message.GetUShort(), message.GetParty());
        }
        
        [MessageHandler((ushort)ServerToClientMessageId.StartBattle)]
        private static void ClientStartBattle(Message message)
        {
            BattleNetworkManager.Instance.ClientStartBattle();
        }
        
        // Turn Item receivers here:
        
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerMove)]
        private static void ClientTurnPlayerMove(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerMove(message);
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerMissed)]
        private static void ClientTurnPlayerMissed(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerMissed();
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerSwapBecauseFainted)]
        private static void ClientTurnPlayerSwapBecauseFainted(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerSwapBecauseFainted(message);
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerSwap)]
        private static void ClientTurnPlayerSwap(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerSwap(message);
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnEndBattle)]
        private static void ClientTurnEndBattle(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnEndBattle(message);
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnStartOfTurnStatusEffects)]
        private static void ClientTurnStartOfTurnStatusEffects(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnStartOfTurnStatusEffects();
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnEndOfTurnStatusEffects)]
        private static void ClientTurnEndOfTurnStatusEffects(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnEndOfTurnStatusEffects();
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerParalysed)]
        private static void ClientTurnPlayerParalysed(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerParalysed(message);
        }
        [MessageHandler((ushort)ServerToClientMessageId.TurnPlayerAsleep)]
        private static void ClientTurnPlayerAsleep(Message message)
        {
            BattleNetworkManager.Instance.ClientGotTurnPlayerAsleep(message);
        }
    }
}