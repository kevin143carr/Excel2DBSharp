# Excel2DBSharp
## Powered by Wing IDE 11 https://wingware.com

Excel2DB is a lightweight command-line tool for macOS, Windows, and Linux that converts Excel or CSV spreadsheets into SQL `INSERT` statements.

It is designed for controlled, auditable imports where you want to **review the SQL before running it**.

---

## What This Tool Actually Does

Excel2DB performs **column mapping**, not just renaming.

You explicitly map:

> Spreadsheet column(s) → Database column

This allows:

* Different names between spreadsheet and database
* Multiple fallback columns
* Default values
* Missing columns
* Sparse or messy input data

---

## Features

✔ Excel (.xlsx, .xls) and CSV support
✔ Explicit column mapping
✔ Fallback source columns
✔ Default values per column
✔ 1-based header row selection
✔ NULL handling
✔ SQL escaping
✔ Dry-run preview mode
✔ No database connection required

---

## Installation

### 1. Python 3.9+

Check version:

```bash
python3 --version
```

### 2. Install dependencies

```bash
pip install typer pyexcel pyexcel-xlsx
```

---

## Publish (Platform-Specific)

Use the platform-specific publish scripts in the repository root:

```bash
./publish-osx-x64.sh
./publish-linux-x64.sh
```

Add `--include-pdb` to keep `.pdb` symbol files in the publish output (default omits them).

On Windows (`cmd.exe`):

```bat
publish-win-x64.bat
```

```bat
publish-win-x64.bat --include-pdb
```

Convenience wrappers:

```bash
./publish-all.sh
```

```bat
publish-all.bat
```

Both `publish-all` scripts publish all supported targets: `win-x64`, `osx-x64`, and `linux-x64`.

Published artifacts are written to `dist/<rid>/`.

---

## Basic Usage

```bash
python excel2db.py import-file input.xlsx \
  --sql-file output.sql \
  --table my_schema.my_table \
  --mapping mapping.json
```

### Optional Flags

| Option          | Description                 |
| --------------- | --------------------------- |
| `--sheet`       | Excel sheet name            |
| `--sheet-index` | Excel sheet index (0-based) |
| `--dry-run`     | Preview first 5 INSERTs     |

---

## Mapping File Format

The mapping file controls how spreadsheet columns map into database columns.

---

## Example `mapping.json`

```json
{
  "header_row": 2,
  "columns": {
    "email": {
      "sources": ["Email Address", "Alt Email"],
      "default": "unknown@example.com"
    },
    "phone": {
      "sources": ["Mobile", "Home Phone", "Work Phone"],
      "default": null
    },
    "status": {
      "sources": [],
      "default": "active"
    }
  }
}
```

---

## Header Row

### `header_row`

* **1-based index** (human-friendly)
* Defaults to `1`

| Value | Meaning    |
| ----- | ---------- |
| 1     | First row  |
| 2     | Second row |
| 3     | Third row  |

Example:

If row 1 is a title and row 2 contains column headers:

```json
"header_row": 2
```

---

## Column Mapping Explained

Each entry under `columns` defines how data flows from the spreadsheet into the database.

### Structure

```json
"target_column": {
  "sources": ["Spreadsheet Col 1", "Spreadsheet Col 2"],
  "default": "some value"
}
```

---

## Terminology

| Term          | Meaning                            |
| ------------- | ---------------------------------- |
| Target column | Column in the database table       |
| Source column | Column name in the spreadsheet     |
| Mapping       | Rule that connects source → target |
| Fallback      | Try multiple source columns        |
| Default       | Used if all sources are empty      |

---

## Fallback Behavior

Sources are tried in order:

```json
"email": {
  "sources": ["Primary Email", "Backup Email"]
}
```

The first non-empty value is used.

---

## Default Values

If all source columns are empty or missing:

```json
"status": {
  "sources": [],
  "default": "active"
}
```

If no default is specified, the value becomes `NULL`.

---

## NULL Handling

If:

* No source columns match
* All values are empty
* No default is provided

Then the value is inserted as:

```sql
NULL
```

---

## Example

### Spreadsheet

| Email Address             | Mobile   |
| ------------------------- | -------- |
| [a@b.com](mailto:a@b.com) | 555-1234 |

### Mapping

```json
{
  "columns": {
    "email": {"sources": ["Email Address"]},
    "phone": {"sources": ["Mobile"]}
  }
}
```

### SQL Output

```sql
INSERT INTO my_table (email, phone)
VALUES ('a@b.com', '555-1234');
```

---

## Why SQL Instead of Direct Insert?

This tool intentionally outputs SQL instead of inserting directly so you can:

✔ Review the data
✔ Modify it
✔ Wrap in transactions
✔ Keep it under version control
✔ Run it safely

---

## Roadmap

* [ ] Warnings for NULL fallbacks
* [ ] Type casting
* [ ] Date formatting
* [ ] PostgreSQL COPY mode
* [ ] Direct database insert mode

---

## License

MIT License
