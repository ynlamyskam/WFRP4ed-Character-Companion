namespace WFRP_Character_Companion.Models.ViewModels
{
    public class CreateCampaignInput
    {
        public string Name { get; set; } = string.Empty;

        public List<int> CharacterIds { get; set; } = [];

        public string? MemberEmail { get; set; }
    }

    public class CampaignListItemVm
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsOwner { get; set; }

        /// <summary>Twórca lub MG — może edytować kampanię i zapraszać graczy.</summary>
        public bool CanManage { get; set; }

        public List<CampaignMemberVm> Members { get; set; } = [];
    }

    public class CampaignMemberVm
    {
        public string UserId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public List<CampaignCharacterVm> Characters { get; set; } = [];
    }

    public class CampaignCharacterVm
    {
        public int CharacterId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
