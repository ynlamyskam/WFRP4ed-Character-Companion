using System.Text.Json;
using System.Text.Json.Serialization;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Services.CharacterCreation
{
    public class CreationContentService(IWebHostEnvironment env)
    {
        private readonly IWebHostEnvironment _env = env;

        public async Task<List<Origin>> LoadOriginsAsync()
        {
            var contentDir1 = Path.Combine(_env.ContentRootPath, "Content");
            var contentDir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content");
            var originFiles = new List<string>();
            if (Directory.Exists(contentDir1))
                originFiles.AddRange(Directory.GetFiles(contentDir1, "origins*.json", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith("origins.combined.json", StringComparison.OrdinalIgnoreCase)));
            if (Directory.Exists(contentDir2))
                originFiles.AddRange(Directory.GetFiles(contentDir2, "origins*.json", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith("origins.combined.json", StringComparison.OrdinalIgnoreCase)));

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());

            var combined = new List<Origin>();
            foreach (var f in originFiles.Distinct().OrderBy(f => f))
            {
                var json = await File.ReadAllTextAsync(f);
                var dtos = JsonSerializer.Deserialize<List<OriginImportDto>>(json, options);
                if (dtos == null) continue;

                foreach (var dto in dtos)
                {
                    var package = new OriginPackage
                    {
                        Skills = dto.Package?.Skills?.Select(s =>
                        {
                            var type = s.Type ?? (s.Skill != null ? GrantType.Fixed : GrantType.Choice);
                            return new SkillGrant
                            {
                                Type = type,
                                Choose = s.Choose ?? 0,
                                Skill = s.Skill == null ? null : new SkillRef { Name = s.Skill.Name, Specialization = s.Skill.Specialization },
                                Options = s.Options?.Select(o => new SkillRef { Name = o.Name, Specialization = o.Specialization }).ToList() ?? []
                            };
                        }).ToList() ?? [],
                        Talents = dto.Package?.Talents?.Select(t =>
                        {
                            var type = t.Type ?? (t.Talent != null ? TalentGrantType.Fixed : TalentGrantType.Choice);
                            return new TalentGrant
                            {
                                Type = type,
                                Choose = t.Choose ?? 0,
                                Count = t.Count ?? 0,
                                Talent = t.Talent == null ? null : new TalentRef { Name = t.Talent.Name, Specialization = t.Talent.Specialization },
                                Options = t.Options?.Select(o => new TalentRef { Name = o.Name, Specialization = o.Specialization }).ToList() ?? []
                            };
                        }).ToList() ?? []
                    };

                    combined.Add(new Origin
                    {
                        Race = dto.Race ?? string.Empty,
                        Name = dto.Name ?? string.Empty,
                        PackageJson = JsonSerializer.Serialize(package, options)
                    });
                }
            }

            return combined
                .GroupBy(o => (Race: Normalize(o.Race), Name: Normalize(o.Name)))
                .Select(g => g.First())
                .ToList();
        }

        public async Task<List<Profession>> LoadProfessionsAsync()
        {
            var dir1 = Path.Combine(_env.ContentRootPath, "Content", "Professions");
            var dir2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "Professions");
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(dir1))
                foreach (var f in Directory.GetFiles(dir1, "*.json")) files.Add(f);
            if (Directory.Exists(dir2))
                foreach (var f in Directory.GetFiles(dir2, "*.json")) files.Add(f);

            var list = new List<Profession>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var f in files.OrderBy(f => f))
            {
                var txt = await File.ReadAllTextAsync(f);
                var pList = JsonSerializer.Deserialize<List<Profession>>(txt, options);
                if (pList != null)
                    list.AddRange(pList);
            }

            return list
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .GroupBy(p => (Class: Normalize(p.Class ?? string.Empty), Name: Normalize(p.Name)))
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>Unikalny klucz profesji — ta sama nazwa może występować w różnych klasach.</summary>
        public static string ToKey(Profession p)
        {
            return $"{p.Class ?? string.Empty}|{p.Name ?? string.Empty}";
        }

        public static (string? Class, string Name) ParseKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return (null, string.Empty);

            var sep = key.IndexOf('|');
            if (sep < 0)
                return (null, key);

            return (key[..sep], key[(sep + 1)..]);
        }

        public async Task<List<Race>> LoadRacesAsync()
        {
            var path1 = Path.Combine(_env.ContentRootPath, "Content", "races.json");
            var path2 = Path.Combine(_env.ContentRootPath, "Data", "Seed", "Content", "races.json");
            string? txt = null;
            if (File.Exists(path1))
                txt = await File.ReadAllTextAsync(path1);
            else if (File.Exists(path2))
                txt = await File.ReadAllTextAsync(path2);

            if (string.IsNullOrEmpty(txt))
                return [];

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Race>>(txt, options) ?? [];
        }

        public static string Normalize(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var form = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in form)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant().Replace(" ", "");
        }

        public Profession? FindProfession(IEnumerable<Profession> professions, string? keyOrName)
        {
            if (string.IsNullOrEmpty(keyOrName))
                return null;

            if (keyOrName.Contains('|'))
            {
                var (cls, name) = ParseKey(keyOrName);
                var classNorm = Normalize(cls);
                var nameNorm = Normalize(name);
                return professions.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.Name) &&
                    Normalize(p.Name) == nameNorm &&
                    Normalize(p.Class ?? string.Empty) == classNorm);
            }

            var norm = Normalize(keyOrName);
            return professions.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.Name) &&
                (string.Equals(p.Name, keyOrName, StringComparison.OrdinalIgnoreCase) || Normalize(p.Name) == norm));
        }
    }
}
