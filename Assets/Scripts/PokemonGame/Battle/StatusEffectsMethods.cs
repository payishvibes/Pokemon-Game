using PokemonGame.General;

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

        public void Poisoned(StatusEffectEventArgs args)
        {
            args.battler.TakeDamage(1, new EmptyDamageSource());

            Battle.Singleton.QueDialogue($"{args.battler.name} was hurt by poison!", "poisonHurt");
        }
    }   
}