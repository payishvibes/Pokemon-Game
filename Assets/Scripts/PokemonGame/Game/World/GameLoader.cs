namespace PokemonGame.Game.World
{
    using Party;
    using Trainers;
    using Global;
    using UnityEngine;
    
    public class GameLoader : MonoBehaviour
    {
        [SerializeField] private Party party;
        
        private void Awake()
        {
            if(SceneLoader.sceneLoadedFrom == "Battle")
            {
                Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyOpen"));
                LoadGameFromBattle();
            }
            else if (SceneLoader.sceneLoadedFrom == "Boot")
            {
                Instantiate(Resources.Load("Pokemon Game/Transitions/CircleFadeOpen"));
                PartyManager.SetPlayerParty(party.Copy());
            }
        }

        private void LoadGameFromBattle()
        {
            bool trainerBattle = SceneLoader.GetVariable<bool>("trainerBattle");
            PartyManager.SetPlayerParty(SceneLoader.GetVariable<Party>("partyOne"));
            string trainerName = "";
            if (trainerBattle)
            {
                trainerName = SceneLoader.GetVariable<string>("trainerName");
            }

            Vector3 playerPos = Player.globalPlayerPos;
            Quaternion playerRotation = Player.globalPlayerRot;
            bool isDefeated = SceneLoader.GetVariable<bool>("isDefeated");
            
            if (isDefeated && trainerBattle)
            {
                GameObject.Find(trainerName).GetComponent<Trainer>().Defeated();
            }
            
            Player.Instance.SetPosRot(playerPos, playerRotation);
        }
    }    
}