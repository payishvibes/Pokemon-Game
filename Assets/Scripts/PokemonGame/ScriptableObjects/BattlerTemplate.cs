using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokeApiNet;
using PokemonGame.General;
using PokemonGame.Global;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;

    /// <summary>
    /// The basic information about a battler, informs a <see cref="Battler"/> instance
    /// </summary>
    [CreateAssetMenu(order = 1, fileName = "New Battler Template", menuName = "Pokemon Game/New Battler Template")]
    public class BattlerTemplate : ScriptableObject
    {
        private static PokeApiClient pokeClient;

        /// <summary>
        /// The name of the battler species
        /// </summary>
        public new string name;
        /// <summary>
        /// The primary type of the battler species
        /// </summary>
        public BasicType primaryType;
        /// <summary>
        /// The secondary type of the battler species
        /// </summary>
        public BasicType secondaryType;
        /// <summary>
        /// The <see cref="Sprites"/> information for the battler species
        /// </summary>
        public Sprites texture;
        /// <summary>
        /// The pokedex number of the battler
        /// </summary>
        public int dexNo;
        /// <summary>
        /// The possible moves the battler can learn
        /// </summary>
        public PossibleMoves possibleMoves;
        /// <summary>
        /// The evolutions the battler can have
        /// </summary>
        public Evolution evolutions;
        /// <summary>
        /// The <see cref="ExperienceGroup"/> of the battler
        /// </summary>
        public ExperienceGroup expGroup;
        /// <summary>
        /// The catch rate of the battler
        /// </summary>
        [Space] [Header("Stats")] public int catchRate;
        /// <summary>
        /// The base friendship of the battler
        /// </summary>
        public int baseFriendship;
        /// <summary>
        /// The base stats of the battler
        /// </summary>
        public BattlerStats baseStats;
        /// <summary>
        /// The exp yield of the battler
        /// </summary>
        public int expYield;
        /// <summary>
        /// The IV yields of the battler
        /// </summary>
        public BattlerStats yields;

        public virtual string GetName()
        {
            return name;
        }

        public virtual BasicType GetPrimaryType()
        {
            return primaryType;
        }

        public virtual BasicType GetSecondaryType()
        {
            return secondaryType;
        }

        public virtual Sprites GetTexture()
        {
            return texture;
        }

        public virtual int GetDexNo()
        {
            return dexNo;
        }

        public virtual PossibleMoves GetPossibleMoves()
        {
            return possibleMoves;
        }

        public virtual Evolution GetEvolutions()
        {
            return evolutions;
        }

        public virtual ExperienceGroup GetExpGroup()
        {
            return expGroup;
        }

        public virtual int GetCatchRate()
        {
            return catchRate;
        }

        public virtual int GetBaseFriendship()
        {
            return baseFriendship;
        }

        public virtual BattlerStats GetBaseStats()
        {
            return baseStats;
        }

        public virtual int GetExpYield()
        {
            return expYield;
        }

        public virtual BattlerStats GetYields()
        {
            return yields;
        }

        #region Auto Complete Stuff
        
        public void TryFillInfo()
        {
            if (pokeClient == null)
            {
                pokeClient = new PokeApiClient();
            }
            
            FillBasicInfoAsync();

            FillMoveAsync();
            FillEvolutionsAsync();
        }

        private async void FillBasicInfoAsync()
        {
            await FillBasicInfo(this);
        }

        private static async Task FillBasicInfo(BattlerTemplate template)
        {
            /*
              TO FILL:
              Primary Type --------DONE
              Secondary Type ------DONE
              Catch Rate ----------DONE
              All Base stats  -----DONE
              Base friendship -----DONE
              Dex Number ----------DONE
              All Texture inf -----DONE
              Exp Group -----------DONE
              All Yields ----------DONE
              Exp Yield -----------DONE
             */
            
            Pokemon pokemon = await pokeClient.GetResourceAsync<Pokemon>(template.name.ToLower());
            PokemonSpecies species = await pokeClient.GetResourceAsync<PokemonSpecies>(template.name.ToLower());
            template.baseFriendship = species.BaseHappiness.HasValue ? species.BaseHappiness.Value : 0;
            template.expYield = pokemon.BaseExperience.HasValue ? pokemon.BaseExperience.Value : 0;
            template.catchRate = species.CaptureRate.HasValue ? species.CaptureRate.Value : 0;

            template.baseStats.maxHealth = pokemon.Stats[0].BaseStat;
            template.baseStats.attack = pokemon.Stats[1].BaseStat;
            template.baseStats.defense = pokemon.Stats[2].BaseStat;
            template.baseStats.specialAttack = pokemon.Stats[3].BaseStat;
            template.baseStats.specialDefense = pokemon.Stats[4].BaseStat;
            template.baseStats.speed = pokemon.Stats[5].BaseStat;
            
            template.yields.maxHealth = pokemon.Stats[0].Effort;
            template.yields.attack = pokemon.Stats[1].Effort;
            template.yields.defense = pokemon.Stats[2].Effort;
            template.yields.specialAttack = pokemon.Stats[3].Effort;
            template.yields.specialDefense = pokemon.Stats[4].Effort;
            template.yields.speed = pokemon.Stats[5].Effort;

            template.primaryType = ToEnum<BasicType>(pokemon.Types[0].Type.Name);
            if (pokemon.Types.Count > 1)
                template.secondaryType = ToEnum<BasicType>(pokemon.Types[0].Type.Name);
            else
                template.secondaryType = BasicType.None;

            Debug.Log(species.PokedexNumbers[0].EntryNumber);
            template.dexNo = species.PokedexNumbers[0].EntryNumber; // national dex
            template.expGroup = ToEnum<ExperienceGroup>(species.GrowthRate.Name.Replace("-",""));

            Sprites texture = new Sprites();
            texture.basic = Resources.Load<Sprite>($"Pokemon Game/sprites/{template.dexNo}");
            texture.shiny = Resources.Load<Sprite>($"Pokemon Game/sprites/shiny/{template.dexNo}");
            texture.female = Resources.Load<Sprite>($"Pokemon Game/sprites/female/{template.dexNo}");
            texture.femaleShiny = Resources.Load<Sprite>($"Pokemon Game/sprites/shiny/female/{template.dexNo}");
            texture.basicBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/{template.dexNo}");
            texture.shinyBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/shiny/{template.dexNo}");
            texture.femaleBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/female/{template.dexNo}");
            texture.femaleShinyBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/shiny/female/{template.dexNo}");
            template.texture = texture;
        }
        
        public static T ToEnum<T>(string value)
        {
            return (T) Enum.Parse(typeof(T), value, true);
        }

        public async void FillEvolutionsAsync()
        {
            await FillEvolutions(this);
        }

        private static void FindInTree(ChainLink link, string target, out ChainLink found)
        {
            found = null;
            if (link.Species.Name == target)
            {
                found = link;
            }

            if (found == null)
            {
                foreach (var newLink in link.EvolvesTo)
                {
                    FindInTree(newLink, target, out found);
                }
            }
        }

        private static async Task FillEvolutions(BattlerTemplate template)
        {
            PokemonSpecies species = await pokeClient.GetResourceAsync<PokemonSpecies>(template.name.ToLower());

            EvolutionChain chain = await pokeClient.GetResourceAsync<EvolutionChain>(species.EvolutionChain);
            
            template.evolutions.possibleEvolutions.Clear();
            
            FindInTree(chain.Chain, species.Name, out ChainLink found);

            if (found != null)
            {
                foreach (var evolvesTo in found.EvolvesTo)
                {
                    NewEvolution(evolvesTo, template);
                }
            }
        }

        private static void NewEvolution(ChainLink link, BattlerTemplate template)
        {
            string method = link.EvolutionDetails[0].Trigger.Name;
            
            EvolutionData data = new EvolutionData();
            data.evolution = Registry.GetBattlerTemplate(link.Species.Name);

            EvolutionDetail evoDets = link.EvolutionDetails[0];
            
            switch (method)
            {
                case "level-up":
                    data.triggerType = EvolutionTriggerType.Level;
                    break;
                case "use-item":
                    data.triggerType = EvolutionTriggerType.UseItem;
                    break;
                case "trade":
                    data.triggerType = EvolutionTriggerType.Trade;
                    break;
            }

            data.minLevel = evoDets.MinLevel ?? -1;
            data.heldItem = evoDets.HeldItem != null ? Registry.GetItem(evoDets.HeldItem.Name.Replace("-", " ")) : null;
            data.useItem = evoDets.Item != null ? Registry.GetItem(evoDets.Item.Name.Replace("-", " ")) : null;
            data.gender = evoDets.Gender != null ? (General.Gender)evoDets.Gender.Value : null;
            data.knownMove = evoDets.KnownMove != null ? Registry.GetMove(evoDets.KnownMove.Name) : null;
            data.knownMoveType = evoDets.KnownMoveType != null ? Registry.GetType(evoDets.KnownMoveType.Name) : null;
            data.minAffection = evoDets.MinAffection ?? -1;
            data.minBeauty = evoDets.MinBeauty ?? -1;
            data.minHappiness = evoDets.MinHappiness ?? -1;
            data.needRain = evoDets.NeedsOverworldRain;
            data.timeOfDay = evoDets.TimeOfDay;
            data.tradeSpecies = evoDets.TradeSpecies != null ? evoDets.TradeSpecies.Name : "";
            
            template.evolutions.possibleEvolutions.Add(data);
        }

        public async void FillMoveAsync()
        {
            await FillMoveInfo(this);
        }

        private static async Task FillMoveInfo(BattlerTemplate template)
        {
            Pokemon pokemon = await pokeClient.GetResourceAsync<Pokemon>(template.name.ToLower());

            // List<PokeApiNet.Move> allMoves = await pokeClient.GetResourceAsync(pokemon.Moves.Select(move => move.Move));

            PossibleMoves newPossibleMoves = new PossibleMoves();

            foreach (var move in pokemon.Moves)
            {
                Move moveCanLearn = Registry.GetMove(move.Move.Name.Replace("-", " "));
                
                // move.VersionGroupDetails[0].
                
                List<VersionGroup> versionGroups =
                    await pokeClient.GetResourceAsync(move.VersionGroupDetails.Select(learnMethod => learnMethod.VersionGroup));

                bool genSeven = false;

                int index = 0;
                
                foreach (var group in versionGroups)
                {
                    if (group.Name == "ultra-sun-ultra-moon")
                    {
                        genSeven = true;
                        break;
                    }

                    index++;
                }

                if (!genSeven)
                {
                    continue;
                }

                MoveLearnMethod moveLearnMethod =
                    await pokeClient.GetResourceAsync(move.VersionGroupDetails[index].MoveLearnMethod);
                    
                bool levelUp = moveLearnMethod.Name == "level-up";
                bool egg = moveLearnMethod.Name == "egg";
                bool tutor = moveLearnMethod.Name == "tutor";
                bool machine = moveLearnMethod.Name == "machine";

                if (levelUp)
                {
                    int level = move.VersionGroupDetails[index].LevelLearnedAt;
                    newPossibleMoves.levelup.Add(moveCanLearn, level);
                }
                else if (egg)
                {
                    newPossibleMoves.egg.Add(moveCanLearn);
                }
                else if (tutor)
                {
                    newPossibleMoves.tutor.Add(moveCanLearn);
                }
                else if (machine)
                {
                    newPossibleMoves.tm.Add(moveCanLearn);
                }
            }

            template.possibleMoves = newPossibleMoves;
        }

        #endregion
    }

    /// <summary>
    /// The possible moves a battler can have
    /// </summary>
    [Serializable]
    public class PossibleMoves
    {
        public SerializableDictionary<Move, int> levelup = new SerializableDictionary<Move, int>();
        public List<Move> tm = new List<Move>();
        public List<Move> egg = new List<Move>();
        public List<Move> tutor = new List<Move>();
    }

    /// <summary>
    /// References to relevant sprites of a battler
    /// </summary>
    [Serializable]
    public class Sprites
    {
        public Sprite basic;
        public Sprite female;
        public Sprite basicBack;
        public Sprite femaleBack;
        public Sprite shiny;
        public Sprite femaleShiny;
        public Sprite shinyBack;
        public Sprite femaleShinyBack;
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();

        [SerializeField] private List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
                throw new System.Exception(string.Format(
                    "there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

            for (int i = 0; i < keys.Count; i++)
                this.Add(keys[i], values[i]);
        }
    }
}