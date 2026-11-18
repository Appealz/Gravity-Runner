using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Unity assemblies are not included in this portfolio-only repository.
// Roslyn syntax parsing works without Unity, but is NOT a compilation or test.
var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
var scriptDirectory = Path.Combine(root, "Script");
var readmePath = Path.Combine(root, "README.md");
var failures = 0;

if (!Directory.Exists(scriptDirectory))
{
    Console.Error.WriteLine($"Missing script directory: {scriptDirectory}");
    return 1;
}

var scripts = Directory.GetFiles(scriptDirectory, "*.cs", SearchOption.AllDirectories)
    .OrderBy(path => path, StringComparer.Ordinal)
    .ToArray();

if (scripts.Length == 0)
{
    Console.Error.WriteLine("No C# scripts found. Validation cannot pass on an empty repository.");
    return 1;
}

foreach (var file in scripts)
{
    var relativePath = Path.GetRelativePath(root, file);
    var tree = CSharpSyntaxTree.ParseText(
        File.ReadAllText(file),
        new CSharpParseOptions(LanguageVersion.Latest),
        path: relativePath);

    foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
    {
        Console.Error.WriteLine(diagnostic.ToString());
        failures++;
    }
}

Console.WriteLine($"Checked C# syntax: {scripts.Length} scripts.");

if (!File.Exists(readmePath))
{
    Console.Error.WriteLine("Missing README.md.");
    failures++;
}
else
{
    var checkedLinks = 0;
    var markdown = File.ReadAllText(readmePath);
    foreach (Match match in Regex.Matches(markdown, @"!?\[[^\]]*\]\((?<link>[^)]+)\)"))
    {
        var link = match.Groups["link"].Value.Trim();
        if (link.Length == 0 ||
            link.StartsWith("#", StringComparison.Ordinal) ||
            link.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            link.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var pathPart = Uri.UnescapeDataString(link.Split('#')[0].Split('?')[0]);
        var candidate = Path.GetFullPath(
            Path.Combine(root, pathPart.Replace('/', Path.DirectorySeparatorChar)));
        checkedLinks++;

        // Reject links outside the repository as well as broken local links.
        if ((!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
             !string.Equals(candidate, root, StringComparison.Ordinal)) ||
            (!File.Exists(candidate) && !Directory.Exists(candidate)))
        {
            Console.Error.WriteLine($"Invalid local README link: {link}");
            failures++;
        }
    }

    Console.WriteLine($"Checked local README links: {checkedLinks}.");
}

if (failures > 0)
{
    Console.Error.WriteLine($"Validation failed: {failures} issue(s).");
    return 1;
}

Console.WriteLine("Portfolio source validation passed (syntax and links only).");
return 0;
