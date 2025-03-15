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
        public EvolutionTree evolutionTree;
        public Sprite texture;
        public int dexNo;
        [Space] 
        [Header("Stats")] 
        public int catchRate;
        public int baseHealth;
        public int baseAttack;
        public int baseDefense;
        public int baseSpecialAttack;
        public int baseSpecialDefense;
        public int baseSpeed;
        public List<int> yields;
        
        public void TryFillInfo()
        {
            StreamReader streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\pokemon.csv");
            bool found = false;
            bool endOfFile = false;
            string data = "";
            int dexNo = 0;
            int index = 0;
            while (!found && !endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (dataString == null)
                {
                    endOfFile = true;
                    break;
                }

                var dataValues = Regex.Split(dataString, @"(?<=[^']),(?=[^'])", RegexOptions.None);
                
                if (dataValues[30] == name)
                {
                    found = true;
                    data = dataString;
                    dexNo = index;
                }
                index++;
            }
            
            streamReader.Close();

            if (found)
            {
                var values = Regex.Split(data, @"(?<=[^']),(?=[^'])", RegexOptions.None);
                
                primaryType = (BasicType) Enum.Parse(typeof(BasicType), values[36], true);
                if (!string.IsNullOrEmpty(values[37]))
                {
                    secondaryType = (BasicType) Enum.Parse(typeof(BasicType), values[37], true);
                }
                
                catchRate = int.Parse(values[23]);
                baseHealth = int.Parse(values[28]);
                baseAttack = int.Parse(values[19]);
                baseDefense = int.Parse(values[25]);
                baseSpecialAttack = int.Parse(values[32]);
                baseSpecialDefense = int.Parse(values[33]);
                baseSpeed = int.Parse(values[34]);
                this.dexNo = dexNo;

                evolutionTree = Resources.Load<EvolutionTree>($"Pokemon Game/EvolutionTrees/{name}");
                texture = Resources.Load<Sprite>($"Pokemon Game/Sprites/{dexNo}");
            }
            
            streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\yield values.csv");
            found = false;
            endOfFile = false;
            data = "";
            while (!found && !endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (dataString == null)
                {
                    endOfFile = true;
                    break;
                }

                var dataValues = dataString.Split(',');
                if (dataValues[0] == name)
                {
                    found = true;
                    data = dataString;
                }
            }
            
            streamReader.Close();
            
            if (found)
            {
                var stats = data.Split(',');

                while (yields.Count < 8)
                {
                    yields.Add(0);
                }

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
    }
}