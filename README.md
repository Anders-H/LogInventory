# LogInventory

LogInventory is a command-line tool for auditing what a C# codebase can write to its logs. It scans `.cs` files recursively for logging calls on `ILogger` and `ILogger<T>` and writes one row per detected call to a semicolon-delimited CSV file.

## Build and run

LogInventory is written in C# and targets .NET 10. Install the .NET 10 SDK and compile the source code before running the tool. From the repository root, run these commands:

```sh
dotnet build LogInventory/LogInventory/LogInventory.csproj
dotnet run --project LogInventory/LogInventory/LogInventory.csproj --no-build
```

1. At `Root path:`, enter the directory containing the C# source code you want to scan.
2. At `Save path:`, enter the destination CSV file path, such as `log-inventory.csv`.
3. If the destination file already exists, enter `y` to overwrite it or `n` to cancel.

## CSV columns

| Column | Contents |
| --- | --- |
| `Path` | Absolute path to the directory containing the source file. |
| `FileName` | Name of the source file, including the `.cs` extension. |
| `Level` | Log level, such as `Info`, `Debug`, or `Error`. If the level is determined at runtime, the report shows the source expression where available. |
| `Last modified` | Date the source file was last modified, in `YYYY-MM-DD` format. |
| `Body` | Message text or message template, including placeholders such as `{Name}`. If the message is determined at runtime, the report shows the source expression where available. |
