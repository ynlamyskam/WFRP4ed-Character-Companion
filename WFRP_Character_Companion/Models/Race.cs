namespace WFRP_Character_Companion.Models
{
    public class Race
    {
        public string Name { get; set; } = string.Empty;

        public int Movement { get; set; }

        public int MinFate { get; set; }

        public int MinResilience { get; set; }

        public int PointsToSpend { get; set; }
    }
}
