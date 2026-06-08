using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Helpers
{
    public static class CharacterRulesHelper
    {
        public static int AttributeBonus(int total) => total / 10;

        public static int GetAttributeBonus(Character character, AttributeType type)
        {
            var attr = character.Attributes.FirstOrDefault(a => a.Type == type);
            return attr == null ? 0 : AttributeBonus(attr.Total);
        }

        public static bool HasTalent(Character character, string talentName) =>
            character.Talents.Any(t =>
                string.Equals(t.Talent.Name, talentName, StringComparison.OrdinalIgnoreCase));

        public static int GetMaxWounds(Character character)
        {
            var s = GetAttributeBonus(character, AttributeType.Strength);
            var t = GetAttributeBonus(character, AttributeType.Toughness);
            var w = GetAttributeBonus(character, AttributeType.Willpower);

            if (HasTalent(character, "Mały"))
                return t * 2 + w;

            return s + t * 2 + w;
        }

        public static int GetMaxEncumbrance(Character character)
        {
            var s = GetAttributeBonus(character, AttributeType.Strength);
            var t = GetAttributeBonus(character, AttributeType.Toughness);
            return s + t;
        }

        public const string BasicSpecializationLabel = "Podstawowa";

        public static bool IsCustomSpecializationPlaceholder(string? specialization)
        {
            if (string.IsNullOrWhiteSpace(specialization)) return false;
            var s = specialization.Trim();
            if (s.StartsWith("Dowoln", StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(s, "Lokalna", StringComparison.OrdinalIgnoreCase);
        }

        public static int GetMaxCorruption(Character character) =>
            GetAttributeBonus(character, AttributeType.Willpower) +
            GetAttributeBonus(character, AttributeType.Toughness);

        public static string FormatWithSpecialization(string name, string? specialization) =>
            string.IsNullOrWhiteSpace(specialization) ? name : $"{name} ({specialization})";

        public static bool UsesImplicitBasicSpecialization(string skillName) =>
            string.Equals(skillName, "Broń Biała", StringComparison.OrdinalIgnoreCase);

        public static string FormatSkillDisplay(string skillName, bool hasSpecialization, string? specialization)
        {
            if (!hasSpecialization)
                return skillName;

            if (UsesImplicitBasicSpecialization(skillName))
                return string.IsNullOrWhiteSpace(specialization)
                    ? FormatWithSpecialization(skillName, BasicSpecializationLabel)
                    : FormatWithSpecialization(skillName, specialization);

            return string.IsNullOrWhiteSpace(specialization)
                ? $"{skillName} ()"
                : FormatWithSpecialization(skillName, specialization);
        }

        /// <summary>Klucz umiejętności w stanie draftu: Nazwa lub Nazwa::Specjalizacja</summary>
        public static string SkillStateKey(string name, string? specialization) =>
            string.IsNullOrWhiteSpace(specialization) ? name : $"{name}::{specialization}";

        public static (string Name, string? Specialization) ParseSkillStateKey(string key)
        {
            var sep = key.IndexOf("::", StringComparison.Ordinal);
            if (sep < 0) return (key, null);
            return (key[..sep], key[(sep + 2)..]);
        }

        public static string NormalizeName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            var form = name.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in form)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant()
                .Replace(" ", "").Replace("/", "").Replace("-", "");
        }
    }
}
