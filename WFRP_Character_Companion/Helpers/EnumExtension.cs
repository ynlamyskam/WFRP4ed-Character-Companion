using System.ComponentModel.DataAnnotations;

namespace WFRP_Character_Companion.Helpers
{
    public static class EnumExtension
    {
        public static string GetDisplayName(this Enum value)
        {
            return value.GetType()
                .GetMember(value.ToString())
                .First()
                .GetCustomAttributes(false)
                .OfType<DisplayAttribute>()
                .FirstOrDefault()?.Name
                ?? value.ToString();
        }
    }
}
