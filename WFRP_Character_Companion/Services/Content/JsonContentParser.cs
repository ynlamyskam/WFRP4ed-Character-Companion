using System.Text.Json;
using System.Text.Json.Serialization;

namespace WFRP_Character_Companion.Services.Content
{
    public class JsonContentParser<TDto, TEntity>(Func<TDto, TEntity> map) : IContentParser<TDto, TEntity>
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly Func<TDto, TEntity> _map = map;

        public List<TEntity> Parse(string json)
        {
            var dto = JsonSerializer.Deserialize<List<TDto>>(json, JsonOptions)
                      ?? [];

            return dto.Select(_map).ToList();
        }
    }
}
