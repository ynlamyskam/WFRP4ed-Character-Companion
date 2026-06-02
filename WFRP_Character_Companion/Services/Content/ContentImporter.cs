using WFRP_Character_Companion.Data;

namespace WFRP_Character_Companion.Services.Content
{
    public class ContentImporter<TDto, TEntity>(ApplicationDbContext db, IContentParser<TDto, TEntity> parser) : IContentImporter<TEntity> where TEntity : class
    {
        private readonly ApplicationDbContext _db = db;
        private readonly IContentParser<TDto, TEntity> _parser = parser;

        public void Import(string filePath)
        {
            if (_db.Set<TEntity>().Any())
                return;

            var json = File.ReadAllText(filePath);

            var entities = _parser.Parse(json);

            _db.Set<TEntity>().AddRange(entities);
            _db.SaveChanges();
        }
    }
}
