using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WFRP_Character_Companion.Models
{
    public class Character
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = default!;

        public ICollection<CharacterAttribute> Attributes { get; set; }
            = [];

        public ICollection<CharacterSkill> Skills { get; set; }
        = [];

        public ICollection<CharacterTalent> Talents { get; set; }
            = [];


        public CharacterSkill? GetSkill(int skillId)
        {
            return Skills.FirstOrDefault(s => s.SkillId == skillId);
        }

        public int GetSkillTotal(Skill skill)
        {
            var advances = Skills.FirstOrDefault(s => s.SkillId == skill.Id)?.Advances ?? 0;

            var attribute = Attributes.First(a => a.Type == skill.GoverningAttribute);

            return attribute.Basic + attribute.Advance + advances;
        }
    }
}
