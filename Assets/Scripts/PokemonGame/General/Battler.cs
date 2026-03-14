using System.Linq;

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
        /// The name of the battler, unlike batter templates this can be changed for nicknames
        /// </summary>
        public new string name;
        
        /// <summary>
        /// The source that the battler uses to determine base stats 
        /// </summary>
        [Header("Source")]
        public BattlerTemplate source;

        private int _oldLevel;
        /// <summary>
        /// The level is what determines stats and if the battler respects the player
        /// </summary>
        [Space]
        public int level;

        /// <summary>
        /// Gender of the battler
        /// </summary>
        public Gender gender;
        
        /// <summary>
        /// The current health of the battler
        /// </summary>
        public int currentHealth;

        /// <summary>
        /// Is the battler shiny
        /// </summary>
        public bool shiny;
        
        /// <summary>
        /// The stat information for the battler
        /// </summary>
        [Space]
        [Header("Stats")]
        public BattlerStats stats;
        
        /// <summary>
        /// The current amount of experience points the battler has in progressing through its current level
        /// </summary>
        [Space] public int exp;

        /// <summary>
        /// The HP EV of the battler
        /// </summary>
        [Space]
        public BattlerStats EVs;
        public BattlerStats IVs;
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
        /// The number of turns the battler will be asleep for
        /// </summary>
        public int sleepTurns = 0;

        [HideInInspector] public bool wantToEvolve;
        [HideInInspector] public EvolutionData evolutionToPerform;
        public event EventHandler<EvolutionData> OnCanEvolve;
        public event EventHandler<int> OnCanLevelUp;
        public event EventHandler<OnLevelUpEventArgs> OnLevelUp;

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
            exp += amountToGain;
            
            HandleLevelUps();
        }

        private void HandleLevelUps()
        {
            if (exp >= ExperienceCalculator.RequiredForNextLevel(this))
            {
                CanLevelUp();
            }
        }

        private void CanLevelUp()
        {
            OnCanLevelUp?.Invoke(this, level+1);
        }

        public OnLevelUpEventArgs InitiateLevelUp()
        {
            if (exp >= ExperienceCalculator.RequiredForNextLevel(this))
            {
                return LevelUp();
            }
            
            Debug.LogWarning("No level up to initiate");
            return null;
        }

        private OnLevelUpEventArgs LevelUp()
        {
            int holdover = ExperienceCalculator.RequiredForNextLevel(this) - exp;

            level += 1;

            int health = stats.maxHealth;
            
            OnLevelUpEventArgs args = new OnLevelUpEventArgs();
            args.oldStats = stats;
                
            UpdateStats();

            args.newStats = stats;
            args.newLevel = level;

            currentHealth += stats.maxHealth - health;
            
            exp = holdover;
            OnLevelUp?.Invoke(this, args);
            CheckIfCanEvolve();
            return args;
        }

        private void CheckIfCanEvolve()
        {
            foreach (var evolution in source.evolutions.possibleEvolutions)
            {
                if (evolution.triggerType == EvolutionTriggerType.Level)
                {
                    if (evolution.minLevel <= level)
                    {
                        wantToEvolve = true;
                        evolutionToPerform = evolution;
                        OnCanEvolve?.Invoke(this, evolution);
                    }
                }
            }
        }

        public void EvolutionApproved()
        {
            bool updateName = name == source.name;
            
            source = evolutionToPerform.evolution;
            wantToEvolve = false;

            if (updateName)
                name = source.name;
        }

        /// <summary>
        /// Handle fainting
        /// </summary>
        /// <param name="dSource">Source of damage that caused the battler to faint</param>
        private void Fainted(DamageSource dSource)
        {
            currentHealth = 0;
            isFainted = true;
            OnFainted?.Invoke(this, new BattlerTookDamageArgs(dSource, this));
        }
        
        /// <summary>
        /// Used to change the battlers current health without inflicting damage
        /// </summary>
        /// <param name="newHealth">The health to set it to</param>
        public void UpdateHealth(int newHealth)
        {
            currentHealth = newHealth;
            
            if (currentHealth > stats.maxHealth)
            {
                currentHealth = stats.maxHealth;
            }

            OnHealthUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void Revive(bool full)
        {
            isFainted = false;

            if (full)
            {
                UpdateHealth(stats.maxHealth);
                foreach (var ppInfo in movePpInfos)
                {
                    ppInfo.CurrentPP = ppInfo.MaxPP;
                }
            }
            else
            {
                UpdateHealth(stats.maxHealth/2);
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
            UpdateStats();
            currentHealth = stats.maxHealth;
            
            if (!statusEffect)
            {
                statusEffect = StatusEffect.Healthy;
            }

            _oldSource = source;
            _oldLevel = level;

            if (currentHealth > stats.maxHealth)
                currentHealth = stats.maxHealth;
            
            //Making sure the battler always has 4 moves
            if (moves.Count > 4)
            {
                moves.RemoveAt(4);
            }
        }

        public List<Move> GetMostRecentMoves()
        {
            SerializableDictionary<Move, int> levelUpMoves = source.possibleMoves.levelup;
            var sortedLevelUpMoves = levelUpMoves.OrderBy(x => x.Value).ToList();

            sortedLevelUpMoves.Reverse();

            List<Move> returnMoves = new List<Move>();

            for (int i = 0; i < sortedLevelUpMoves.Count; i++)
            {
                if (returnMoves.Count >= 4)
                {
                    break;
                }
                
                if (sortedLevelUpMoves[i].Value <= level)
                {
                    returnMoves.Add(sortedLevelUpMoves[i].Key);
                }
            }

            return returnMoves;
        }
        
        /// <summary>
        /// Updates the stats of the battler
        /// </summary>
        public void UpdateStats()
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

            stats.maxHealth = Mathf.FloorToInt(((2f * source.baseStats.maxHealth + IVs.maxHealth + Mathf.FloorToInt(EVs.maxHealth / 4f)) * level)/(100)) + level + 10;
            
            stats.attack = Mathf.FloorToInt(((2f * source.baseStats.attack + IVs.attack + Mathf.FloorToInt(EVs.attack / 4f)) *
                level) / 100) + 5;
            stats.attack = Mathf.FloorToInt(stats.attack * attackModifier);
            
            stats.defense = Mathf.FloorToInt(((2f * source.baseStats.defense + IVs.attack + Mathf.FloorToInt(EVs.defense / 4f)) *
                level) / 100) + 5;
            stats.defense = Mathf.FloorToInt(stats.defense * defenseModifier);
            
            stats.specialAttack = Mathf.FloorToInt(((2f * source.baseStats.specialAttack + IVs.specialAttack + Mathf.FloorToInt(EVs.specialAttack / 4f)) *
                level) / 100) + 5;
            stats.specialAttack = Mathf.FloorToInt(stats.specialAttack * specialAttackModifier);
            
            stats.specialDefense = Mathf.FloorToInt(((2f * source.baseStats.specialDefense + IVs.specialDefense + Mathf.FloorToInt(EVs.specialDefense / 4f)) *
                level) / 100) + 5;
            stats.specialDefense = Mathf.FloorToInt(stats.specialDefense * specialDefenseModifier);
            
            stats.speed = Mathf.FloorToInt(((2f * source.baseStats.speed + IVs.speed + Mathf.FloorToInt(EVs.speed / 4f)) *
                level) / 100) + 5;
            stats.speed = Mathf.FloorToInt(stats.speed * speedModifier);
        }

        public void UpdateLevel(int newLevel)
        {
            level = newLevel;
            UpdateStats();
            currentHealth = stats.maxHealth;
        }

        /// <summary>
        /// Returns a battler that has been created using the parameters given
        /// </summary>
        /// <param name="sourceBattler">The Battler that the new battler will be based on</param>
        /// <param name="autoAssignHealth">Automatically set the battlers health to the max health?</param>
        /// <returns>A battler that has been created using the parameters given</returns>
        public static Battler Init(Battler sourceBattler, bool autoAssignHealth)
        {
            return Init(sourceBattler.source, sourceBattler.level, sourceBattler.name, sourceBattler.gender, sourceBattler.shiny, sourceBattler.isFainted,
                sourceBattler.exp, sourceBattler.statusEffect, sourceBattler.EVs, sourceBattler.IVs, sourceBattler.nature,
                sourceBattler.currentHealth,
                sourceBattler.moves, autoAssignHealth);
        }

        public static Battler Init(BattlerTemplate source, int level, string name, List<Move> moves, bool autoAssignHealth)
        {
            BattlerStats EVs = BattlerStats.zero;
            
            BattlerStats IVs = new BattlerStats(
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32),
                UnityEngine.Random.Range(0, 32)
            );
            
            Nature nature = (Nature)UnityEngine.Random.Range(0, (int)Nature.Timid);

            bool shiny = UnityEngine.Random.Range(0, 4096) == 0;

            Gender gender = Gender.Female;
            
            return Init(source, level, name, gender, shiny, false,
                0, StatusEffect.Healthy, EVs, IVs, nature, 0,
                moves, autoAssignHealth);
        }

        public Sprite GetSpriteFront()
        {
            if (shiny)
            {
                if (source.texture.female != null && gender == Gender.Female)
                {
                    return source.texture.femaleShiny;
                }
                return source.texture.shiny;
            }
            if (source.texture.female != null && gender == Gender.Female)
            {
                return source.texture.female;
            }
            return source.texture.basic;
        }

        public Sprite GetSpriteBack()
        {
            return source.texture.basicBack;
        }

        public int GetIndexOfMove(Move move)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i] == move)
                {
                    return i;
                }
            }

            return 0;
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
        public static Battler Init(BattlerTemplate source, int level, string name, Gender gender, bool shiny, bool isFainted, int exp,
            StatusEffect statusEffect, BattlerStats EVs, BattlerStats IVs, Nature nature, int currentHealth, List<Move> moves, bool autoAssignHealth)
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
            returnBattler.shiny = shiny;
            returnBattler.gender = gender;
            returnBattler.IVs = IVs;
            returnBattler.nature = nature;

            foreach (var move in moves)
            {
                if(move != null)
                {
                    returnBattler.LearnMove(move);
                }
            }
            
            returnBattler.UpdateStats();

            if (autoAssignHealth)
                returnBattler.currentHealth = returnBattler.stats.maxHealth;
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

    public class OnLevelUpEventArgs
    {
        public BattlerStats oldStats;
        public BattlerStats newStats;
        public int newLevel;
    }

    public enum Nature : int
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

    public enum Gender : int
    {
        Female,
        Male,
        Genderless
    }

    [Serializable]
    public struct BattlerStats
    {
        public int maxHealth;
        public int attack;
        public int defense;
        public int specialAttack;
        public int specialDefense;
        public int speed;

        public static BattlerStats zero = new BattlerStats(0, 0, 0, 0, 0, 0);

        public BattlerStats(int maxHealth, int attack, int defense, int specialAttack, int specialDefense, int speed)
        {
            this.maxHealth = maxHealth;
            this.attack = attack;
            this.defense = defense;
            this.specialAttack = specialAttack;
            this.specialDefense = specialDefense;
            this.speed = speed;
        }
    }
}