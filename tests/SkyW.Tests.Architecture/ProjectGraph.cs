using System.Xml.Linq;

namespace SkyW.Tests.Architecture;

// Читает граф ссылок из .csproj: тест не должен зависеть от загрузки сборок.
internal sealed class ProjectGraph(
    Dictionary<string, string[]> projectReferences,
    Dictionary<string, string[]> packageReferences)
{
    public IReadOnlyCollection<string> Names => projectReferences.Keys;

    public static ProjectGraph Load()
    {
        string root = FindRoot();
        Dictionary<string, string[]> references = new(StringComparer.Ordinal);
        Dictionary<string, string[]> packages = new(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(path)) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            XDocument document = XDocument.Load(path);
            references[name] = ReadProjectReferences(document);
            packages[name] = ReadPackageReferences(document);
        }
        return new ProjectGraph(references, packages);
    }

    public string[] ReferencesOf(string project) => projectReferences.TryGetValue(project, out string[]? references)
        ? references
        : throw new InvalidOperationException($"Проект не найден: {project}");

    public string[] PackageReferencesOf(string project) => packageReferences.TryGetValue(project, out string[]? references)
        ? references
        : throw new InvalidOperationException($"Проект не найден: {project}");

    private static string[] ReadProjectReferences(XDocument document) => document
        .Descendants("ProjectReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(include => !string.IsNullOrWhiteSpace(include))
        .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
        .ToArray();

    private static string[] ReadPackageReferences(XDocument document) => document
        .Descendants("PackageReference")
        .Select(element => (string?)element.Attribute("Include"))
        .Where(include => !string.IsNullOrWhiteSpace(include))
        .Select(include => include!)
        .ToArray();

    private static bool IsBuildArtifact(string path)
    {
        string separator = Path.DirectorySeparatorChar.ToString();
        return path.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
            || path.Contains($"{separator}obj{separator}", StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyW.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new InvalidOperationException("Корень репозитория с SkyW.slnx не найден.");
    }
}
