using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace WFRP_Character_Companion.Models
{
    public class Origin
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Race { get; set; } = string.Empty;

        public string PackageJson { get; set; } = "{}";

        [NotMapped]
        public OriginPackage Package
        {
            get => string.IsNullOrWhiteSpace(PackageJson) ? new OriginPackage() : JsonSerializer.Deserialize<OriginPackage>(PackageJson) ?? new OriginPackage();
            set => PackageJson = JsonSerializer.Serialize(value);
        }
    }
}
