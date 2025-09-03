using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokeApiNet;
using PokemonGame.General;
using UnityEditor;
using PokemonGame.Dialogue;

namespace PokemonGame.Battle
{
    using Global;
    using UnityEngine;
    using ScriptableObjects;

    /// <summary>
    /// Contains all the logic for every move
    /// </summary>
    [CreateAssetMenu(fileName = "New Moves Methods", menuName = "All/New Moves Methods")]
    public class MovesMethods : ScriptableObject
    {
        private int CalculateDamage(Move move, Battler battlerThatUsed, Battler battlerBeingAttacked, out int effectiveIndex, out bool hitCrit)
        {
            //Damage calculation equation from: https://bulbapedia.bulbagarden.net/wiki/Damage#Generation_II
            
            int damage = 0;
            
            hitCrit = false;
            effectiveIndex = 0;
            
            //Checking to see if the move is capable of hitting the opponent battler
            foreach (var hType in move.type.cantHit)
            {
                if (hType == Type.FromBasic(battlerBeingAttacked.source.primaryType) || hType == Type.FromBasic(battlerBeingAttacked.source.secondaryType))
                {
                    Debug.Log(move.type + " can't hit that battler");
                    effectiveIndex = 3;
                    return 0;
                }
            }

            float type = 1;

            //Calculating type disadvantages
            foreach (var weakType in move.type.weakAgainst)
            {
                if (weakType == Type.FromBasic(battlerBeingAttacked.source.primaryType))
                {
                    type /= 2;
                }
                if (weakType == Type.FromBasic(battlerBeingAttacked.source.secondaryType))
                {
                    type /= 2;
                }
            }

            //Calculating type advantages
            foreach (var strongType in move.type.strongAgainst)
            {
                if (strongType == Type.FromBasic(battlerBeingAttacked.source.primaryType))
                {
                    type *= 2;
                }
                if (strongType == Type.FromBasic(battlerBeingAttacked.source.secondaryType))
                {
                    type *= 2;
                }
            }

            //Failsafe
            if (type > 4)
                type = 4;
            if (type < .25f)
                type = .25f;

            //STAB =  Same type attack bonus
            int stab = 1;
            if (move.type == Type.FromBasic(battlerThatUsed.source.primaryType))
            {
                stab = 2;
            }

            int attack = 0;
            int defense = 0;

            int level = battlerThatUsed.stats.attack;

            int power = move.damage;

            int item = 1;

            float critical = 1;

            int TK = 1;

            int weather = 1;

            // requires implementation of badges and gyms
            int badge = 1;

            int moveMod = 1;

            int doubleDmg = 1;

            float targets = 1;

            float pb = 1;

            float glaiveRush = 1;

            float burn = 1;

            if (move.category == MoveCategory.Physical && battlerThatUsed.statusEffect == Registry.GetStatusEffect("Burn"))
            {
                burn = 0.5f;
            }

            float other = 1;

            float zMove = 1;

            float random = Random.Range(0.85f, 1f);
            
            if (move.category == MoveCategory.Physical)
            {
                attack = battlerThatUsed.stats.attack;
                defense = battlerBeingAttacked.stats.defense;
            }
            else if (move.category == MoveCategory.Special)
            {
                attack = battlerThatUsed.stats.specialAttack;
                defense = battlerBeingAttacked.stats.specialDefense;
            }
            
            // critical hit calc

            int critStage = 1;
            
            if (move.increasedCritChance)
            {
                critStage += 1;
            }

            switch (critStage)
            {
                case 1:
                    hitCrit = Random.Range(1, 25) == 1;
                    break;
                case 2:
                    hitCrit = Random.Range(1, 9) == 1;
                    break;
                case 3:
                    hitCrit = Random.Range(1, 3) == 1;
                    break;
            }

            if (critStage >= 4)
            {
                hitCrit = true;
            }

            if (hitCrit)
            {
                critical = 1.5f;
            }

            if (type > 1)
            {
                effectiveIndex = 2;
            }else if (type < 1)
            {
                effectiveIndex = 1;
            }
            else if (Mathf.Approximately(type, 1))
            {
                effectiveIndex = 0;
            }

            damage = Mathf.RoundToInt(((((2f * level) / 5) * power * (attack / (float)defense)) / 50) + 2 * targets * pb * weather *
                glaiveRush * critical * random * stab * type * burn * other * zMove);

            int randomness = Mathf.RoundToInt(Random.Range(.8f * damage, damage * 1.2f));
            damage = randomness;

            return damage;
        }

        public void UpdateMoves()
        {
            UpdateMovesInfo();

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
#endif
        }

        private async void UpdateMovesInfo()
        {
            await UpdateMovesInfoTask();
        }

        private static async Task UpdateMovesInfoTask()
        {
            PokeApiClient pokeClient = new PokeApiClient();
            
            List<Move> moves = Resources.FindObjectsOfTypeAll<Move>().ToList();
            
            foreach (var pokeMove in moves)
            {
                Debug.Log($"setting the priority of the move {pokeMove.name}");

                try
                {
                    PokeApiNet.Move move =
                        await pokeClient.GetResourceAsync<PokeApiNet.Move>(pokeMove.name.ToLower());
                    
                    pokeMove.priority = move.Priority;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not find an entry for move: {pokeMove.name}");
                }
            }
        }

        public static MovesMethods GetMoveMethods()
        {
            return Resources.Load<MovesMethods>("Pokemon Game/Move/Move Methods");
        }

        private int CalculateDamage(MoveMethodEventArgs e)
        {
            int dmg = CalculateDamage(e.move, e.attacker, e.target, out int effectiveIndex, out bool crit);
            e.effectiveIndex =  effectiveIndex;
            e.crit = crit;
            return dmg;
        }

        public void DefaultMoveMethod(MoveMethodEventArgs e)
        {
            if (e.move.category != MoveCategory.Status)
            {
                e.target.TakeDamage(CalculateDamage(e), new BattlerDamageSource(e.attacker));
            }
        }
        
        public void Toxic(MoveMethodEventArgs e)
        {
            e.target.statusEffect = Registry.GetStatusEffect("Poisoned");
            Battle.Singleton.QueDialogue($"{e.target.name} was poisoned!", DialogueBoxType.Event);
        }

        public void LeechLife(MoveMethodEventArgs e)
        {
            int damage = CalculateDamage(e);
            e.target.TakeDamage(damage, new BattlerDamageSource(e.attacker));
            Battle.Singleton.QueDialogue($"{e.attacker.name} healed {damage/2} health!", DialogueBoxType.Event);
            e.attacker.Heal(damage/2);
        }

        public void SleepPowder(MoveMethodEventArgs e)
        {
            e.target.statusEffect = Registry.GetStatusEffect("Asleep");
            e.target.sleepTurns = Random.Range(1, 4);
            Battle.Singleton.QueDialogue($"{e.target.name} was put to sleep!", DialogueBoxType.Event);
        }

        public void WillOWisp(MoveMethodEventArgs e)
        {
            e.target.statusEffect = Registry.GetStatusEffect("Burn");
            Battle.Singleton.QueDialogue($"{e.target.name} was burned!", DialogueBoxType.Event);
        }

        public void BadTime(MoveMethodEventArgs e)
        {
            int damageDealt = CalculateDamage(e);
            e.target.TakeDamage(damageDealt, new BattlerDamageSource(e.attacker));
            Battle.Singleton.QueDialogue($"{e.target.name} is going to have a very Bad Time", DialogueBoxType.Event);
        }

        public void SelfDestruct(MoveMethodEventArgs e)
        {
            int damageDealt = CalculateDamage(e);
            e.target.TakeDamage(damageDealt, new BattlerDamageSource(e.attacker));
        }
    }
}