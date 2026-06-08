using WFRP_Character_Companion.Helpers;

namespace WFRP_Character_Companion.Models
{
    public class CharacterTalent
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public Character Character { get; set; } = default!;

        public int TalentId { get; set; }
        public Talent Talent { get; set; } = default!;

        public string? Specialization { get; set; }

        public int Level { get; set; } = 1;

        public string DisplayName => CharacterRulesHelper.FormatWithSpecialization(Talent?.Name ?? string.Empty, Specialization);
    }
}
