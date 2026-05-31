using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace WFRP_Character_Companion.Models
{
    public class Talent
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<TalentTestEffect> TestEffects { get; set; } = [];

        public TalentMaxLevelType MaxLevelType { get; set; }

        public int? FixedMaxLevel { get; set; }

        public ICollection<AttributeType> MaxLevelAttributes { get; set; } = [];
    }
}
