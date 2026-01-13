using System.Collections.Generic;

public class Mapping
{
    public int? HeaderRow { get; set; }
    public Dictionary<string, ColumnConfig> Columns { get; set; } = new();
}

public class ColumnConfig
{
    public List<string> Sources { get; set; } = new();
    public object? Default { get; set; }
}

