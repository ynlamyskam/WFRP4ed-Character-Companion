using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Data;
using WFRP_Character_Companion.Helpers;
using WFRP_Character_Companion.Models;
using WFRP_Character_Companion.Models.Import;

namespace WFRP_Character_Companion.Services.Content
{
    public class TalentSyncService(ApplicationDbContext db, IContentParser<TalentImportDto, Talent> parser)
    {
        public void SyncFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var json = File.ReadAllText(filePath);
            var incoming = parser.Parse(json);
            var existing = db.Talents.Include(t => t.TestEffects).ToList();

            foreach (var talent in incoming)
            {
                var match = existing.FirstOrDefault(t =>
                    string.Equals(t.Name, talent.Name, StringComparison.OrdinalIgnoreCase) ||
                    CharacterRulesHelper.NormalizeName(t.Name) == CharacterRulesHelper.NormalizeName(talent.Name));

                if (match == null)
                {
                    db.Talents.Add(talent);
                    existing.Add(talent);
                }
                else
                {
                    match.Description = talent.Description;
                    match.MaxLevelType = talent.MaxLevelType;
                    match.FixedMaxLevel = talent.FixedMaxLevel;
                    match.MaxLevelAttributes = talent.MaxLevelAttributes;
                }
            }

            db.SaveChanges();
        }

        public Talent? FindTalent(IEnumerable<Talent> talents, string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var norm = CharacterRulesHelper.NormalizeName(name);
            return talents.FirstOrDefault(t =>
                string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) ||
                CharacterRulesHelper.NormalizeName(t.Name) == norm);
        }
    }
}
