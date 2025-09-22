using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        ConnectionInfo,
        AllPartiesReceived
    }
    
    public enum ServerToClientMessageId : ushort
    {
        UpdatePlayerInfo,
        PartyInfo,
        StartBattle
    }
    
    public class BattleNetworkManager : MonoBehaviour
    {
        public static BattleNetworkManager Instance;

        public Server Server;
        public Client Client;
        
        public Dictionary<ushort, NetworkPlayer> Players = new Dictionary<ushort, NetworkPlayer>();
        public int MaxPlayerCount;
        public bool IsHost;

        private int _acknowledgedPartyCount;
        
        public event EventHandler OnUpdatePlayerInfo;
        public event EventHandler OnStartedGeneratingParties;
        public event EventHandler OnFinishedGeneratingParties;
        public event EventHandler OnSentPartiesInfo;
        public event EventHandler OnReadyToStartGame;
        
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
            ClientSendBasicInfo();
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
            Server.Start(7777, 2);
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
            OnStartedGeneratingParties?.Invoke(this,  EventArgs.Empty);
            List<BattlerTemplate> potentialBattlers = 
                Resources.LoadAll<BattlerTemplate>("Pokemon Game/Battler Template").ToList();
            GenerateParty(potentialBattlers);
        }

        private void GenerateParty(List<BattlerTemplate> potentialBattlers)
        {
            Debug.Log($"Doing party generation");
            int start = DateTime.UtcNow.Millisecond;
            
            foreach (var player in Players.Values)
            {
                Debug.Log($"Generating party {player.Id}");
                player.Party = GenerateParty(50, potentialBattlers);
                Debug.Log($"Generated party {player.Id}");
            }
            
            int end = DateTime.UtcNow.Millisecond;
            
            Debug.Log($"Doing party generation took {end-start}ms");
            OnFinishedGeneratingParties?.Invoke(this,  EventArgs.Empty);
            // we don't send it to the host so they have already "acknowledged" the party generation
            _acknowledgedPartyCount = 1;
            
            if (_acknowledgedPartyCount >= MaxPlayerCount)
            {
                StartCoroutine(StartBattle());
                OnReadyToStartGame?.Invoke(this, EventArgs.Empty);
                ServerSendStartBattle();
            }
            
            foreach (var player in Players.Values)
            {
                ServerSendPartyInfoFor(player);
            }
            
            OnSentPartiesInfo?.Invoke(this, EventArgs.Empty);
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
        
        private Party GenerateParty(int level, List<BattlerTemplate> potential)
        {
            Party partyToReturn = new Party();
            
            Debug.Log("created new party obj");
            
            for (int i = 0; i < 6; i++)
            {
                BattlerTemplate template = potential[UnityEngine.Random.Range(0, potential.Count)];
                
                Debug.Log($"{i} picked template");
                
                Battler attacker = Battler.Init(template, level, template.name, new List<Move>(), true);
                Debug.Log($"{i} initialised");
                List<Move> moves = attacker.GetMostRecentMoves();
                Debug.Log($"{i} picked moves");
                
                for (int j = 0; j < moves.Count; j++)
                {
                    attacker.LearnMove(moves[j]);
                }
                
                Debug.Log($"{i} learnt moves");
                
                partyToReturn.Add(attacker);
                
                Debug.Log($"{i} added to party");
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
            }
            
            Server.SendToAll(message, Client.Id);
            OnUpdatePlayerInfo?.Invoke(this, EventArgs.Empty);
        }

        private void ServerSendPartyInfoFor(NetworkPlayer player)
        {
            Message message = Message.Create(MessageSendMode.Reliable, ServerToClientMessageId.PartyInfo);
            message.AddUShort(player.Id);
            message.AddParty(player.Party);
            
            Server.SendToAll(message, Client.Id);
        }

        private void ServerSendStartBattle()
        {
            Message message = Message.Create(MessageSendMode.Reliable, ServerToClientMessageId.StartBattle);
            
            Server.SendToAll(message, Client.Id);
        }
        
        private void ClientSendBasicInfo()
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

        private void ClientSendAllPartiesReceived()
        {
            _acknowledgedPartyCount = 0;
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerMessageId.AllPartiesReceived);

            Client.Send(message);
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
        
        public void ServerAllPartiesReceived()
        {
            _acknowledgedPartyCount++;
            
            if (_acknowledgedPartyCount >= MaxPlayerCount)
            {
                StartCoroutine(StartBattle());
                OnReadyToStartGame?.Invoke(this, EventArgs.Empty);
                ServerSendStartBattle();
            }
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
                if (Players.TryGetValue(id, out NetworkPlayer player))
                {
                    player.Username = username;
                    player.Pfp = pfp;
                }
                else
                {
                    player = new NetworkPlayer();
                    player.Id = id;
                    player.Username = username;
                    player.Pfp = pfp;
                    
                    Players.Add(player.Id, player);
                }
            }

            OnUpdatePlayerInfo?.Invoke(this, EventArgs.Empty);
        }

        public void ClientPartyInfo(ushort player, Party party)
        {
            if (Players.TryGetValue(player, out NetworkPlayer playerInfo))
            {
                playerInfo.Party = party;
            }

            _acknowledgedPartyCount++;
            if (_acknowledgedPartyCount == MaxPlayerCount)
            {
                ClientSendAllPartiesReceived();
            }
        }

        public void ClientStartBattle()
        {
            StartCoroutine(StartBattle());
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