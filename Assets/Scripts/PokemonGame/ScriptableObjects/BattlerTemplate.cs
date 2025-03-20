using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(order = 1, fileName = "New Battler Template", menuName = "Pokemon Game/New Battler Template")]
    public class BattlerTemplate : ScriptableObject
    {
        public new string name;
        public BasicType primaryType;
        public BasicType secondaryType;
        public Sprites texture;
        public int dexNo;
        public PossibleMoves possibleMoves;
        public Evolution evolutions;
        public ExperienceGroup expGroup;
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
            
            FillBasicInfo();
            FillYieldInfo();
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
            
        }
    }

    [Serializable]
    public class PossibleMoves
    {
        public Dictionary<Move, int> levelup;
        public Dictionary<Move, int> tm;
        public List<Move> egg;
        public List<Move> tutor;
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
}