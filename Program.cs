/*
Excel2DBSharp - Spreadsheet to SQL Import Tool
Author: Kevin Carr
License: MIT
Copyright (c) 2026 Kevin Carr

Description:
Converts Excel and CSV files into SQL INSERT statements using
optional JSON-based column mappings, defaults, and header offsets.

Repository: https://github.com/kevin143carr/Excel2DBSharp

MIT License
*/

using System;
using System.Collections.Generic;
using System.Formats.Asn1;
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

    static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "1"
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
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
            string arg = args[i];

            if (!arg.StartsWith("--"))
            {
                if (file == null)
                    file = arg;
                else
                    throw new Exception($"Unexpected argument: {arg}");
                continue;
            }

            if (arg.StartsWith("--dry-run"))
            {
                if (arg.Contains('='))
                {
                    var parts = arg.Split('=', 2);
                    dryRun = ParseBool(parts[1]);
                }
                else
                {
                    dryRun = true;
                }
                continue;
            }

            // Require = sign
            int eqIndex = arg.IndexOf('=');
            if (eqIndex < 0)
                throw new Exception($"Argument '{arg}' must use '=' (e.g., --table=MyTable)");

            string key = arg[..eqIndex];
            string value = arg[(eqIndex + 1)..];

            switch (key)
            {
                case "--sql-file":
                    sqlFile = value;
                    break;
                case "--table":
                    table = value;
                    break;
                case "--mapping":
                    mapping = value;
                    break;
                case "--sheet":
                    sheet = value;
                    break;
                case "--sheet-index":
                    sheetIndex = int.Parse(value);
                    break;
                default:
                    throw new Exception($"Unknown argument: {key}");
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

        int headerRow1Based = mappingData.HeaderRow ?? 2;
        int headerRow = headerRow1Based - 1;

        var allRows = LoadSheet(file, sheet, sheetIndex);

        if (headerRow < 0 || headerRow >= allRows.Count)
            throw new Exception($"Header row {headerRow1Based} is out of range. File has {allRows.Count} rows.");

        var headers = allRows[headerRow];
        var dataRows = allRows.Skip(headerRow + 1).ToList();

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
        {
            var headerName = headers[i]?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(headerName))
                headerIndex[headerName] = i;
        }

        Console.WriteLine("Detected headers:");
        foreach (var h in headers) Console.WriteLine($"- '{h}'");

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

        foreach (var row in dataRows)
        {
            var values = new List<string>();

            foreach (var col in finalColumns)
            {
                object? chosenValue = null;

                foreach (var sourceCol in columnSources[col])
                {
                    var normalizedSource = sourceCol.Trim();
                    if (!headerIndex.TryGetValue(normalizedSource, out int idx))
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

            insertStatements.Add(
                $"INSERT INTO {table} ({string.Join(", ", finalColumns)}) VALUES ({string.Join(", ", values)});"
            );
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
        if (val == null) return "NULL";

        var s = val.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return "NULL";

        return $"'{s.Replace("'", "''")}'";
    }

    static Mapping LoadMapping(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return new Mapping();

        var json = File.ReadAllText(path);

        // Use source-generated context
        var mapping = JsonSerializer.Deserialize(json, MappingJsonContext.Default.Mapping);
        return mapping ?? new Mapping();
    }

    static List<List<object?>> LoadSheet(string file, string? sheet, int? sheetIndex)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        var rows = new List<List<object?>>();

        if (ext == ".csv")
        {
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            while (csv.Read())
            {
                var row = new List<object?>();
                for (int i = 0; csv.TryGetField(i, out string? field); i++)
                    row.Add(field);
                rows.Add(row);
            }
        }
        else
        {
            using var wb = new XLWorkbook(file);
            var ws = sheet != null ? wb.Worksheet(sheet) : wb.Worksheet(sheetIndex ?? 1);

            int colCount = ws.RowsUsed().Max(r => r.LastCellUsed()?.Address.ColumnNumber ?? 0);

            foreach (var wsRow in ws.RowsUsed())
            {
                var row = new List<object?>();
                for (int i = 1; i <= colCount; i++)
                    row.Add(wsRow.Cell(i).Value);
                rows.Add(row);
            }
        }

        return rows;
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Excel2DBSharp <file> --sql-file=<out.sql> --table=<table> [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --mapping=<file.json>");
        Console.WriteLine("  --sheet=<name>");
        Console.WriteLine("  --sheet-index=<n>");
        Console.WriteLine("  --dry-run");
    }
}
