namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;


    [CreateAssetMenu(fileName = "New Item", menuName = "Pokemon Game/New TM")]
    public class TM : Item
    {
        public Move move;
    }
}