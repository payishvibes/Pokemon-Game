using System;
using System.IO;
using System.Text.RegularExpressions;
using PokemonGame.General;
using UnityEditor;

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

        public void CreateMoves()
        {
            DateTime start = DateTime.Now;
            StreamReader streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\moves other.csv");
            bool endOfFile = false;
            while (!endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (string.IsNullOrEmpty(dataString))
                {
                    endOfFile = true;
                    break;
                }
                
                dataString = dataString.Replace("—", "-1");
                dataString = dataString.Replace("%", "");
                dataString = dataString.Replace("∞", "-2");
                dataString = dataString.Replace("\"", "");
                var values = Regex.Split(dataString, @",(?=[^0])", RegexOptions.None);

                values[0] = values[0].Replace(",", "");

                values[2] = values[2].Replace("https://img.pokemondb.net/images/icons/move-special.png", "Special");
                values[2] = values[2].Replace("https://img.pokemondb.net/images/icons/move-physical.png", "Physical");
                values[2] = values[2].Replace("https://img.pokemondb.net/images/icons/move-special.png", "Status");
                
                Move move = CreateInstance<Move>();
                
                move.type = Type.FromBasic((BasicType)Enum.Parse(typeof(BasicType), values[1], true));
                if (string.IsNullOrEmpty(values[2]))
                {
                    move.category = MoveCategory.ZMove;
                }
                else
                {
                    move.category = (MoveCategory)Enum.Parse(typeof(MoveCategory), values[2], true);
                }
                move.name = values[0];
                move.basePP = int.Parse(values[5]);
                move.description = values[6];
                if (values[3] == "-1")
                {
                    move.probability = 0;
                }
                else
                {
                    move.probability = int.Parse(values[7]);
                }
                if (values[3] == "-2")
                {
                    move.damage = Int32.MaxValue;
                }
                else if (values[3] == "-1")
                {
                    move.damage = 0;
                }
                else
                {
                    move.damage = int.Parse(values[3]);
                }
                
                if (values[4] == "-2")
                {
                    move.accuracy = Int32.MaxValue;
                }
                else if (values[4] == "-1")
                {
                    move.accuracy = 0;
                }
                else
                {
                    move.accuracy = int.Parse(values[4])/100f;
                }
                
                AssetDatabase.CreateAsset(move, $"Assets/Resources/Pokemon Game/Move/{move.type.name}/{move.name}.asset");
                
                Debug.Log($"Created Move: {move.name}");

                endOfFile = true;
            }
            
            streamReader.Close();
            
            AssetDatabase.SaveAssets();
        }

        public static MovesMethods GetMoveMethods()
        {
            return Resources.Load<MovesMethods>("Pokemon Game/Move/Move Methods");
        }

        private int CalculateDamage(MoveMethodEventArgs e)
        {
            return CalculateDamage(e.move, e.attacker, e.target);
        }

        public void DefaultMoveMethod(MoveMethodEventArgs e)
        {
            e.damageDealt = CalculateDamage(e.move, e.attacker, e.target);
        }
        
        public void Toxic(MoveMethodEventArgs e)
        {
            e.target.statusEffect = Registry.GetStatusEffect("Poisoned");
            Battle.Singleton.QueDialogue($"{e.target.name} was poisoned!", true);
        }

        public void LeechLife(MoveMethodEventArgs e)
        {
            int damage = CalculateDamage(e);
            e.damageDealt = damage;
            Battle.Singleton.QueDialogue($"{e.attacker.name} healed {damage/2} health!");
            e.attacker.Heal(damage/2);
        }
    }   
}