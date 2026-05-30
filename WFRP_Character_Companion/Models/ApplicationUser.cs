using Microsoft.AspNetCore.Identity;

namespace WFRP_Character_Companion.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = "";
        public string? AvatarFileName { get; set; }
    }
}
