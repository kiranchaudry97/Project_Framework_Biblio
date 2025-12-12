using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Text;

// Usage: dotnet run --project Tools/ResxSync/ResxSync.csproj <solutionRoot> [--fill]
// Generates a synchronization report for SharedResource.*.resx under Biblio_Web/Resources/Vertalingen
// If --fill is provided, missing keys in en/fr will be filled from English -> Dutch -> key.

var argsList = args;
if (argsList.Length == 0)
{
    Console.WriteLine("Provide solution root path as first argument.");
    return;
}

var root = argsList[0];
var doFill = argsList.Length > 1 && argsList[1] == "--fill";

var folder = Path.Combine(root, "Biblio_Web", "Resources", "Vertalingen");
if (!Directory.Exists(folder))
{
    Console.WriteLine($"Folder not found: {folder}");
    return;
}

var files = Directory.GetFiles(folder, "SharedResource.*.resx", SearchOption.TopDirectoryOnly);
if (files.Length == 0)
{
    Console.WriteLine("No SharedResource.*.resx files found in Vertalingen.");
    return;
}

Console.WriteLine($"Found {files.Length} resource files.");

// Load all docs and keys
var docs = new Dictionary<string, XDocument>(StringComparer.OrdinalIgnoreCase);
var keyValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

foreach (var f in files)
{
    var doc = XDocument.Load(f);
    docs[f] = doc;
    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var data in doc.Root.Elements("data"))
    {
        var name = (string?)data.Attribute("name");
        if (string.IsNullOrEmpty(name)) continue;
        var val = (string?)data.Element("value") ?? string.Empty;
        map[name] = val;
        allKeys.Add(name);
    }
    keyValues[f] = map;
}

// Prepare CSV report
var csvLines = new List<string>();
csvLines.Add("Key,Present_in_EN,Present_in_NL,Present_in_FR,EN_Value,NL_Value,FR_Value,Status");

string enPath = files.FirstOrDefault(p => p.EndsWith(".en.resx", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
string nlPath = files.FirstOrDefault(p => p.EndsWith(".nl.resx", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
string frPath = files.FirstOrDefault(p => p.EndsWith(".fr.resx", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

int totalMissing = 0;
var missingPerFile = new Dictionary<string, List<string>>();

foreach (var f in files) missingPerFile[f] = new List<string>();

foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
{
    var presentEn = !string.IsNullOrEmpty(enPath) && keyValues[enPath].ContainsKey(key);
    var presentNl = !string.IsNullOrEmpty(nlPath) && keyValues[nlPath].ContainsKey(key);
    var presentFr = !string.IsNullOrEmpty(frPath) && keyValues[frPath].ContainsKey(key);

    var enVal = presentEn ? keyValues[enPath][key] : string.Empty;
    var nlVal = presentNl ? keyValues[nlPath][key] : string.Empty;
    var frVal = presentFr ? keyValues[frPath][key] : string.Empty;

    var status = new List<string>();
    if (!presentEn) { status.Add("MISSING_EN"); missingPerFile[enPath].Add(key); }
    if (!presentNl) { status.Add("MISSING_NL"); missingPerFile[nlPath].Add(key); }
    if (!presentFr) { status.Add("MISSING_FR"); missingPerFile[frPath].Add(key); }

    // untranslated heuristics: value equals key or empty
    if (presentEn && string.Equals(enVal, key, StringComparison.OrdinalIgnoreCase)) status.Add("UNTRANSLATED_EN");
    if (presentNl && string.Equals(nlVal, key, StringComparison.OrdinalIgnoreCase)) status.Add("UNTRANSLATED_NL");
    if (presentFr && string.Equals(frVal, key, StringComparison.OrdinalIgnoreCase)) status.Add("UNTRANSLATED_FR");

    var statusText = status.Count == 0 ? "OK" : string.Join("|", status);
    if (statusText != "OK") totalMissing++;

    string escape(string s)
    {
        if (s == null) return string.Empty;
        return '"' + s.Replace("\"", "\"\"") + '"';
    }

    csvLines.Add($"{escape(key)},{presentEn},{presentNl},{presentFr},{escape(enVal)},{escape(nlVal)},{escape(frVal)},{escape(statusText)}");
}

// Save report
var reportFolder = Path.Combine(root, "scripts");
if (!Directory.Exists(reportFolder)) Directory.CreateDirectory(reportFolder);
var csvPath = Path.Combine(reportFolder, "resx-report.csv");
File.WriteAllLines(csvPath, csvLines, Encoding.UTF8);

Console.WriteLine($"Wrote report to {csvPath}");

// Optionally fill missing keys in en/fr using fallback (en->nl->key)
if (doFill)
{
    Console.WriteLine("Filling missing keys in resource files (using EN->NL->key fallback)...");
    foreach (var f in files)
    {
        var doc = docs[f];
        var rootElem = doc.Root!;
        var existing = new HashSet<string>(rootElem.Elements("data").Select(e => e.Attribute("name")?.Value), StringComparer.OrdinalIgnoreCase);
        var missing = allKeys.Where(k => !existing.Contains(k)).ToList();
        if (missing.Count == 0) continue;

        foreach (var k in missing)
        {
            string fill = string.Empty;
            if (!string.IsNullOrEmpty(enPath) && keyValues.ContainsKey(enPath) && keyValues[enPath].TryGetValue(k, out var ev) && !string.IsNullOrEmpty(ev)) fill = ev;
            else if (!string.IsNullOrEmpty(nlPath) && keyValues.ContainsKey(nlPath) && keyValues[nlPath].TryGetValue(k, out var nv) && !string.IsNullOrEmpty(nv)) fill = nv;
            else fill = k;

            // For non-English files, if we are filling, mark for translation
            if (!f.Equals(enPath, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(fill))
            {
                fill = "[TRANSLATE] " + fill;
            }

            var data = new XElement("data",
                new XAttribute("name", k),
                new XAttribute(XNamespace.Xml + "space", "preserve"),
                new XElement("value", fill)
            );
            rootElem.Add(data);
            Console.WriteLine($"Added key {k} to {Path.GetFileName(f)}");
        }
        doc.Save(f);
    }
    Console.WriteLine("Finished filling missing keys.");
}

// Summary
Console.WriteLine("Summary:");
foreach (var f in files)
{
    var missing = missingPerFile[f].Count;
    Console.WriteLine($"{Path.GetFileName(f)}: missing {missing} keys.");
}
Console.WriteLine($"Total issues found: {totalMissing}");
Console.WriteLine("Done.");
