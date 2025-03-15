using PokemonGame.ScriptableObjects;
using UnityEngine;

namespace PokemonGame.General
{
    public static class ExperienceCalculator
    {
        public static int GetExperienceFromDefeatingBattler(Battler defeated, bool trainer, int noOfBattlersParticipated)
        {
            // Calculations from: https://bulbapedia.bulbagarden.net/wiki/Experience#Experience_gain_in_battle
            
            int exp = 0;

            double a = trainer ? 1.5 : 1;

            // lucky egg
            double e = 1;

            double s = noOfBattlersParticipated;

            double b = defeated.source.yields[0];

            double L = defeated.level;

            exp = (int)(b * L / 7 * 1 / s * e * a);

            return exp;
        }

        public static int RequiredForNextLevel(Battler battler)
        {
            int required = 0;

            if (!battler.source.evolutionTree)
            {
                return -1;
            }

            switch (battler.source.evolutionTree.expGroup)
            {
                case ExperienceGroup.Erratic:
                    required = ErraticGrowth(battler);
                    break;
                case ExperienceGroup.Fast:
                    required = FastGrowth(battler);
                    break;
                case ExperienceGroup.MediumFast:
                    required = MediumFastGrowth(battler);
                    break;
                case ExperienceGroup.MediumSlow:
                    required = MediumSlowGrowth(battler);
                    break;
                case ExperienceGroup.Slow:
                    required = SlowGrowth(battler);
                    break;
                case ExperienceGroup.Fluctuating:
                    required = FluctuatingGrowth(battler);
                    break;
            }

            return required;
        }

        private static int ErraticGrowth(Battler battler)
        {
            int required = 0;

            switch (battler.level)
            {
                case <50:
                    required = ((battler.level * battler.level * battler.level) * (100 - battler.level)) / (50);
                    break;
                case var expression when (50 <= battler.level && battler.level < 68):
                    required = ((battler.level * battler.level * battler.level) * (150 - battler.level)) / (100);
                    break;
                case var expression when (68 <= battler.level && battler.level < 98):
                    required = ((battler.level * battler.level * battler.level) * Mathf.FloorToInt((1911 - 10 * battler.level) / 3f))/(500);
                    break;
                case >= 98:
                    required = ((battler.level * battler.level * battler.level) * (160 - battler.level)) / (100);
                    break;
            }
            
            return required;
        }

        private static int FastGrowth(Battler battler)
        {
            int required = 0;

            required = (4 * (battler.level * battler.level * battler.level)) / (5);
            
            return required;
        }

        private static int MediumFastGrowth(Battler battler)
        {
            int required = 0;

            required = battler.level * battler.level * battler.level;
            
            return required;
        }

        private static int MediumSlowGrowth(Battler battler)
        {
            int required = 0;

            required = (6/5)*(battler.level * battler.level * battler.level) - (15 * battler.level * battler.level) + 100*battler.level - 140;
            
            return required;
        }

        private static int SlowGrowth(Battler battler)
        {
            int required = 0;

            required = (5 * battler.level * battler.level * battler.level) / (4);
            
            return required;
        }

        private static int FluctuatingGrowth(Battler battler)
        {
            int required = 0;

            switch (battler.level)
            {
                case <15:
                    required = (((battler.level * battler.level * battler.level) * (Mathf.FloorToInt((battler.level + 1)/3f) + 24))/50);
                    break;
                case var expression when (15 <= battler.level && battler.level < 36):
                    required = ((battler.level * battler.level * battler.level) * (battler.level + 14)) / (50);
                    break;
                case var expression when (36 <= battler.level && battler.level < 100):
                    required = (((battler.level * battler.level * battler.level) * (Mathf.FloorToInt((battler.level / 2f)) + 32)) / 50);
                    break;
            }
            
            return required;
        }

        public static bool Captured(Battler target, PokeBall ball)
        {
            float statusBonus = 1;

            if (target.statusEffect.name == "Asleep" || target.statusEffect.name == "Frozen")
            {
                statusBonus = 2;
            }

            switch (target.statusEffect.name)
            {
                case "Paralysed":
                    statusBonus = 1.5f;
                    break;
                case "Poisoned":
                    statusBonus = 1.5f;
                    break;
                case "Burned":
                    statusBonus = 1.5f;
                    break;
            }
            
            float a = ((3f*target.maxHealth - 2f * target.currentHealth)/(3f*target.maxHealth)) * target.source.catchRate * ball.bonus * statusBonus;
            
            return a <= Random.Range(0, 255);
        }
    }   
}