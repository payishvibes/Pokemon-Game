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
                new StreamReader("C:\\Users\\Mr. Monster\\Documents\\Pokemon Gen 7 database\\pokeballs.csv");
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
                dataString = dataString.Replace("\"", "");
                dataString = dataString.Replace("é", "e");
                var values = Regex.Split(dataString, @",(?=[^ ])", RegexOptions.None);
                
                if (values[0] == "???")
                {
                    continue;
                }
                
                PokeBall item = CreateInstance<PokeBall>();
                
                item.type = ItemType.PokeBalls;
                item.name = values[0];
                item.description = values[2];
                item.bonus = float.Parse(values[1]);
                item.sprite = Resources.Load<Sprite>($"Pokemon Game/Sprites/items/{values[0].ToLower().Replace(" ", "-")}");
                if (!item.sprite)
                {
                    Debug.LogWarning("No sprite found, possible spelling difference or missing sprite");
                }

                if (values.Length > 4)
                {
                    Debug.LogWarning("More than 2 entries, possible faulty description");
                }
                
                AssetDatabase.CreateAsset(item, $"Assets/Resources/Pokemon Game/Item/PokeBalls/{item.name}.asset");
                
                Debug.Log($"Created Item: {item.name}");
            
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