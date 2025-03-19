using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace PokemonGame.Battle
{
    using UnityEngine;
    using ScriptableObjects;

    /// <summary>
    /// Contains all the logic for every move
    /// </summary>
    [CreateAssetMenu(fileName = "New Item Methods", menuName = "All/New Item Methods")]
    public class ItemMethods : ScriptableObject
    {
        public void CreateItems()
        {
            DateTime start = DateTime.Now;
            StreamReader streamReader =
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\items.csv");
            bool endOfFile = false;
            while (!endOfFile)
            {
                string dataString = streamReader.ReadLine();
                if (string.IsNullOrEmpty(dataString))
                {
                    endOfFile = true;
                    break;
                }
                
                // var values = Regex.Split(dataString, @",(?=[^0])", RegexOptions.None);
                var values = dataString.Split(",");

                if (string.IsNullOrEmpty(values[1]))
                {
                    break;
                }
                
                Item item = CreateInstance<Item>();
                
                item.type = (ItemType)Enum.Parse(typeof(ItemType), values[1].Replace(" ", ""), true);
                item.name = values[0];
                item.description = values[2];
                
                AssetDatabase.CreateAsset(item, $"Assets/Resources/Pokemon Game/Item//{item.name}.asset");
                endOfFile = true;
            }
            
            streamReader.Close();
            
            AssetDatabase.SaveAssets();
        }
        
        public void Potion(ItemMethodEventArgs e)
        {
            if (!e.target.isFainted)
            {
                e.target.Heal(20);
                e.success = true;
            }
        }
        
        public void Revive(ItemMethodEventArgs e)
        {
            if (e.target.isFainted)
            {
                e.target.Revive(false);
                e.success = true;
            }
        }
        
        public void MaxRevive(ItemMethodEventArgs e)
        {
            if (e.target.isFainted)
            {
                e.target.Revive(true);
                e.success = true;
            }
        }
    }   
}