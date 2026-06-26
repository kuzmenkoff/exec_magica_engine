using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Loads card data from JSON using Newtonsoft.Json.
/// </summary>
public static class CardJsonLoader
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Converters = { new StringEnumConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Deserializes all cards from a JSON string.
    /// </summary>
    public static AllCards LoadAllCards(string json)
    {
        return JsonConvert.DeserializeObject<AllCards>(json, Settings);
    }

    /// <summary>
    /// Serializes all cards to a JSON string.
    /// </summary>
    public static string SaveAllCards(AllCards cards, bool prettyPrint = true)
    {
        return JsonConvert.SerializeObject(
            cards,
            prettyPrint ? Formatting.Indented : Formatting.None,
            Settings
        );
    }
}
