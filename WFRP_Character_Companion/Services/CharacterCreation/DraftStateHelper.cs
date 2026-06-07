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
    }
}
