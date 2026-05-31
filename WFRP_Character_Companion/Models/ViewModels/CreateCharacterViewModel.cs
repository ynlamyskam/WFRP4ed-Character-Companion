namespace WFRP_Character_Companion.Models.ViewModels
{
    public class CreateCharacterViewModel
    {
        public string Name { get; set; } = string.Empty;

        public List<CharacterAttribute> Attributes { get; set; } = [];

        public List<CreateSkillViewModel> Skills { get; set; } = [];

        public List<CreateTalentViewModel> Talents { get; set; } = [];
    }

    public class CreateSkillViewModel
    {
        public int SkillId { get; set; }
        public int Advances { get; set; }
    }

    public class CreateTalentViewModel
    {
        public int TalentId { get; set; }
        public int Level { get; set; } = 1;
    }
}
