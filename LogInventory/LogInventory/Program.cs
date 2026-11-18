using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.Logging;

Console.Write("Root path: ");
var path = new DirectoryInfo(Console.ReadLine() ?? "");
Console.Write("Save path: ");
var savePath = Console.ReadLine() ?? "";

if (File.Exists(savePath))
{
    Console.Write("Overwrite? (y/n) ");
    var overwrite = Console.ReadLine() ?? "";

    if (overwrite.ToLower() != "y")
        return;

    File.Delete(savePath);
}

var sourceFiles = new List<FileInfo>();
GetFiles(path, sourceFiles);
var count = 0;
var total = sourceFiles.Count;
var padding = total.ToString().Length;
var logInventoryList = new List<LogInventory>();
var compilation = CSharpCompilation.Create("LogInventory.Source",
    syntaxTrees: [CSharpSyntaxTree.ParseText("global using Microsoft.Extensions.Logging;")],
    references:
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
        MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location)
    ]);

foreach (var sourceFile in sourceFiles)
{
    count++;
    Console.WriteLine($"{count.ToString().PadLeft(padding)}/{total} : {sourceFile.FullName}");
    FindLogUsage(sourceFile, logInventoryList);
}

using var writer = new StreamWriter(savePath, false, Encoding.UTF8);
writer.WriteLine("Path; FileName; Level; Last modified; Body");

foreach (var logInventory in logInventoryList)
    writer.WriteLine(logInventory.ToString());

return;

static void GetFiles(DirectoryInfo directory, List<FileInfo> sourceFiles)
{
    foreach (var file in directory.GetFiles())
    {
        if (file.Name.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase))
            sourceFiles.Add(file);
    }

    foreach (var subDirectory in directory.GetDirectories())
    {
        GetFiles(subDirectory, sourceFiles);
    }
}

void FindLogUsage(FileInfo file, List<LogInventory> list)
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName), path: file.FullName);
    var model = compilation.AddSyntaxTrees(tree).GetSemanticModel(tree);

    foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
        var (receiver, method) = invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => (access.Expression, access.Name.Identifier.ValueText),
            MemberBindingExpressionSyntax binding => (invocation.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault()?.Expression, binding.Name.Identifier.ValueText),
            _ => (null, "")
        };

        if (receiver is null || !method.StartsWith("Log", StringComparison.Ordinal))
            continue;

        var type = model.GetTypeInfo(receiver).Type as INamedTypeSymbol;

        if (type?.OriginalDefinition.ToDisplayString() is not ("Microsoft.Extensions.Logging.ILogger" or "Microsoft.Extensions.Logging.ILogger<TCategoryName>"))
            continue;

        var arguments = invocation.ArgumentList.Arguments;
        var levelArgument = method == "Log" ? arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "logLevel") ?? arguments.FirstOrDefault(argument => argument.NameColon is null) : null;
        var level = method == "Log" ? GetLogLevel(levelArgument?.Expression, model) : GetLevelName(method[3..]);
        var message = arguments.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText is "message" or "state" || (model.GetOperation(argument) as IArgumentOperation)?.Parameter?.Name is "message" or "state");
        message ??= arguments.FirstOrDefault(argument => argument != levelArgument && argument.NameColon is null && model.GetTypeInfo(argument.Expression).Type?.ToDisplayString() is not ("Microsoft.Extensions.Logging.EventId" or "System.Exception") && (model.GetOperation(argument) as IArgumentOperation)?.Parameter?.Name is not ("eventId" or "exception" or "formatter"));
        var body = message is null ? "" : GetMessage(message.Expression, model);
        list.Add(new LogInventory(file.Directory?.FullName ?? "", file.Name, level, file.LastWriteTime, body));
    }
}

static string GetLogLevel(ExpressionSyntax? expression, SemanticModel model)
{
    if (expression is null)
        return "";

    var constant = model.GetConstantValue(expression);

    return constant.HasValue && constant.Value is int value && Enum.IsDefined((LogLevel)value)
        ? GetLevelName(((LogLevel)value).ToString())
        : expression.ToString();
}

static string GetLevelName(string name) =>
    name == "Information" ? "Info" : name;

static string GetMessage(ExpressionSyntax expression, SemanticModel model)
{
    var constant = model.GetConstantValue(expression);
    return constant.HasValue && constant.Value is string message ? message : expression.ToString();
}

public class LogInventory
{
    public string Path { get; }
    public string FileName { get; }
    public string Level { get; }
    public DateTime LastModified { get; }
    public string Body { get; }

    public LogInventory(string path, string fileName, string level, DateTime lastModified, string body)
    {
        Path = path;
        FileName = fileName;
        Level = level;
        LastModified = lastModified;
        Body = body;
    }

    public override string ToString() =>
        $"{Path.Replace(";", "")}; {FileName.Replace(";", "")}; {Level.Replace(";", "")}; {LastModified:yyyy-MM-dd};{EscapeBody(Body)}";

    private static string EscapeBody(string body) =>
        body.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? "\"" + body.Replace("\"", "\"\"") + "\"" : body;
}
