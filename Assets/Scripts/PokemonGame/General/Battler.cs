namespace PokemonGame.General
{
    using System;
    using System.Collections.Generic;
    using ScriptableObjects;
    using UnityEngine;

    /// <summary>
    /// The class that contains all the information for a battler
    /// </summary>
    [CreateAssetMenu(order = 7, fileName = "New Battler Prefab", menuName = "Pokemon Game/New Battler Prefab")]
    public class Battler : ScriptableObject
    {
        private BattlerTemplate _oldSource;
        /// <summary>
        /// The source that the battler uses to determine base stats 
        /// </summary>
        [Header("Source")]
        public BattlerTemplate source;

        private int _oldLevel;
        /// <summary>
        /// </summary>
        /// The level is what determines stats and if the battler respects the player
        [Space]
        public int level;
        
        /// <summary>
        /// The current health of the battler
        /// </summary>
        public int currentHealth;

        /// <summary>
        /// The name of the battler, unlike batter templates this can be changed for nicknames
        /// </summary>
        public new string name;
        
        /// <summary>
        /// The maximum health of the battler
        /// </summary>
        [Space]
        [Header("Stats")]
        public int maxHealth;
        
        /// <summary>
        /// The attack statistic for the battler
        /// </summary>
        public int attack;
        /// <summary>
        /// The defense statistic for the battler
        /// </summary>
        public int defense;
        /// <summary>
        /// The special attack statistic for the battler
        /// </summary>
        public int specialAttack;
        /// <summary>
        /// The special defence statistic for the battler
        /// </summary>
        public int specialDefense;
        /// <summary>
        /// The speed statistic for the battler
        /// </summary>
        public int speed;
        /// <summary>
        /// The current amount of experience points the battler has in progressing through its current level
        /// </summary>
        [Space] public int exp;

        /// <summary>
        /// The HP EV of the battler
        /// </summary>
        [Space]
        public int[] EVs = new int[6];
        public int[] IVs = new int[6];
        public Nature nature;

        /// <summary>
        /// The Affection stat of the battler
        /// </summary>
        public int affection;
        
        /// <summary>
        /// Is the battler fainted
        /// </summary>
        [Space]
        public bool isFainted;
        /// <summary>
        /// The current status effect that the batter has
        /// </summary>
        public StatusEffect statusEffect;
        
        /// <summary>
        /// The list of moves that the battler has
        /// </summary>
        [Space]
        public List<Move> moves;

        /// <summary>
        /// The pp information about this battler's moves
        /// </summary>
        public List<MovePPData> movePpInfos;

        /// <summary>
        /// Invoked when the battler takes damage
        /// </summary>
        public event EventHandler OnTookDamage;
        /// <summary>
        /// Invoked when the battler faints
        /// </summary>
        public event EventHandler<BattlerTookDamageArgs> OnFainted;
        /// <summary>
        /// Invoked when the battlers health updates (includes taking damage)
        /// </summary>
        public event EventHandler OnHealthUpdated;

        /// <summary>
        /// Inflict damage onto the battler
        /// </summary>
        /// <param name="damage">The amount of damage to inflict</param>
        /// <param name="dSource">The source type of the damage</param>
        public void TakeDamage(int damage, DamageSource dSource)
        {
            Debug.Log($"Damage dealt: {damage}/{currentHealth}");
            
            currentHealth -= damage;
            
            OnTookDamage?.Invoke(this, EventArgs.Empty);
            OnHealthUpdated?.Invoke(this, EventArgs.Empty);
            
            if (currentHealth <= 0)
            {
                Fainted(dSource);
            }
        }

        /// <summary>
        /// Heal the battler by the amount to heal, battlers health will not go above the <see cref="maxHealth"/>
        /// </summary>
        /// <param name="amountToHeal">The amount of health to gain</param>
        public void Heal(int amountToHeal)
        {
            UpdateHealth(currentHealth + amountToHeal);
        }

        /// <summary>
        /// Give the battler exp, will handle leveling up and stat gains
        /// </summary>
        /// <param name="amountToGain">The amount of exp to give</param>
        public void GainExp(int amountToGain)
        {
            // TODO: all the fucking logic for carrying over exp and other shit like leveling up and evolving
            exp += amountToGain;
        }

        /// <summary>
        /// Handle fainting
        /// </summary>
        /// <param name="source">Source of damage that caused the battler to faint</param>
        private void Fainted(DamageSource source)
        {
            currentHealth = 0;
            isFainted = true;
            OnFainted?.Invoke(this, new BattlerTookDamageArgs(source, this));
            Debug.Log("Battler Fainted");
        }

        /// <summary>
        /// Used to change the battlers current health without inflicting damage
        /// </summary>
        /// <param name="newHealth">The health to set it to</param>
        public void UpdateHealth(int newHealth)
        {
            currentHealth = newHealth;
            
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            OnHealthUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Revive(bool full)
        {
            isFainted = false;

            if (full)
            {
                UpdateHealth(maxHealth);
                foreach (var ppInfo in movePpInfos)
                {
                    ppInfo.CurrentPP = ppInfo.MaxPP;
                }
            }
            else
            {
                UpdateHealth(maxHealth/2);
            }
        }

        /// <summary>
        /// Trys to make the battler learn a move
        /// </summary>
        /// <param name="moveToLearn">The move you want the battler to learn</param>
        /// <returns>Weather the battler was able to learn the move</returns>
        public bool LearnMove(Move moveToLearn)
        {
            if (moves.Count < 4)
            {
                moves.Add(moveToLearn);
                movePpInfos.Add(new MovePPData(moveToLearn.basePP, moveToLearn.basePP));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Used for updating stats and such outside of runtime
        /// </summary>
        private void OnValidate()
        {
            if (!statusEffect)
            {
                statusEffect = StatusEffect.Healthy;
            }
            
            if (_oldLevel != level)
            {
                UpdateStats();
            }
            
            if (_oldSource != source)
            {
                UpdateStats();
            }

            _oldSource = source;
            _oldLevel = level;

            if (currentHealth > maxHealth)
                currentHealth = maxHealth;
            
            //Making sure the battler always has 4 moves
            if (moves.Count > 4)
            {
                moves.RemoveAt(4);
            }
        }
        
        /// <summary>
        /// Updates the stats of the battler
        /// </summary>
        private void UpdateStats()
        {
            if(!source) return;

            float attackModifier = 1;
            float defenseModifier = 1;
            float specialAttackModifier = 1;
            float specialDefenseModifier = 1;
            float speedModifier = 1;

            switch (nature)
            {
                case Nature.Adamant:
                    attackModifier = 1.1f;
                    specialAttackModifier = 0.9f;
                    break;
                case Nature.Bold:
                    defenseModifier = 1.1f;
                    attackModifier = 0.9f;
                    break;
                case Nature.Brave:
                    attackModifier = 1.1f;
                    speedModifier = 0.9f;
                    break;
                case Nature.Calm:
                    specialDefenseModifier = 1.1f;
                    attackModifier = 0.9f;
                    break;
                case Nature.Careful:
                    specialDefenseModifier = 1.1f;
                    attackModifier = 0.9f;
                    break;
                case Nature.Gentle:
                    specialDefenseModifier = 1.1f;
                    defenseModifier = 0.9f;
                    break;
                case Nature.Hasty:
                    speedModifier = 1.1f;
                    attackModifier = 0.9f;
                    break;
                case Nature.Impish:
                    defenseModifier = 1.1f;
                    specialAttackModifier = 0.9f;
                    break;
                case Nature.Jolly:
                    speedModifier = 1.1f;
                    specialAttackModifier = 0.9f;
                    break;
                case Nature.Lax:
                    defenseModifier = 1.1f;
                    specialDefenseModifier = 0.9f;
                    break;
                case Nature.Lonely:
                    attackModifier = 1.1f;
                    defenseModifier = 0.9f;
                    break;
                case Nature.Mild:
                    specialAttackModifier = 1.1f;
                    defenseModifier = 0.9f;
                    break;
                case Nature.Modest:
                    attackModifier = 1.1f;
                    specialAttackModifier = 0.9f;
                    break;
                case Nature.Naive:
                    specialAttackModifier = 1.1f;
                    defenseModifier = 0.9f;
                    break;
                case Nature.Naughty:
                    attackModifier = 1.1f;
                    specialDefenseModifier = 0.9f;
                    break;
                case Nature.Quiet:
                    specialAttackModifier = 1.1f;
                    speedModifier = 0.9f;
                    break;
                case Nature.Rash:
                    specialAttackModifier = 1.1f;
                    specialDefenseModifier = 0.9f;
                    break;
                case Nature.Relaxed:
                    defenseModifier = 1.1f;
                    speedModifier = 0.9f;
                    break;
                case Nature.Sassy:
                    specialDefenseModifier = 1.1f;
                    speedModifier = 0.9f;
                    break;
                case Nature.Timid:
                    speedModifier = 1.1f;
                    attackModifier = 0.9f;
                    break;
            }

            maxHealth = Mathf.FloorToInt((((2f * source.baseHealth + IVs[0]) * 2 + Mathf.FloorToInt(EVs[0] / 4f)) *
                                          level) / 100) + level + 10;
            
            attack = Mathf.FloorToInt((((2f * source.baseAttack + IVs[1]) * 2 + Mathf.FloorToInt(EVs[1] / 4f)) *
                                          level) / 100) + 5;
            attack = Mathf.FloorToInt(attack * attackModifier);
            
            defense = Mathf.FloorToInt((((2f * source.baseDefense + IVs[2]) * 2 + Mathf.FloorToInt(EVs[2] / 4f)) *
                                       level) / 100) + 5;
            defense = Mathf.FloorToInt(defense * defenseModifier);
            
            specialAttack = Mathf.FloorToInt((((2f * source.baseSpecialAttack + IVs[3]) * 2 + Mathf.FloorToInt(EVs[3] / 4f)) *
                                        level) / 100) + 5;
            specialAttack = Mathf.FloorToInt(specialAttack * specialAttackModifier);
            
            specialDefense = Mathf.FloorToInt((((2f * source.baseSpecialDefense + IVs[4]) * 2 + Mathf.FloorToInt(EVs[4] / 4f)) *
                                        level) / 100) + 5;
            specialDefense = Mathf.FloorToInt(specialDefense * specialDefenseModifier);
            
            speed = Mathf.FloorToInt((((2f * source.baseSpeed + IVs[5]) * 2 + Mathf.FloorToInt(EVs[5] / 4f)) *
                                               level) / 100) + 5;
            speed = Mathf.FloorToInt(speed * speedModifier);
        }

        public void UpdateLevel(int newLevel)
        {
            level = newLevel;
            UpdateStats();
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Returns a battler that has been created using the parameters given
        /// </summary>
        /// <param name="sourceBattler">The Battler that the new battler will be based on</param>
        /// <param name="autoAssignHealth">Automatically set the battlers health to the max health?</param>
        /// <returns>A battler that has been created using the parameters given</returns>
        public static Battler Init(Battler sourceBattler, bool autoAssignHealth)
        {
            return Init(sourceBattler.source, sourceBattler.level, sourceBattler.name, sourceBattler.isFainted,
                sourceBattler.exp, sourceBattler.statusEffect, sourceBattler.EVs, sourceBattler.IVs,
                sourceBattler.currentHealth,
                sourceBattler.moves, autoAssignHealth);
        }

        public static Battler Init(BattlerTemplate source, int level, string name, List<Move> moves, bool autoAssignHealth)
        {
            int[] EVs = new[]
            {
                0,
                0,
                0,
                0,
                0,
                0
            };
            
            int[] IVs = new[]
            {
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32)
            };
            
            return Init(source, level, name, false,
                0, StatusEffect.Healthy, EVs, IVs, 0,
                moves, autoAssignHealth);
        }

        public Sprite GetSpriteFront()
        {
            return source.texture.basic;
        }

        public Sprite GetSpriteBack()
        {
            return source.texture.basicBack;
        }

        /// <summary>
        /// Returns a battler that has been created using the parameters given
        /// </summary>
        /// <param name="source">The Battler Template that the new battler will use to calculate stats</param>
        /// <param name="level">The <see cref="level"/> of the new battler</param>
        /// <param name="statusEffect">The status effect that the new battler will have</param>
        /// <param name="name">The nickname of the new battler</param>
        /// <param name="moves">Moves that the battler has</param>
        /// <param name="autoAssignHealth">Auto assign health to the <see cref="maxHealth"/> when creating</param>
        /// <returns>A battler that has been created using the parameters given</returns>
        public static Battler Init(BattlerTemplate source, int level, string name, bool isFainted, int exp,
            StatusEffect statusEffect, int[] EVs, int[] IVs, int currentHealth, List<Move> moves, bool autoAssignHealth)
        {
            Battler returnBattler = CreateInstance<Battler>();
            
            returnBattler.source = source;
            returnBattler.UpdateLevel(level);
            returnBattler.name = name;
            returnBattler.isFainted = isFainted;
            returnBattler.exp = exp;
            returnBattler.statusEffect = statusEffect;
            returnBattler.moves = new List<Move>();
            returnBattler.movePpInfos = new List<MovePPData>();
            returnBattler.EVs = EVs;

            foreach (var move in moves)
            {
                if(move != null)
                {
                    returnBattler.LearnMove(move);
                }
            }
            
            returnBattler.UpdateStats();

            if (autoAssignHealth)
                returnBattler.currentHealth = returnBattler.maxHealth;
            else
                returnBattler.currentHealth = currentHealth;
            
            return returnBattler;
        }
        
        /// <summary>
        /// Creates an exact copy of the battler it is given
        /// </summary>
        /// <param name="battler">The battler to duplicate</param>
        /// <returns>The copied battler</returns>
        public static Battler CreateCopy(Battler battler)
        {
            Battler returnBattler = Init(battler, true);

            returnBattler.currentHealth = battler.currentHealth;
            returnBattler.isFainted = battler.isFainted;
            
            return returnBattler;
        }
    }

    public enum Nature
    {
        Adamant,
        Bashful,
        Bold,
        Brave,
        Calm,
        Careful,
        Docile,
        Gentle,
        Hardy,
        Hasty,
        Impish,
        Jolly,
        Lax,
        Lonely,
        Mild,
        Modest,
        Naive,
        Naughty,
        Quiet,
        Quirky,
        Rash,
        Relaxed,
        Sassy,
        Serious,
        Timid
    }
}