using System.Text.Json;

namespace WFRP_Character_Companion.Services.Content
{
    public class JsonContentParser<TDto, TEntity>(Func<TDto, TEntity> map) : IContentParser<TDto, TEntity>
    {
        private readonly Func<TDto, TEntity> _map = map;

        public List<TEntity> Parse(string json)
        {
            var dto = JsonSerializer.Deserialize<List<TDto>>(json)
                      ?? [];

            return dto.Select(_map).ToList();
        }
    }
}
