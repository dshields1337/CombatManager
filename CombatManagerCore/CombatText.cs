using System;

namespace CombatManager
{
    /// <summary>
    /// Platform-neutral formatting and parsing helpers shared by combat models.
    /// </summary>
    public static class CombatText
    {
        public static Stat StatFromName(string name)
        {
            foreach (Stat stat in Enum.GetValues(typeof(Stat)))
            {
                if (string.Equals(StatText(stat), name, StringComparison.OrdinalIgnoreCase))
                {
                    return stat;
                }
            }

            return Stat.Strength;
        }

        public static string StatText(Stat stat)
        {
            switch (stat)
            {
                case Stat.Strength: return "Strength";
                case Stat.Dexterity: return "Dexterity";
                case Stat.Constitution: return "Constitution";
                case Stat.Intelligence: return "Intelligence";
                case Stat.Wisdom: return "Wisdom";
                case Stat.Charisma: return "Charisma";
                default: return null;
            }
        }

        public static DieRoll FindNextDieRoll(string text, int start = 0)
        {
            return DieRoll.FromString(text, start);
        }

        public static string DieRollText(DieRoll roll)
        {
            return roll == null ? "0d0" : roll.Text;
        }
    }
}
