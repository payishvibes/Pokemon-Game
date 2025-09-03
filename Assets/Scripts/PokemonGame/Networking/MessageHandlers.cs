using Riptide;

namespace PokemonGame.Networking
{
    public class MessageHandlers
    {
        [MessageHandler((ushort)ClientToServerMessageId.ConnectionInfo)]
        private static void ServerConnectionInfo(ushort fromPlayerId, Message message)
        {
            BattleNetworkManager.Instance.ServerConnectionInfo(fromPlayerId, message.GetString(), message.GetInt());
        }
        
        [MessageHandler((ushort)ServerToClientMessageId.UpdatePlayerInfo)]
        private static void ClientConnectionInfo(Message message)
        {
            BattleNetworkManager.Instance.ClientUpdatePlayerInfo(message);
        }
    }
}