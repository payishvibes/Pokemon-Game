using PokemonGame.Dialogue;

namespace PokemonGame.Battle
{
    using UnityEngine;
    using ScriptableObjects;

    /// <summary>
    /// Contains all the logic for every status effect
    /// </summary>
    [CreateAssetMenu(fileName = "New All Status Effects", menuName = "All/New All Status Effect Methods")]
    public class StatusEffectsMethods : ScriptableObject
    {
        public void Healthy(StatusEffectEventArgs args)
        {
            //Debug.Log(args.battler.name + " was healthy");
        }
        
        public void Asleep(StatusEffectEventArgs args)
        {
            if (args.battler.sleepTurns <= 0)
            {
                // wake up
                args.battler.sleepTurns = 0;
                args.battler.statusEffect = StatusEffect.Healthy;
                
                Battle.Singleton.QueDialogue($"{args.battler.name} woke up!", DialogueBoxType.Event, "generalFinishing");
            }
            else
            {
                args.battler.sleepTurns--;
                Battle.Singleton.TurnQueueItemEnded();
            }
        }

        public void Poisoned(StatusEffectEventArgs args)
        {
            args.battler.TakeDamage(Mathf.CeilToInt(args.battler.stats.maxHealth/8f), new StatusEffectDamageSource(args.battler.statusEffect));

            Battle.Singleton.QueDialogue($"{args.battler.name} was hurt by poison!", DialogueBoxType.Event, "generalFinishing");
        }

        public void Burn(StatusEffectEventArgs args)
        {
            args.battler.TakeDamage(Mathf.CeilToInt(args.battler.stats.maxHealth/8f), new StatusEffectDamageSource(args.battler.statusEffect));

            Battle.Singleton.QueDialogue($"{args.battler.name} was burned!", DialogueBoxType.Event, "generalFinishing");
        }
    }   
}