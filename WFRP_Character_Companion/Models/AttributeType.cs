using System.ComponentModel.DataAnnotations;

namespace WFRP_Character_Companion.Models
{
    public enum AttributeType
    {
        [Display(Name = "WW")]
        WeaponSkill,

        [Display(Name = "US")]
        BallisticSkill,

        [Display(Name = "S")]
        Strength,

        [Display(Name = "Wt")]
        Toughness,

        [Display(Name = "I")]
        Initiative,

        [Display(Name = "Zw")]
        Agility,

        [Display(Name = "Zr")]
        Dexterity,

        [Display(Name = "Int")]
        Intelligence,

        [Display(Name = "SW")]
        Willpower,

        [Display(Name = "Ogd")]
        Fellowship
    }
}
