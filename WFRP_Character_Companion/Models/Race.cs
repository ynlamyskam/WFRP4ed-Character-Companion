namespace WFRP_Character_Companion.Models
{
    public class Race
    {
        public string Name { get; set; } = string.Empty;

        public int Movement { get; set; }

        public int MinFate { get; set; }

        public int MinResilience { get; set; }

        public int PointsToSpend { get; set; }
        // Bases for attributes (key is AttributeType name)
        public Dictionary<string,int> Bases { get; set; } = new();

        // Fate / Hero defaults and pools
        public int FateBase { get; set; }
        public int FateToAssign { get; set; }
        public int HeroBase { get; set; }
        public int HeroToAssign { get; set; }
    }
}
