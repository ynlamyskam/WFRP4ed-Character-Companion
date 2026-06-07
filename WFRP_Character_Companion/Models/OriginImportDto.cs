using System.Text.Json.Serialization;

namespace WFRP_Character_Companion.Models
{
    public class OriginImportDto
    {
        [JsonPropertyName("race")]
        public string Race { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("package")]
        public OriginPackageDto Package { get; set; }
    }

    public class OriginPackageDto
    {
        [JsonPropertyName("skills")]
        public List<SkillGrantDto> Skills { get; set; } = new();

        [JsonPropertyName("talents")]
        public List<TalentGrantDto> Talents { get; set; } = new();
    }

    public class SkillGrantDto
    {
        [JsonPropertyName("type")]
        public GrantType? Type { get; set; } = GrantType.Fixed;

        [JsonPropertyName("choose")]
        public int? Choose { get; set; } = 1;

        [JsonPropertyName("skill")]
        public SkillRefDto? Skill { get; set; }

        [JsonPropertyName("options")]
        public List<SkillRefDto>? Options { get; set; } = new();
    }

    public class SkillRefDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("specialization")]
        public string? Specialization { get; set; }
    }

    public class TalentGrantDto
    {
        [JsonPropertyName("type")]
        public TalentGrantType? Type { get; set; }

        [JsonPropertyName("choose")]
        public int? Choose { get; set; } = 1;

        [JsonPropertyName("count")]
        public int? Count { get; set; }

        [JsonPropertyName("talent")]
        public TalentRefDto? Talent { get; set; }

        [JsonPropertyName("options")]
        public List<TalentRefDto>? Options { get; set; } = new();
    }

    public class TalentRefDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("specialization")]
        public string? Specialization { get; set; }
    }
}
