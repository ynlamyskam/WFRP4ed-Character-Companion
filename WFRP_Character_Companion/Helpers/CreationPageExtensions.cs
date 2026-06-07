using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WFRP_Character_Companion.Helpers
{
    public static class CreationPageExtensions
    {
        public const string ErrorKey = "ErrorMessage";

        public static IActionResult PageWithError(this PageModel page, string message)
        {
            page.ViewData[ErrorKey] = message;
            return page.Page();
        }
    }
}
