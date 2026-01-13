# Excel2DB Example

This folder contains a working example of Excel2DB.

## Files

- sample_people.csv
- sample_people.xlsx
- mapping.json

## Example Command

```bash
excel2db import-file sample_people.xlsx   --sql-file output.sql   --table people   --mapping mapping.json
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
excel2db import-file sample_people.csv   --sql-file output.sql   --table people   --mapping mapping.json   --dry-run
```

This will show the first 5 INSERT statements without writing the file.