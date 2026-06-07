namespace WFRP_Character_Companion.Models
{
    public class RaceBase
    {
        public string? Name { get; set; }
        public Dictionary<string, int> Bases { get; set; } = new();
    }
}
