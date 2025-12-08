using System.Xml;
using System.Xml.Linq;
using System.Globalization;

// Simple tool to scan Razor views for Localizer usages and add missing keys to three resx files
// Usage: dotnet run --project tools/ResxSync/ResxSync.csproj <solutionRoot>

var argsList = args;
if (argsList.Length == 0)
{
    Console.WriteLine("Provide solution root path as first argument.");
    return;
}

var root = argsList[0];
var views = Directory.GetFiles(root, "*.cshtml", SearchOption.AllDirectories);
Console.WriteLine($"Scanning {views.Length} views...");

var keySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var rx = new System.Text.RegularExpressions.Regex(@"Localizer\[\s*""(?<key>[^""]+)""\s*\]|@Localizer\[\s*""(?<key2>[^""]+)""\s*\]", System.Text.RegularExpressions.RegexOptions.Compiled);

foreach (var v in views)
{
    var content = await File.ReadAllTextAsync(v);
    foreach (System.Text.RegularExpressions.Match m in rx.Matches(content))
    {
        var k = m.Groups["key"].Success ? m.Groups["key"].Value : m.Groups["key2"].Value;
        if (!string.IsNullOrWhiteSpace(k)) keySet.Add(k);
    }
}

Console.WriteLine($"Found {keySet.Count} keys in views.");

var resxPaths = new[] {
    Path.Combine(root, "Biblio_Web", "Resources", "Vertalingen", "SharedResource.nl.resx"),
    Path.Combine(root, "Biblio_Web", "Resources", "Vertalingen", "SharedResource.en.resx"),
    Path.Combine(root, "Biblio_Web", "Resources", "Vertalingen", "SharedResource.fr.resx")
};

foreach (var resx in resxPaths)
{
    if (!File.Exists(resx)) { Console.WriteLine($"Resx not found: {resx}"); continue; }
    var doc = XDocument.Load(resx);
    var rootElem = doc.Root!;
    var existing = new HashSet<string>(rootElem.Elements("data").Select(e => e.Attribute("name")?.Value), StringComparer.OrdinalIgnoreCase);
    var missing = keySet.Where(k => !existing.Contains(k)).OrderBy(k => k).ToList();
    Console.WriteLine($"{Path.GetFileName(resx)}: {missing.Count} missing keys.");
    if (missing.Count == 0) continue;
    foreach (var k in missing)
    {
        var data = new XElement("data",
            new XAttribute("name", k),
            new XAttribute(XNamespace.Xml + "space", "preserve"),
            new XElement("value", k)
        );
        rootElem.Add(data);
    }
    doc.Save(resx);
    Console.WriteLine($"Updated {resx} with {missing.Count} keys.");
}

Console.WriteLine("Done.");
