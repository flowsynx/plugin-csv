## FlowSynx CSV Plugin

The CSV Plugin is a pre-packaged, plug-and-play integration component for the FlowSynx engine. It enables reading from and writing to CSV files with configurable parameters such as file path, delimiter, headers, and encoding. Designed for FlowSynx’s no-code/low-code automation workflows, this plugin simplifies data extraction and transformation tasks.

This plugin is automatically installed by the FlowSynx engine when selected within the platform. It is not intended for manual installation or standalone developer use outside the FlowSynx environment.

## Purpose

The CSV Plugin allows FlowSynx users to:

- Parse and inspect CSV structures.
- Map CSV data to specific output fields.
- Filter CSV data based on conditions.
- Transform CSV data inline for downstream workflows.

## Supported Operations

- **read**: Converts a structured object (e.g., rows from a database, in-memory objects) into CSV output using the provided parameters (e.g., delimiter, headers).  
- **filter**: Filters rows in the CSV using defined `Filter` conditions. Supports logical operations (`and`, `or`) and common operators like `equals`, `contains`, `startsWith`, `endsWith`, `greaterThan`, and `lessThan`.  
- **map**: Maps existing fields in the CSV to a new subset of keys or column arrangement for simplified output.

Minimum FlowSynx version: 1.3.0.

## Input Parameters

The plugin accepts the following parameters:

- `Operation` (string): Required. The type of operation to perform. Supported values are `read`, `filter`, and `map`.  
- `Data` (string/object): Required. The input to process. For `filter` and `map`, provide a raw CSV string. For `read`, provide a structured object or rows to convert to CSV.  
- `Delimiter` (string): Optional. Defaults to `,`. The character used to separate fields in the CSV.  
- `Mappings` (list): Required for `map` operation. Defines which fields to include in the output.  
- `IgnoreBlankLines` (bool): Optional. Specifies whether blank lines in the CSV should be ignored (`true`) or treated as data rows (`false`). Defaults to `true`.  
- `HasHeader` (bool): Optional. Indicates if the first row of the CSV contains headers (`true`) or data (`false`). Defaults to `true`.  
- `Filters` (object): Optional. Used with the `filter` operation to define filtering criteria.  

### Example input

```json
{
  "Data": { ... },
  "Mappings": ["LastName", "Email"],
  "IgnoreBlankLines": true,
  "HasHeader": true,
  "Delimiter": ","
}
```

## Operation Examples

### read Operation

**Input Data (object):**

```json
{ 
    "Data": [ /* csv string or structured rows */ ], 
    "IgnoreBlankLines": true,
    "HasHeader": true, 
    "Delimiter": ","
}
```

### map Operation

**Input Data:**
```csv
CustomerID,FirstName,LastName,Email,Phone,Country
1,John,Doe,john.doe@example.com,1234,USA
2,Jane,Smith,jane.smith@example.com,201234,UK
3,Raj,Patel,raj.patel@example.com,98765,India
4,Anna,Schmidt,anna.schmidt@example.com,30234,Germany
5,Maria,Gonzalez,maria.gonzalez@example.com,911234,Spain
```

**Input Parameters:**
```json
{
  "Data": { ... },
  "Mappings": ["LastName", "Email"],
  "IgnoreBlankLines": true,
  "HasHeader": true,
  "Delimiter": ","
}
```

**Output:**
```json
LastName,Email
Doe,john.doe@example.com
Smith,jane.smith@example.com
Patel,raj.patel@example.com
Schmidt,anna.schmidt@example.com
Gonzalez,maria.gonzalez@example.com
```

### filter Operation

**Input Data:**
```csv
CustomerID,FirstName,LastName,Email,Phone,Country
1,John,Doe,john.doe@example.com,1234,USA
2,Jane,Smith,jane.smith@example.com,201234,UK
3,Raj,Patel,raj.patel@example.com,98765,India
4,Anna,Schmidt,anna.schmidt@example.com,30234,Germany
5,Maria,Gonzalez,maria.gonzalez@example.com,911234,Spain
```

**Input Parameters:**
```json
{
  "Data": { ... },
  "Filters": {
    "Logic": "and",
    "Filters": [
        {
            "Column": "Country",
            "Operator": "equals",
            "Value": "USA"
        },
        {
            "Column": "FirstName",
            "Operator": "startsWith",
            "Value": "J"
        }
    ]
  },
  "IgnoreBlankLines": true,
  "HasHeader": true,
  "Delimiter": ","
}
```

**Output:**
```csv
CustomerID,FirstName,LastName,Email,Phone,Country
1,John,Doe,john.doe@example.com,1234,USA

```

## Debugging Tips

- Ensure that the `Delimiter` matches the file’s actual separator (`,` for standard CSV, `;` or `\t` for others).  
- Validate that `Mappings` and `Filters` reference columns that exist in the CSV header row.  
- If unexpected rows are excluded or included during filtering, check the logical operators (`and` / `or`) and ensure that data types align (e.g., string comparisons for string fields).  
- To troubleshoot encoding issues, verify that the CSV input uses UTF-8 or specify encoding explicitly if supported by FlowSynx.  

## Security Notes

- No data is persisted unless explicitly configured.
- All operations run in a secure sandbox within FlowSynx.
- Only authorized platform users can view or modify configurations.

## License

© FlowSynx. All rights reserved.