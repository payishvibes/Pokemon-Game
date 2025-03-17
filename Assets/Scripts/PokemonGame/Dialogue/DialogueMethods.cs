using System.Collections.Generic;
using PokemonGame.Game;
using PokemonGame.Game.Party;
using PokemonGame.General;
using PokemonGame.Global;
using PokemonGame.ScriptableObjects;

namespace PokemonGame.Dialogue
{
    public class DialogueMethods
    {
        public void HandleGlobalTag(string tagKey, string[] tagValues)
        {
            switch (tagKey)
            {
                case "giveItem":
                    Bag.Add(Registry.GetItem(tagValues[0]), int.Parse(tagValues[1]));
                    break;
                case "heal":
                    PartyManager.HealAll();
                    break;
                case "givebattler":
                    BattlerTemplate template = Registry.GetBattlerTemplate(tagValues[0]);
                    Battler battler = Battler.Init(template, int.Parse(tagValues[1]),
                        template.name, new List<Move>(), true);
                    PartyManager.AddBattler(battler);
                    break;
            }
        }
    }
}