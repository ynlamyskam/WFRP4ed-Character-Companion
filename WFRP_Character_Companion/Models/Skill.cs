namespace WFRP_Character_Companion.Models
{
    public class Skill
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsAdvanced { get; set; }

        public bool HasSpecialization { get; set; }

        public AttributeType GoverningAttribute { get; set; }
    }
}
