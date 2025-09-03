using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PokemonGame.Game.Party;
using PokemonGame.General;
using PokemonGame.Global;
using PokemonGame.ScriptableObjects;
using UnityEngine;
using Riptide;
using Riptide.Utils;
using UnityEngine.SceneManagement;

namespace PokemonGame.Networking
{
    public enum ClientToServerMessageId : ushort
    {
        ConnectionInfo
    }
    
    public enum ServerToClientMessageId : ushort
    {
        UpdatePlayerInfo
    }
    
    public class BattleNetworkManager : MonoBehaviour
    {
        public static BattleNetworkManager Instance;

        public Server Server;
        public Client Client;
        
        public Dictionary<ushort, NetworkPlayer> Players = new Dictionary<ushort, NetworkPlayer>();
        public int MaxPlayerCount;
        public bool IsHost;
        
        public event EventHandler OnUpdatePlayerInfo;
        
        private void Awake()
        {
            Instance = this;
#if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
            RiptideLogger.Initialize(Debug.Log, true);
#endif
            
            Server = new Server();
            Client = new Client();
            
            SubscribeToClientEvents();
            SubscribeToServerEvents();
        }
        
        private void SubscribeToClientEvents()
        {
            Client.Connected += ClientOnConnected;
            Client.Disconnected += ClientOnDisconnected;
        }

        private void UnSubscribeToClientEvents()
        {
            Client.Connected -= ClientOnConnected;
            Client.Disconnected -= ClientOnDisconnected;
        }
        
        private void SubscribeToServerEvents()
        {
            Server.ClientDisconnected += ServerOnClientDisconnected;
        }

        private void UnSubscribeToServerEvents()
        {
            Server.ClientDisconnected -= ServerOnClientDisconnected;
        }
        
        private void ClientOnConnected(object sender, EventArgs e)
        {
            if (Players.TryGetValue(0, out NetworkPlayer player))
            {
                Message message = Message.Create(MessageSendMode.Reliable, ClientToServerMessageId.ConnectionInfo);
                message.AddString(player.Username);
                message.AddInt(player.Pfp);
                
                Client.Send(message);
            }
            
            Players.Clear();
        }

        private void ClientOnDisconnected(object sender, DisconnectedEventArgs e)
        {
            Players.Clear();
            SceneManager.LoadScene("Multiplayer");
        }

        private void ServerOnClientDisconnected(object sender, ServerDisconnectedEventArgs e)
        {
            Players.Remove(e.Client.Id);
            ServerSendPlayersInfo();
        }
        
        public void StartHosting()
        {
            Server.Start(7777, 1);
            MaxPlayerCount = Server.MaxClientCount;
            IsHost = true;
        }
        
        public void JoinGame(string address, string username, int pfp)
        {
            if (Client.IsConnected || Client.IsConnecting)
            {
                return;
            }
            
            NetworkPlayer player = new NetworkPlayer();
            player.Id = 0;
            player.Username = username;
            player.Pfp = pfp;
            
            Players.Add(player.Id, player);
            
            Client.Connect(address);
        }

        public void StartGame()
        {
            foreach (var player in Players.Values)
            {
                player.Party = GenerateParty(50);
            }
            
            ServerSendPlayersInfo();
        }

        private IEnumerator StartBattle()
        {
            Dictionary<string, object> vars = new Dictionary<string, object>
            {
                { "partyOne", Players.Values.ToList()[0].Party},
                { "partyTwo", Players.Values.ToList()[1].Party },
                { "online", true },
                { "trainerBattle", false}
            };
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyClose"));
            
            yield return new WaitForSeconds(0.4f);
            
            SceneLoader.LoadScene("Battle", vars);
        }
        
        private Party GenerateParty(int level)
        {
            Party partyToReturn = new Party();
            
            for (int i = 0; i < 6; i++)
            {
                List<BattlerTemplate> potential = Resources.LoadAll<BattlerTemplate>("Pokemon Game/Battler Template").ToList();
                
                BattlerTemplate template = potential[UnityEngine.Random.Range(0, potential.Count)];
                
                Battler attacker = Battler.Init(template, level, template.name, new List<Move>(), true);
                List<Move> moves = attacker.GetMostRecentMoves();
                
                for (int j = 0; j < moves.Count; j++)
                {
                    attacker.LearnMove(moves[j]);
                }
                
                partyToReturn.Add(attacker);
            }

            return partyToReturn;
        }
        
        private void ServerSendPlayersInfo()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ServerToClientMessageId.UpdatePlayerInfo);
            message.AddInt(MaxPlayerCount);
            message.AddInt(Players.Count);

            foreach (var playerInfoBeingSent in Players.Values)
            {
                message.AddUShort(playerInfoBeingSent.Id);
                message.AddString(playerInfoBeingSent.Username);
                message.AddInt(playerInfoBeingSent.Pfp);
                message.AddBool(playerInfoBeingSent.Party != null);
                
                if (playerInfoBeingSent.Party != null)
                {
                    message.AddParty(playerInfoBeingSent.Party);
                }
            }
            
            Server.SendToAll(message, Client.Id);
            OnUpdatePlayerInfo?.Invoke(this, EventArgs.Empty);
        }
        
        private void FixedUpdate()
        {
            Server.Update();
            Client.Update();
        }
        
        private void OnDisable()
        {
            Server.Stop();
            Client.Disconnect();
            
            UnSubscribeToClientEvents();
            UnSubscribeToServerEvents();
        }
        
        public void ServerConnectionInfo(ushort messageId, string username, int pfp)
        {
            NetworkPlayer player = new NetworkPlayer();
            player.Id = messageId;
            player.Username = username;
            player.Pfp = pfp;
            
            Players.Add(player.Id, player);
            ServerSendPlayersInfo();
        }

        public void ClientUpdatePlayerInfo(Message message)
        {
            MaxPlayerCount = message.GetInt();
            int length = message.GetInt();

            for (int i = 0; i < length; i++)
            {
                ushort id = message.GetUShort();
                string username = message.GetString();
                int pfp = message.GetInt();
                bool hasParty = message.GetBool();
                if (Players.TryGetValue(id, out NetworkPlayer player))
                {
                    player.Username = username;
                    player.Pfp = pfp;
                    if (hasParty)
                    {
                        player.Party = message.GetParty();
                    }
                }
                else
                {
                    player = new NetworkPlayer();
                    player.Id = id;
                    player.Username = username;
                    player.Pfp = pfp;
                    if (hasParty)
                    {
                        player.Party = message.GetParty();
                    }
                    
                    Players.Add(player.Id, player);
                }
            }

            OnUpdatePlayerInfo?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public class NetworkPlayer
    {
        public ushort Id;
        public string Username;
        public int Pfp;
        public Party Party;
    }
}