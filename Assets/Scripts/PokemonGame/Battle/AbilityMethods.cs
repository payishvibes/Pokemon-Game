using PokemonGame.Dialogue;
using UnityEngine;

namespace PokemonGame.Battle
{
    using ScriptableObjects;
    
    [CreateAssetMenu(fileName = "New All Ability Methods", menuName = "All/New All Ability Methods")]
    public class AbilityMethods : ScriptableObject
    {
        public void SpeedBoost(AbilityEventArgs args)
        {
            Battle.Singleton.QueDialogue($"{args.Battler.name} speed rose", DialogueBoxType.Event, "generalFinishing");
            args.Battler.modifierStats.speedStage += 1;
        }
    }
}