# Excel2DB Example

This folder contains a working example of Excel2DB.

## Files

- sample_people.csv
- sample_people.xlsx
- example_mapping.json

## Example Command

macOS/Linux (published binary examples):

```bash
../dist/osx-x64/Excel2DBSharp sample_people.xlsx --sql-file=output.sql --table=people --mapping=example_mapping.json
```

```bash
../dist/linux-x64/Excel2DBSharp sample_people.xlsx --sql-file=output.sql --table=people --mapping=example_mapping.json
```

Windows (published binary):

```bat
..\dist\win-x64\Excel2DBSharp.exe sample_people.xlsx --sql-file=output.sql --table=people --mapping=example_mapping.json
```

## Features Demonstrated

### Column Fallback
Email will fall back to Alt Email if missing.

### Defaults
If Age is missing, it defaults to 0.

### NULL Handling
Empty cells become NULL unless a default is specified.

### Dry Run

```bash
../dist/osx-x64/Excel2DBSharp sample_people.csv --sql-file=output.sql --table=people --mapping=example_mapping.json --dry-run
```

This will show the first 5 INSERT statements without writing the file.
