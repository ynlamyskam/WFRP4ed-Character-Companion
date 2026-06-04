using System.Text.Json.Serialization;

namespace WFRP_Character_Companion.Models.Import
{
    public class TalentImportDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("maxLevelType")]
        public TalentMaxLevelType MaxLevelType { get; set; } = TalentMaxLevelType.AttributeBonus;

        [JsonPropertyName("fixedMaxLevel")]
        public int? FixedMaxLevel { get; set; }

        [JsonPropertyName("maxLevelAttributes")]
        public List<AttributeType>? MaxLevelAttributes { get; set; }

        [JsonPropertyName("tests")]
        public List<TalentTestEffectDto>? Tests { get; set; }
    }

    public class TalentTestEffectDto
    {
        public string? Skill { get; set; }

        public string? Condition { get; set; }

        public int BonusPerLevelAbove1 { get; set; } = 1;
    }
}
