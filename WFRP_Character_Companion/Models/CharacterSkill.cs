namespace WFRP_Character_Companion.Models
{
    public class CharacterSkill
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public Character Character { get; set; } = default!;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = default!;

        public string? Specialization { get; set; }

        public int Advances { get; set; }
    }
}
