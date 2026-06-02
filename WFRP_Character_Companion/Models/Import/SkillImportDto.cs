using System.Text.Json.Serialization;

namespace WFRP_Character_Companion.Models.Import
{
    public class SkillImportDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("isAdvanced")]
        public bool IsAdvanced { get; set; } = false;

        [JsonPropertyName("hasSpecialization")]
        public bool HasSpecialization { get; set; } = false;

        [JsonPropertyName("governingAttribute")]
        public AttributeType GoverningAttribute { get; set; }
    }
}
