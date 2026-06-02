namespace WFRP_Character_Companion.Models
{
    public class SkillGrant
    {
        public GrantType Type { get; set; }

        public int Choose { get; set; }

        public SkillRef? Skill { get; set; }

        public List<SkillRef> Options { get; set; } = [];
    }
}
