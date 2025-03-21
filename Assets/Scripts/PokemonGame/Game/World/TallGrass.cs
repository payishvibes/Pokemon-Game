using System.Collections;
using System.Collections.Generic;
using Ink.Parsed;
using PokemonGame.Dialogue;
using PokemonGame.Game;
using PokemonGame.Game.Party;
using PokemonGame.General;
using PokemonGame.Global;
using PokemonGame.ScriptableObjects;
using UnityEngine;
using Random = UnityEngine.Random;

public class TallGrass : DialogueTrigger
{
    [SerializeField] private List<BattlerTemplate> pool;
    [SerializeField] private int minLevel, maxLevel;
    [SerializeField] private float attemptDelay;
    [SerializeField] private int oneInChance;
    [SerializeField] private CharacterController player;

    private bool _playerInsideGrass = false;

    private bool _waitingForStartBattle;

    private Battler _attacker;

    private void OnEnable()
    {
        DialogueManager.instance.DialogueEnded += DialogueEnded;
    }

    private void OnDisable()
    {
        DialogueManager.instance.DialogueEnded -= DialogueEnded;
    }

    private void DialogueEnded(object sender, DialogueEndedEventArgs e)
    {
        if (_waitingForStartBattle)
        {
            StartCoroutine(StartBattle());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            _playerInsideGrass = true;
            StartCoroutine(AttemptAttack());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            _playerInsideGrass = false;
        }
    }

    private IEnumerator AttemptAttack()
    {
        yield return new WaitForSeconds(attemptDelay);

        if (_playerInsideGrass)
        {
            if (Random.Range(0, oneInChance) == 0)
            {
                Attack();
            }
            else
            {
                StartCoroutine(AttemptAttack());
            }
        }
    }

    private void Attack()
    {
        BattlerTemplate template = pool[Random.Range(0, pool.Count)];
        
        Battler attacker = Battler.Init(template, Random.Range(minLevel, maxLevel), template.name, new List<Move>(), true);
        List<Move> moves = attacker.GetMostRecentMoves();

        for (int i = 0; i < moves.Count; i++)
        {
            attacker.LearnMove(moves[i]);
        }
        
        _attacker = attacker;
        _waitingForStartBattle = true;
        
        QueDialogue($"{attacker.name} appeared?!");
    }

    private IEnumerator StartBattle()
    {
        Instantiate(Resources.Load("Pokemon Game/Transitions/SpikyClose"));

        yield return new WaitForSeconds(0.4f);
        
        Party party = new Party();
        party.Add(_attacker);
        Debug.Log(party[0].level);
        Debug.Log(party[0].currentHealth);
            
        Dictionary<string, object> vars = new Dictionary<string, object>
        {
            { "playerParty", PartyManager.GetParty()},
            { "opponentParty", party },
            { "playerPosition", Player.Instance.transform.position },
            { "playerRotation", Player.Instance.targetRot },
            { "trainerBattle", false}
        };

        SceneLoader.LoadScene("Battle", vars);
    }
}
