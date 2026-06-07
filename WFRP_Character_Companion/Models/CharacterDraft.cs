namespace WFRP_Character_Companion.Models
{
    public class CharacterDraft
    {
        public int Id { get; set; }
        public string UserId { get; set; } = default!;

        public CharacterCreationStep Step { get; set; }

        // Krok 1
        public string? Race { get; set; }
        public string? Origin { get; set; }

        public bool RaceAccepted { get; set; }
        public bool OriginAccepted { get; set; }

        public int Experience { get; set; }

        public string StateJson { get; set; } = "{}";
    }
}
