using PokemonGame.ScriptableObjects;

namespace PokemonGame.Trainers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using General;
    using Global;
    using UnityEngine;
    using Game.Party;
    using Game;
    using NPC;

    /// <summary>
    /// Initiates a battle based on certain inspector parameters 
    /// </summary>
    public class Trainer : NPC
    {
        /// <summary>
        /// Is the trainer defeated
        /// </summary>
        [Space] [Header("Defeated")] public bool isDefeated;
        
        /// <summary>
        /// The party that the trainer load's into the battle for the player to fight
        /// </summary>
        [Space] [Header("Party")] public Party party;

        /// <summary>
        /// The ai that the trainer load's into the battle for the player to fight
        /// </summary>
        [Space] [Header("AI")] public EnemyAI ai;

        [Space]
        [Header("Dialogue")]
        [SerializeField] private TextAsset startBattleText;
        [SerializeField] private TextAsset defeatedBattleText;
        [SerializeField] private TextAsset idleDialogue;
        
        private bool _hasTalkedDefeatedText;

        private bool isStartingBattle;

        private void Start()
        {
            isDefeated = TrainerRegister.IsDefeated(this);
            interactable = isDefeated;
            DialogueFinished += DialogueEnded;
        }

        protected override void OverrideUpdate()
        {
            if (!isDefeated)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity))
                {
                    if (hit.transform.GetComponent<Player>())
                    {
                        StartBattle();
                    }
                }
            }
        }

        /// <summary>
        /// Triggers the defeated dialogue
        /// </summary>
        public void Defeated()
        {
            isDefeated = true;
            
            TrainerRegister.Defeated(this);
            Debug.Log($"Trainer is now deafeated: {TrainerRegister.IsDefeated(this)}");
            
            StartCoroutine(StartDefeatedDialogue());
        }

        private IEnumerator StartDefeatedDialogue()
        {
            yield return new WaitForEndOfFrame();
            QueDialogue(defeatedBattleText);
        }

        protected override void OnPlayerInteracted()
        {
            QueDialogue(idleDialogue);
            base.OnPlayerInteracted();
        }

        private void StartBattle()
        {
            if (!isDefeated && !isStartingBattle)
            {
                isStartingBattle = true;
                Player.Instance.LookAtTarget(transform.position);
                QueDialogue(startBattleText);
            }
        }

        private void DialogueEnded(object sender, EventArgs args)
        {
            if(!isDefeated && isStartingBattle)
            {
                StartCoroutine(LoadBattle());
            }
            else
            {
                interactable = true;
            }
        }

        private IEnumerator LoadBattle()
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i])
                {
                    Battler replacementBattler = Battler.CreateCopy(party[i]);
                    party[i] = replacementBattler;
                }
            }

            Party playerParty = PartyManager.GetParty();

            Battler charmander = Registry.GetBattler("Player Charmander");

            if (playerParty == null)
            {
                playerParty = new Party();
                
                playerParty.Add(Battler.CreateCopy(charmander));
            }else if (playerParty.Count == 0)
            {
                playerParty.Add(Battler.CreateCopy(charmander));
            }
            
            Dictionary<string, object> vars = new Dictionary<string, object>
            {
                { "playerParty", playerParty},
                { "opponentParty", party },
                { "enemyAI", ai },
                { "opponentName", gameObject.name },
                { "playerPosition", Player.Instance.transform.position },
                { "playerRotation", Player.Instance.targetRot },
                { "trainerBattle", true}
            };
            
            Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyClose"));

            yield return new WaitForSeconds(0.4f);

            SceneLoader.LoadScene("Battle", vars);
        }
    }
}