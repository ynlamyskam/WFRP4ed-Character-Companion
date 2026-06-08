using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.Import;

namespace WFRP_Character_Companion.Services.Content
{
    public static class TalentImportMapper
    {
        public static Talent Map(TalentImportDto dto) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            MaxLevelType = dto.MaxLevelType,
            FixedMaxLevel = dto.FixedMaxLevel,
            MaxLevelAttributes = dto.MaxLevelAttributes ?? [],
            TestEffects = dto.Tests?.Select(x => new TalentTestEffect
            {
                SkillName = x.Skill,
                Condition = x.Condition,
                BonusPerLevelAbove1 = x.BonusPerLevelAbove1
            }).ToList() ?? []
        };
    }
}
