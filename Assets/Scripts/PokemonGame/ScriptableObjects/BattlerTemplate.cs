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
        public int baseHealth;
        public int baseAttack;
        public int baseDefense;
        public int baseSpecialAttack;
        public int baseSpecialDefense;
        public int baseSpeed;
        public List<int> yields;

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
                baseHealth = int.Parse(values[23]);
                baseAttack = int.Parse(values[24]);
                baseDefense = int.Parse(values[25]);
                baseSpecialAttack = int.Parse(values[26]);
                baseSpecialDefense = int.Parse(values[27]);
                baseSpeed = int.Parse(values[28]);
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

                while (yields.Count < 8)
                {
                    yields.Add(0);
                }

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

                yields[0] = int.Parse(stats[1]);
                yields[1] = int.Parse(stats[2]);
                yields[2] = int.Parse(stats[3]);
                yields[3] = int.Parse(stats[4]);
                yields[4] = int.Parse(stats[5]);
                yields[5] = int.Parse(stats[6]);
                yields[6] = int.Parse(stats[7]);
                yields[7] = int.Parse(stats[8]);
            }
        }

        public async void FillEvolutionsAsync()
        {
            await FillEvolutions(this);
        }

        private static void FindInTree(ChainLink link, string target, out ChainLink found)
        {
            found = null;
            Debug.Log(link.Species.Name);
            Debug.Log(target);
            Debug.Log(link.Species.Name == target);
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
            PokemonSpecies pokemon = await pokeClient.GetResourceAsync<PokemonSpecies>(template.name.ToLower());

            EvolutionChain chain = await pokeClient.GetResourceAsync<EvolutionChain>(pokemon.EvolutionChain);
            
            template.evolutions.possibleEvolutions.Clear();
            
            FindInTree(chain.Chain, pokemon.Name, out ChainLink found);

            foreach (var evolvesTo in found.EvolvesTo)
            {
                await NewEvolution(evolvesTo, template);
            }
        }

        private static async Task NewEvolution(ChainLink link, BattlerTemplate template)
        {
            string method = link.EvolutionDetails[0].Trigger.Name;
            
            EvolutionData data = new EvolutionData();
            data.evolution = Registry.GetBattlerTemplate(link.Species.Name);

            if (method == "level-up")
            {
                data.level = link.EvolutionDetails[0].MinLevel ?? 0;
                data.triggerType = EvolutionTriggerType.Level;
            }
            else if (method == "use-item")
            {
                data.item = Registry.GetItem(link.EvolutionDetails[0].HeldItem.Name);
                data.triggerType = EvolutionTriggerType.Item;
            }
            
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