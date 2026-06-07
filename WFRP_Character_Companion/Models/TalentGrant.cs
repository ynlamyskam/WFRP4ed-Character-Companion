namespace WFRP_Character_Companion.Models
{
    public class TalentGrant
    {
        public TalentGrantType Type { get; set; }

        public int Choose { get; set; }

        public int Count { get; set; }

        public TalentRef? Talent { get; set; }

        public List<TalentRef> Options { get; set; } = [];
    }
}
