using UnityEngine;

namespace PokemonGame.General
{
    using ScriptableObjects;
    
    public static class ExperienceCalculator
    {
        public static int GetExperienceFromDefeatingBattler(Battler defeated, Battler victor, bool trainer, int noOfBattlersParticipated)
        {
            // Calculations from: https://bulbapedia.bulbagarden.net/wiki/Experience#Experience_gain_in_battle
            
            int exp = 0;

            float a = trainer ? 1.5f : 1f;
            
            float e = 1; // lucky egg

            float s = noOfBattlersParticipated;

            float b = defeated.source.yields[0];

            float L = defeated.level;

            float Lp = victor.level;

            float t = 1;

            float v = 1; // evolution check one, gains more if should be able to evolve

            float f = 1.7f; // friendship bonus

            float p = 1; // rotom exp check

            exp = Mathf.FloorToInt((((b * L) / 5) * (1/s) * Mathf.Pow((2 * L + 10) / (L + Lp + 10), 2.5f) + 1f) * t * e * v * f * p);
            
            return exp;
        }

        public static int RequiredForNextLevel(Battler battler)
        {
            int required = 0;

            switch (battler.source.expGroup)
            {
                case ExperienceGroup.Erratic:
                    required = ErraticGrowth(battler.level+1) - ErraticGrowth(battler.level);
                    break;
                case ExperienceGroup.Fast:
                    required = FastGrowth(battler.level+1) - FastGrowth(battler.level);
                    break;
                case ExperienceGroup.MediumFast:
                    required = MediumFastGrowth(battler.level+1) - MediumFastGrowth(battler.level);
                    break;
                case ExperienceGroup.MediumSlow:
                    required = MediumSlowGrowth(battler.level+1) - MediumSlowGrowth(battler.level);
                    break;
                case ExperienceGroup.Slow:
                    required = SlowGrowth(battler.level+1) - SlowGrowth(battler.level);
                    break;
                case ExperienceGroup.Fluctuating:
                    required = FluctuatingGrowth(battler.level+1) - FluctuatingGrowth(battler.level);
                    break;
            }

            return required;
        }

        private static int ErraticGrowth(int level)
        {
            int required = 0;

            switch (level)
            {
                case <50:
                    required = Mathf.FloorToInt(((level * level * level) * (100 - level)) / (50f));
                    break;
                case var expression when (50 <= level && level < 68):
                    required = Mathf.FloorToInt((level * level * level) * (150 - level) / (100f));
                    break;
                case var expression when (68 <= level && level < 98):
                    required = Mathf.FloorToInt(((level * level * level) * (1911 - 10 * level) / 3f)/(500f));
                    break;
                case >= 98:
                    required = Mathf.FloorToInt((level * level * level) * (160 - level) / (100f));
                    break;
            }
            
            return required;
        }

        private static int FastGrowth(int level)
        {
            int required = 0;

            required = Mathf.FloorToInt(4 * (level * level * level) / (5f));
            
            return required;
        }

        private static int MediumFastGrowth(int level)
        {
            int required = 0;

            required = level * level * level;
            
            return required;
        }

        private static int MediumSlowGrowth(int level)
        {
            int required = 0;

            required = Mathf.FloorToInt(6f / 5f * level * level * level) - 15 * level * level + 100*level - 140;
            
            return required;
        }

        private static int SlowGrowth(int level)
        {
            int required = 0;

            required = Mathf.FloorToInt(5 * level * level * level / (4f));
            
            return required;
        }

        private static int FluctuatingGrowth(int level)
        {
            int required = 0;

            switch (level)
            {
                case <15:
                    required = Mathf.FloorToInt(((level * level * level) * (((level + 1)/3f) + 24))/50f);
                    break;
                case var expression when (15 <= level && level < 36):
                    required = Mathf.FloorToInt(((level * level * level) * (level + 14)) / (50f));
                    break;
                case var expression when (36 <= level && level < 100):
                    required = Mathf.FloorToInt(((level * level * level) * (((level / 2f)) + 32)) / 50f);
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