using PokemonGame.Global;

namespace PokemonGame.ScriptableObjects
{
    using UnityEngine;

    [CreateAssetMenu(order = 3, fileName = "New Type", menuName ="Pokemon Game/New Type")]
    public class Type : ScriptableObject
    {
        public new string name;
        public Color color;
        public Sprite sprite;
        public Type[] strongAgainst;
        public Type[] cantHit;
        public Type[] weakAgainst;

        public static Type FromBasic(BasicType basicType)
        {
            if (basicType != BasicType.None)
            {
                return Registry.GetType(basicType.ToString());
            }

            return null;
        }
    }

    public enum BasicType
    {
        None,
        Bug,
        Dark,
        Dragon,
        Electric,
        Fairy,
        Fighting,
        Fire,
        Flying,
        Ghost,
        Grass,
        Ground,
        Ice,
        Normal,
        Poison,
        Psychic,
        Rock,
        Steel,
        Water
    }
}