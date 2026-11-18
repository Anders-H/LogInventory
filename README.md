# LogInventory

LogInventory is a command-line tool for auditing what a C# codebase can write to its logs. It scans `.cs` files recursively for logging calls on `ILogger` and `ILogger<T>` and writes one row per detected call to a semicolon-delimited CSV file. The report includes the source directory, file name, log level, file modification date, and message or message template.

## Build and run

LogInventory is written in C# and targets .NET 10. Install the .NET 10 SDK and compile the source code before running the tool. From the repository root, run these commands:

```sh
dotnet build LogInventory/LogInventory/LogInventory.csproj
dotnet run --project LogInventory/LogInventory/LogInventory.csproj --no-build
```

1. At `Root path:`, enter the directory containing the C# source code you want to scan.
2. At `Save path:`, enter the destination CSV file path, such as `log-inventory.csv`.
3. If the destination file already exists, enter `y` to overwrite it or `n` to cancel.

The report describes logging calls found in source code. Message templates keep their placeholders, and values computed at runtime are shown as source expressions where available.
