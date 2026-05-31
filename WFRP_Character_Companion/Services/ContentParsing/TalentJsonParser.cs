using WFRP_Character_Companion.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using WFRP_Character_Companion.Models.Import;

namespace WFRP_Character_Companion.Services.ContentParsing
{
    public class TalentJsonParser
    {
        public List<Talent> Parse(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            var dto = JsonSerializer.Deserialize<List<TalentImportDto>>(
                json,
                options
            );

            var result = new List<Talent>();

            foreach (var t in dto)
            {
                if (string.IsNullOrWhiteSpace(t.Name))
                    throw new Exception("Talent JSON error: missing Name");

                result.Add(new Talent
                {
                                       
                    Name = t.Name,
                    Description = t.Description,

                    MaxLevelType = t.MaxLevelType,
                    FixedMaxLevel = t.FixedMaxLevel,

                    MaxLevelAttributes = t.MaxLevelAttributes?.ToList() ?? [],

                    TestEffects = t.Tests?.Select(x => new TalentTestEffect
                    {
                        SkillName = x.Skill,
                        Condition = x.Condition,
                        BonusPerLevelAbove1 = x.BonusPerLevelAbove1
                    }).ToList() ?? []
                });
            }

            return result;
        }
    }
}
