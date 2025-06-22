using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PokeApiNet;
using PokemonGame.General;
using PokemonGame.Global;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(order = 1, fileName = "New Battler Template", menuName = "Pokemon Game/New Battler Template")]
    public class BattlerTemplate : ScriptableObject
    {
        public static PokeApiClient pokeClient;

        public new string name;
        public BasicType primaryType;
        public BasicType secondaryType;
        public Sprites texture;
        public int dexNo;
        public PossibleMoves possibleMoves;
        public Evolution evolutions;
        public ExperienceGroup expGroup;
        [Space] [Header("Stats")] public int catchRate;
        public int baseFriendship;
        public BattlerStats baseStats;
        public int expYield;
        public BattlerStats yields;

        public void TryFillInfo()
        {
            FillBasicInfo();
            FillYieldInfo();

            FillMoveAsync();
            FillEvolutionsAsync();
        }

        private void FillBasicInfo()
        {
            StreamReader streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\Pokemon Database.csv");
            bool found = false;
            bool endOfFile = false;
            string data = "";
            int dexNo = 0;
            while (!found && !endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (dataString == null)
                {
                    endOfFile = true;
                    break;
                }

                var dataValues = Regex.Split(dataString, @"(?<=[^ ]),(?=[^ ])", RegexOptions.None);

                if (dataValues[2].Replace("\"", "") == name)
                {
                    found = true;
                    data = dataString;
                    dexNo = int.Parse(dataValues[1]);
                }
            }

            streamReader.Close();

            if (found)
            {
                data = data.Replace("\"", "");
                data = data.Replace("NULL", "");
                var values = Regex.Split(data, @"(?<=[^ ]),(?=[^ ])", RegexOptions.None);

                primaryType = (BasicType)Enum.Parse(typeof(BasicType), values[9], true);
                if (!string.IsNullOrEmpty(values[10]))
                {
                    secondaryType = (BasicType)Enum.Parse(typeof(BasicType), values[10], true);
                }

                catchRate = int.Parse(values[37]);
                baseStats.maxHealth = int.Parse(values[23]);
                baseStats.attack = int.Parse(values[24]);
                baseStats.defense = int.Parse(values[25]);
                baseStats.specialAttack = int.Parse(values[26]);
                baseStats.specialDefense = int.Parse(values[27]);
                baseStats.speed = int.Parse(values[28]);
                baseFriendship = int.Parse(values[21]);
                this.dexNo = dexNo;

                texture = new Sprites();

                texture.basic = Resources.Load<Sprite>($"Pokemon Game/sprites/{dexNo}");
                texture.shiny = Resources.Load<Sprite>($"Pokemon Game/sprites/shiny/{dexNo}");
                texture.female = Resources.Load<Sprite>($"Pokemon Game/sprites/female/{dexNo}");
                texture.femaleShiny = Resources.Load<Sprite>($"Pokemon Game/sprites/shiny/female/{dexNo}");
                texture.basicBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/{dexNo}");
                texture.shinyBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/shiny/{dexNo}");
                texture.femaleBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/female/{dexNo}");
                texture.femaleShinyBack = Resources.Load<Sprite>($"Pokemon Game/sprites/back/shiny/female/{dexNo}");

                expGroup = (ExperienceGroup)Enum.Parse(typeof(ExperienceGroup), values[38].Replace(" ", ""), true);
            }
        }

        private void FillYieldInfo()
        {
            Debug.Log("getting yield info");
            
            StreamReader streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\yield values.csv");
            bool found = false;
            bool endOfFile = false;
            string data = "";
            while (!found && !endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (dataString == null)
                {
                    endOfFile = true;
                    break;
                }

                var dataValues = Regex.Split(dataString, @"(?<=[^']),(?=[^'])", RegexOptions.None);

                if (dataValues[0].Replace("\"", "") == name)
                {
                    found = true;
                    data = dataString;
                }
            }

            streamReader.Close();

            if (found)
            {
                data = data.Replace("\"", "");
                var stats = Regex.Split(data, @"(?<=[^']),(?=[^'])", RegexOptions.None);

                expYield = int.Parse(stats[1]);
                yields.maxHealth = int.Parse(stats[2]);
                yields.attack = int.Parse(stats[3]);
                yields.defense = int.Parse(stats[4]);
                yields.specialAttack = int.Parse(stats[5]);
                yields.specialDefense = int.Parse(stats[6]);
                yields.speed = int.Parse(stats[7]);
            }
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

            if (method == "level-up")
            {
                data.triggerType = EvolutionTriggerType.Level;
            }
            else if (method == "use-item")
            {
                data.triggerType = EvolutionTriggerType.UseItem;
            }
            else if (method == "trade")
            {
                data.triggerType = EvolutionTriggerType.Trade;
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
            if (pokeClient == null)
            {
                pokeClient = new PokeApiClient();
            }
            
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
    }

    [Serializable]
    public class PossibleMoves
    {
        public SerializableDictionary<Move, int> levelup = new SerializableDictionary<Move, int>();
        public List<Move> tm = new List<Move>();
        public List<Move> egg = new List<Move>();
        public List<Move> tutor = new List<Move>();
    }

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