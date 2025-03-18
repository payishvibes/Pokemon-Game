using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PokemonGame.General;
using PokemonGame.Global;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(order = 1, fileName = "New Battler Template", menuName = "Pokemon Game/New Battler Template")]
    public class BattlerTemplate : ScriptableObject
    {
        public new string name;
        public BasicType primaryType;
        public BasicType secondaryType;
        public EvolutionTree evolutionTree;
        public ExperienceGroup expGroup;
        public Sprite texture;
        public int dexNo;
        public List<PossibleMove> possibleMoves;
        [Space] 
        [Header("Stats")] 
        public int catchRate;
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
            FillMoveInfo();
            
            // FillBasicInfo();
            // FillYieldInfo();
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
                
                primaryType = (BasicType) Enum.Parse(typeof(BasicType), values[9], true);
                if (!string.IsNullOrEmpty(values[10]))
                {
                    secondaryType = (BasicType) Enum.Parse(typeof(BasicType), values[10], true);
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

                evolutionTree = Resources.Load<EvolutionTree>($"Pokemon Game/EvolutionTrees/{name}");
                texture = Resources.Load<Sprite>($"Pokemon Game/Sprites/{dexNo}");
                
                while (yields.Count < 8)
                {
                    yields.Add(0);
                }
                
                expGroup = (ExperienceGroup) Enum.Parse(typeof(ExperienceGroup), values[38].Replace(" ", ""), true);
            }
        }

        private void FillYieldInfo()
        {
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
                
                if (dataValues[2].Replace("\"", "") == name)
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

        private void FillMoveInfo()
        {
            using (StreamReader r = new StreamReader("C:\\Users\\Mr. Monster\\Downloads\\ultrasunultramoon.json"))
            {
                string json = r.ReadToEnd();
                List<PokemonData> items = JsonConvert.DeserializeObject<List<PokemonData>>(json);

                List<MoveData> movesNumbers = items[dexNo - 1].moves.ToList();
                List<PossibleMove> newPossibleMoves = new List<PossibleMove>(); 
                
                for (int i = 0; i < movesNumbers.Count; i++)
                {
                    StreamReader streamReader =
                        new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\moves.csv");
                    bool endOfFile = false;
                    while (!endOfFile)
                    {
                        string dataString = streamReader.ReadLine();
                        if (dataString == null)
                        {
                            endOfFile = true;
                            break;
                        }

                        var dataValues = Regex.Split(dataString, @"(?<=[^']),(?=[^'])", RegexOptions.None);
                
                        if (int.Parse(dataValues[0]) == movesNumbers[i].move)
                        {
                            dataString = dataString.Replace("\"", "");
                            dataValues[1] = dataValues[1].Replace(",", "");

                            PossibleMove newPossibleMove = new PossibleMove();
                            newPossibleMove.move = Registry.GetMove(dataValues[1]);
                            newPossibleMove.throughLevelUp = movesNumbers[i].method == "levelup";
                            if (newPossibleMove.throughLevelUp)
                            {
                                newPossibleMove.level = movesNumbers[i].level;
                            }
                            else
                            {
                                newPossibleMove.item = Registry.GetItem(movesNumbers[i].item);
                            }
                            
                            newPossibleMoves.Add(newPossibleMove);
                        }
                    }
            
                    streamReader.Close();
                }

                possibleMoves = newPossibleMoves;
            }
        }
    }

    public class PokemonData
    {
        public int pokemon;
        public int form;
        public MoveData[] moves;
    }

    public class MoveData
    {
        public int move;
        public string method;
        public int level;
        public string item;
    }

    [Serializable]
    public class PossibleMove
    {
        public Move move;
        public bool throughLevelUp;
        public int level;
        public Item item;
    }
}