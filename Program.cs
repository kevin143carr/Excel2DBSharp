using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 1;
        }

        try
        {
            return ImportFile(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static int ImportFile(string[] args)
    {
        string? file = null;
        string? sqlFile = null;
        string? table = null;
        string? mapping = null;
        string? sheet = null;
        int? sheetIndex = null;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--") && file == null)
            {
                file = args[i];
                continue;
            }

            switch (args[i])
            {
                case "--sql-file":
                    sqlFile = GetNextArg(args, ref i);
                    break;
                case "--table":
                    table = GetNextArg(args, ref i);
                    break;
                case "--mapping":
                    mapping = GetNextArg(args, ref i);
                    break;
                case "--sheet":
                    sheet = GetNextArg(args, ref i);
                    break;
                case "--sheet-index":
                    sheetIndex = int.Parse(GetNextArg(args, ref i));
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(file) ||
            string.IsNullOrWhiteSpace(sqlFile) ||
            string.IsNullOrWhiteSpace(table))
        {
            throw new Exception("Missing required arguments: file, --sql-file, and --table are required.");
        }

        Console.WriteLine($"Loading: {file}");

        var mappingData = LoadMapping(mapping);

        int headerRow1Based = mappingData.HeaderRow ?? 1;
        if (headerRow1Based < 1)
            throw new Exception("header_row must be 1 or greater");

        int headerRow = headerRow1Based - 1;

        var allRows = LoadSheet(file, sheet, sheetIndex);

        if (headerRow >= allRows.Count)
            throw new Exception($"header_row {headerRow1Based} is out of range. File has only {allRows.Count} rows.");

        var headers = allRows[headerRow];
        var rows = allRows.Skip(headerRow + 1).ToList();

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            var headerName = headers[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(headerName))
            {
                headerIndex[headerName] = i;
            }
        }

        if (mappingData.Columns == null || mappingData.Columns.Count == 0)
            throw new Exception("Mapping file must contain a 'columns' section.");

        var finalColumns = new List<string>();
        var columnSources = new Dictionary<string, List<string>>();
        var columnDefaults = new Dictionary<string, object?>();

        foreach (var kv in mappingData.Columns)
        {
            if (kv.Value?.Sources == null)
                throw new Exception($"Column '{kv.Key}' must contain a 'sources' list.");

            finalColumns.Add(kv.Key);
            columnSources[kv.Key] = kv.Value.Sources;
            columnDefaults[kv.Key] = kv.Value.Default;
        }

        var insertStatements = new List<string>();

        foreach (var row in rows)
        {
            var values = new List<string>();

            foreach (var col in finalColumns)
            {
                object? chosenValue = null;

                foreach (var sourceCol in columnSources[col])
                {
                    if (!headerIndex.TryGetValue(sourceCol, out int idx))
                        continue;

                    object? cellValue = idx < row.Count ? row[idx] : null;

                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        chosenValue = cellValue;
                        break;
                    }
                }

                if (chosenValue == null)
                    columnDefaults.TryGetValue(col, out chosenValue);

                values.Add(InferSqlValue(chosenValue));
            }

            var stmt =
                $"INSERT INTO {table} ({string.Join(", ", finalColumns)}) VALUES ({string.Join(", ", values)});";

            insertStatements.Add(stmt);
        }

        if (dryRun)
        {
            Console.WriteLine("Dry run enabled. Showing first 5 statements:\n");
            foreach (var stmt in insertStatements.Take(5))
                Console.WriteLine(stmt);

            return 0;
        }

        Console.WriteLine($"Writing {insertStatements.Count} INSERT statements to {sqlFile}");
        File.WriteAllLines(sqlFile, insertStatements);
        Console.WriteLine("SQL file generation complete.");

        return 0;
    }

    static string InferSqlValue(object? val)
    {
        if (val == null)
            return "NULL";

        var s = val.ToString()?.Trim();
        if (string.IsNullOrEmpty(s))
            return "NULL";

        return "'" + s.Replace("'", "''") + "'";
    }

    static Mapping LoadMapping(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new Mapping();

        var json = File.ReadAllText(path);
        var mapping = JsonSerializer.Deserialize<Mapping>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return mapping ?? new Mapping();
    }

    static List<List<object?>> LoadSheet(string file, string? sheet, int? sheetIndex)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();

        if (ext == ".csv")
        {
            var rows = new List<List<object?>>();

            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            while (csv.Read())
            {
                var row = new List<object?>();
                for (int i = 0; csv.TryGetField(i, out string? field); i++)
                    row.Add(field);

                rows.Add(row);
            }

            return rows;
        }
        else
        {
            using var wb = new XLWorkbook(file);
            var ws = sheet != null
                ? wb.Worksheet(sheet)
                : wb.Worksheet(sheetIndex ?? 1);

            var rows = new List<List<object?>>();

            foreach (var r in ws.RowsUsed())
            {
                var row = new List<object?>();
                foreach (var c in r.CellsUsed())
                    row.Add(c.Value);

                rows.Add(row);
            }

            return rows;
        }
    }

    static string GetNextArg(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            throw new Exception($"Missing value for {args[i]}");

        return args[++i];
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Excel2DBSharp <file> --sql-file <out.sql> --table <table> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --mapping <file.json>");
        Console.WriteLine("  --sheet <name>");
        Console.WriteLine("  --sheet-index <n>");
        Console.WriteLine("  --dry-run");
    }
}

class Mapping
{
    public int? HeaderRow { get; set; }
    public Dictionary<string, ColumnConfig> Columns { get; set; } = new();
}

class ColumnConfig
{
    public List<string> Sources { get; set; } = new();
    public object? Default { get; set; }
}
