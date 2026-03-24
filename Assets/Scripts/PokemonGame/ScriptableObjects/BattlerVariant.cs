using PokemonGame.General;
using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.ScriptableObjects
{
    [CreateAssetMenu(order = 2, fileName = "New Battler Variant", menuName = "Pokemon Game/New Battler Variant")]
    public class BattlerVariant : BattlerTemplate
    {
        public BattlerTemplate rootTemplate;
        public string variantPrefix;
        public string variantSuffix;

        public bool overrideName;
        public bool overridePrimaryType;
        public bool overrideSecondaryType;
        public bool overrideTexture;
        public bool overrideDexNo;
        public bool overridePossibleMoves;
        public bool overrideEvolutions;
        public bool overrideExpGroup;
        public bool overrideCatchRate;
        public bool overrideBaseFriendship;
        public bool overrideBaseStats;
        public bool overrideExpYield;
        public bool overrideYields;

        public override string GetName()
        {
            if (name != rootTemplate.GetName() && overrideName)
                return name;
            return base.GetName();
        }

        public override BasicType GetPrimaryType()
        {
            if (primaryType != rootTemplate.GetPrimaryType() && overridePrimaryType)
                return primaryType;
            return base.GetPrimaryType();
        }

        public override BasicType GetSecondaryType()
        {
            if (secondaryType != rootTemplate.GetSecondaryType() && overrideSecondaryType)
                return secondaryType;
            return base.GetSecondaryType();
        }

        public override Sprites GetTexture()
        {
            if (texture != rootTemplate.GetTexture() && overrideTexture)
                return texture;
            return base.GetTexture();
        }

        public override int GetDexNo()
        {
            if (dexNo != rootTemplate.GetDexNo() && overrideDexNo)
                return dexNo;
            return base.GetDexNo();
        }

        public override PossibleMoves GetPossibleMoves()
        {
            if (possibleMoves != rootTemplate.GetPossibleMoves() && overridePossibleMoves)
                return possibleMoves;
            return base.GetPossibleMoves();
        }

        public override Evolution GetEvolutions()
        {
            if (evolutions != rootTemplate.GetEvolutions() && overrideEvolutions)
                return evolutions;
            return base.GetEvolutions();
        }

        public override ExperienceGroup GetExpGroup()
        {
            if (expGroup != rootTemplate.GetExpGroup() && overrideExpGroup)
                return expGroup;
            return base.GetExpGroup();
        }

        public override int GetCatchRate()
        {
            if (catchRate != rootTemplate.GetCatchRate() && overrideCatchRate)
                return catchRate;
            return base.GetCatchRate();
        }

        public override int GetBaseFriendship()
        {
            if (baseFriendship != rootTemplate.GetBaseFriendship() && overrideBaseFriendship)
                return baseFriendship;
            return base.GetBaseFriendship();
        }

        public override BattlerStats GetBaseStats()
        {
            if (!baseStats.Equals(rootTemplate.GetBaseStats()) && overrideBaseStats)
                return baseStats;
            return base.GetBaseStats();
        }

        public override int GetExpYield()
        {
            if (expYield != rootTemplate.GetExpYield())
                return expYield;
            return base.GetExpYield();
        }

        public override BattlerStats GetYields()
        {
            if (!yields.Equals(rootTemplate.GetYields()) && overrideYields)
                return yields;
            return base.GetYields();
        }
    }
}
