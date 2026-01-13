using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Mapping))]
internal partial class MappingJsonContext : JsonSerializerContext
{
}
