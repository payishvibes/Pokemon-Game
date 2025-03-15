using PokemonGame.General;

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
        private int CalculateDamage(Move move, Battler battlerThatUsed, Battler battlerBeingAttacked)
        {
            //Damage calculation equation from: https://bulbapedia.bulbagarden.net/wiki/Damage#Generation_II
            
            int damage = 0;
            
            //Checking to see if the move is capable of hitting the opponent battler
            foreach (var hType in move.type.cantHit)
            {
                if (hType == Type.FromBasic(battlerBeingAttacked.source.primaryType) || hType == Type.FromBasic(battlerBeingAttacked.source.secondaryType))
                {
                    Debug.Log(move.type + " can't hit that battler");
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

            int level = battlerThatUsed.attack;

            int power = move.damage;

            int item = 1;

            int critical = 1;

            int TK = 1;

            int weather = 1;

            // requires implementation of badges and gyms
            int badge = 1;

            int moveMod = 1;

            int doubleDmg = 1;
            
            if (move.category == MoveCategory.Physical)
            {
                attack = battlerThatUsed.attack;
                defense = battlerBeingAttacked.defense;
            }
            else if (move.category == MoveCategory.Special)
            {
                attack = battlerThatUsed.specialAttack;
                defense = battlerThatUsed.specialDefense;
            }

            damage = Mathf.RoundToInt((((2 * level / 5 + 2) * power * (attack / defense) / 50) * item * critical + 2) * TK *
                     weather * badge * stab * type * moveMod * doubleDmg);

            int randomness = Mathf.RoundToInt(Random.Range(.8f * damage, damage * 1.2f));
            damage = randomness;

            return damage;
        }

        private int CalculateDamage(MoveMethodEventArgs e)
        {
            return CalculateDamage(e.move, e.attacker, e.target);
        }
        
        public void Toxic(MoveMethodEventArgs e)
        {
            e.target.statusEffect = Registry.GetStatusEffect("Poisoned");
            Battle.Singleton.QueDialogue($"{e.target.name} was poisoned!", true);
        }

        public void Ember(MoveMethodEventArgs e)
        {
            e.damageDealt = CalculateDamage(e.move, e.attacker, e.target);
        }

        public void RazorLeaf(MoveMethodEventArgs e)
        {
            e.damageDealt = CalculateDamage(e.move, e.attacker, e.target);
        }

        public void Tackle(MoveMethodEventArgs e)
        {
            int damage = CalculateDamage(e.move, e.attacker, e.target);
            e.damageDealt = damage;
        }

        public void LeechLife(MoveMethodEventArgs e)
        {
            int damage = CalculateDamage(e);
            e.damageDealt = damage;
            Battle.Singleton.QueDialogue($"{e.attacker.name} healed {damage/2} health!");
            e.attacker.Heal(damage/2);
        }

        public void StringShot(MoveMethodEventArgs e)
        {
            
        }
    }   
}