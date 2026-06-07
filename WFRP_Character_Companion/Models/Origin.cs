using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WFRP_Character_Companion.Models
{
    public class Origin
    {
        private static readonly JsonSerializerOptions PackageJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Race { get; set; } = string.Empty;

        public string PackageJson { get; set; } = "{}";

        [NotMapped]
        public OriginPackage Package
        {
            get => string.IsNullOrWhiteSpace(PackageJson)
                ? new OriginPackage()
                : JsonSerializer.Deserialize<OriginPackage>(PackageJson, PackageJsonOptions) ?? new OriginPackage();
            set => PackageJson = JsonSerializer.Serialize(value, PackageJsonOptions);
        }
    }
}
