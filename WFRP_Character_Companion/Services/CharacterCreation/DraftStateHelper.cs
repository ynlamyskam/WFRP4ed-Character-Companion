using System.Text.Json;

namespace WFRP_Character_Companion.Services.CharacterCreation
{
    public static class DraftStateHelper
    {
        public static Dictionary<string, JsonElement> Parse(string stateJson)
        {
            if (string.IsNullOrWhiteSpace(stateJson))
                return new Dictionary<string, JsonElement>();

            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stateJson)
                   ?? new Dictionary<string, JsonElement>();
        }

        public static int GetInt(Dictionary<string, JsonElement> state, string key, int defaultValue = 0)
        {
            if (!state.TryGetValue(key, out var el))
                return defaultValue;

            return el.ValueKind switch
            {
                JsonValueKind.Number => el.TryGetInt32(out var i) ? i : defaultValue,
                JsonValueKind.String => int.TryParse(el.GetString(), out var s) ? s : defaultValue,
                JsonValueKind.True => 1,
                JsonValueKind.False => 0,
                _ => defaultValue
            };
        }

        public static string GetString(Dictionary<string, JsonElement> state, string key, string defaultValue = "")
        {
            if (!state.TryGetValue(key, out var el))
                return defaultValue;

            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString() ?? defaultValue,
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => el.ToString()
            };
        }

        public static List<string> GetStringList(Dictionary<string, JsonElement> state, string key)
        {
            if (!state.TryGetValue(key, out var el))
                return [];

            if (el.ValueKind == JsonValueKind.Array)
                return el.EnumerateArray()
                    .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? string.Empty : x.ToString())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            if (el.ValueKind == JsonValueKind.String)
            {
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(el.GetString() ?? "[]") ?? [];
                }
                catch
                {
                    return [];
                }
            }

            return [];
        }

        public static string Serialize(Dictionary<string, JsonElement> state)
        {
            return JsonSerializer.Serialize(state);
        }

        public static Dictionary<string, JsonElement> SetValue(Dictionary<string, JsonElement> state, string key, object value)
        {
            var json = JsonSerializer.SerializeToElement(value);
            state[key] = json;
            return state;
        }

        public record TalentEntry(string Name, string? Specialization);

        public static List<TalentEntry> GetTalentEntries(Dictionary<string, JsonElement> state, string key)
        {
            if (!state.TryGetValue(key, out var el))
                return [];

            if (el.ValueKind == JsonValueKind.Array)
            {
                var list = new List<TalentEntry>();
                foreach (var item in el.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrEmpty(s))
                            list.Add(new TalentEntry(s, null));
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() :
                                   item.TryGetProperty("Name", out var n2) ? n2.GetString() : null;
                        if (string.IsNullOrEmpty(name)) continue;
                        string? spec = null;
                        if (item.TryGetProperty("specialization", out var sp)) spec = sp.GetString();
                        else if (item.TryGetProperty("Specialization", out var sp2)) spec = sp2.GetString();
                        list.Add(new TalentEntry(name, spec));
                    }
                }
                return list;
            }

            return GetStringList(state, key).Select(s => new TalentEntry(s, null)).ToList();
        }
    }
}
