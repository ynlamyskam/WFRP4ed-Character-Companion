using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Services
{
    public class TalentRulesService
    {
        public int GetCharacteristicBonus(int value)
        => value / 10;

        public int GetMaxLevel(Character character, Talent talent)
        {
            return talent.MaxLevelType switch
            {
                TalentMaxLevelType.Fixed =>
                    talent.FixedMaxLevel ?? 1,

                TalentMaxLevelType.AttributeBonus =>
                    talent.MaxLevelAttributes.Sum(attr =>
                    {
                        var a = character.Attributes.First(x => x.Type == attr);
                        return GetCharacteristicBonus(a.Basic + a.Advance);
                    }),

                TalentMaxLevelType.None =>
                    int.MaxValue,

                _ => 1
            };
        }

        public int GetTestBonus(CharacterTalent t, string skill, string? context)
        {
            var effect = t.Talent.TestEffects
                .FirstOrDefault(e => e.SkillName == skill);

            if (effect == null)
                return 0;

            if (!string.IsNullOrEmpty(effect.Condition))
            {
                if (string.IsNullOrEmpty(context))
                    return 0;

                if (!context.Contains(effect.Condition, StringComparison.OrdinalIgnoreCase))
                    return 0;
            }

            return (t.Level - 1) * effect.BonusPerLevelAbove1;
        }
    }
}
