using WFRP_Character_Companion.Services.ContentParsing;

namespace WFRP_Character_Companion.Data.Seed.Importers
{
    public class TalentImporter
    {
        private readonly ApplicationDbContext _db;
        private readonly TalentJsonParser _parser;

        public TalentImporter(ApplicationDbContext db, TalentJsonParser parser)
        {
            _db = db;
            _parser = parser;
        }

        public void Import(string filePath)
        {
            if (_db.Talents.Any()) return;

            var json = File.ReadAllText(filePath);

            var talents = _parser.Parse(json);

            _db.Talents.AddRange(talents);
            _db.SaveChanges();
        }
    }
}
