using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PokemonGame.Networking
{
    public class LobbyUIManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private List<GameObject> playerDisplays;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button startButton;
        
        private void Awake()
        {
            HookNetworkEvents();
        }
        
        private void Start()
        {
            OnUpdatePlayerInfo(this, EventArgs.Empty);
        }
        
        private void HookNetworkEvents()
        {
            BattleNetworkManager.Instance.OnUpdatePlayerInfo += OnUpdatePlayerInfo;
            BattleNetworkManager.Instance.OnStartedGeneratingParties += OnStartedGeneratingParties;
            BattleNetworkManager.Instance.OnFinishedGeneratingParties += OnFinishedGeneratingParties;
            BattleNetworkManager.Instance.OnSentPartiesInfo += OnSentPartiesInfo;
            BattleNetworkManager.Instance.OnReadyToStartGame += OnReadyToStartGame;
        }
        
        private void UnHookNetworkEvents()
        {
            BattleNetworkManager.Instance.OnUpdatePlayerInfo -= OnUpdatePlayerInfo;
            BattleNetworkManager.Instance.OnStartedGeneratingParties -= OnStartedGeneratingParties;
            BattleNetworkManager.Instance.OnFinishedGeneratingParties -= OnFinishedGeneratingParties;
            BattleNetworkManager.Instance.OnSentPartiesInfo -= OnSentPartiesInfo;
            BattleNetworkManager.Instance.OnReadyToStartGame -= OnReadyToStartGame;
        }
        
        private void OnUpdatePlayerInfo(object sender, EventArgs e)
        {
            foreach (var playerDisplay in playerDisplays)
            {
                playerDisplay.SetActive(false);
            }
            
            List<NetworkPlayer> players = BattleNetworkManager.Instance.Players.Values.ToList();
            for (int i = 0; i < players.Count; i++)
            {
                NetworkPlayer player = players[i];
                GameObject playerDisplay = playerDisplays[i];
                playerDisplay.SetActive(true);
                
                TextMeshProUGUI username = playerDisplay.GetComponentInChildren<TextMeshProUGUI>();
                Image pfp = playerDisplay.GetComponentsInChildren<Image>()[1];
                
                username.text = player.Username;
                pfp.sprite = GetSpriteFromPokemonName(player.Pfp);
            }
            
            playerCountText.text = $"{players.Count}/{BattleNetworkManager.Instance.MaxPlayerCount}";
            startButton.gameObject.SetActive(BattleNetworkManager.Instance.IsHost);
            startButton.interactable = players.Count == BattleNetworkManager.Instance.MaxPlayerCount;
        }
        
        private void OnStartedGeneratingParties(object sender, EventArgs e)
        {
            statusText.text = "Generating parties, please wait...";
        }
        
        private void OnFinishedGeneratingParties(object sender, EventArgs e)
        {
            statusText.text = "Finished generating parties, sending party info...";
        }
        
        private void OnSentPartiesInfo(object sender, EventArgs e)
        {
            statusText.text = "Sent all party info...";
        }
        
        private void OnReadyToStartGame(object sender, EventArgs e)
        {
            statusText.text = "Ready to start game!";
        }
        
        private Sprite GetSpriteFromPokemonName(int dexNo)
        {
            return Resources.Load<Sprite>($"Pokemon Game/sprites/{dexNo}");
        }
        
        private void OnDisable()
        {
            UnHookNetworkEvents();
        }
        
        public void JoinLobby()
        {
            BattleNetworkManager.Instance.JoinGame(addressInput.text, usernameInput.text, 700);
        }
        
        public void HostLobby()
        {
            BattleNetworkManager.Instance.StartHosting();
            BattleNetworkManager.Instance.JoinGame("127.0.0.1:7777", usernameInput.text,700);
        }

        public void StartGame()
        {
            BattleNetworkManager.Instance.StartGame();
        }
    }
}