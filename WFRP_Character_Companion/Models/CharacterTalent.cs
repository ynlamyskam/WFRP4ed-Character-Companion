namespace WFRP_Character_Companion.Models
{
    public class CharacterTalent
    {
        public int Id { get; set; }

        public int CharacterId { get; set; }
        public Character Character { get; set; } = default!;

        public int TalentId { get; set; }
        public Talent Talent { get; set; } = default!;

        public int Level { get; set; } = 1;
    }
}
