using System.ComponentModel.DataAnnotations;

namespace WFRP_Character_Companion.Models
{
    public class CharacterAttribute
    {
        public int Id { get; set; }

        public AttributeType Type { get; set; }

        public int Basic { get; set; }

        public int Advance { get; set; }

        public int Total => Basic + Advance;

        public int CharacterId { get; set; }

        public Character Character { get; set; } = default!;
    }
}
