using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Mapping
{
    [JsonPropertyName("header_row")]
    public int? HeaderRow { get; set; }

    // Backward-compatible alias for camelCase mapping files.
    [JsonPropertyName("headerRow")]
    public int? HeaderRowCamelCase
    {
        get => HeaderRow;
        set => HeaderRow = value;
    }

    public Dictionary<string, ColumnConfig> Columns { get; set; } = new();
}

public class ColumnConfig
{
    public List<string> Sources { get; set; } = new();
    public object? Default { get; set; }
}
