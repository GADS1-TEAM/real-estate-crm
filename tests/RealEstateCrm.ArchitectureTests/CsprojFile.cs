using System.Xml.Linq;

namespace RealEstateCrm.ArchitectureTests;

internal sealed record CsprojFile(string FullPath)
{
    private readonly Lazy<XDocument> _document = new(() => XDocument.Load(FullPath));

    public string FileName => Path.GetFileName(FullPath);

    public IReadOnlyList<string> PackageReferenceNames =>
        _document.Value.Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(name => name.Length > 0)
            .ToList();

    public IReadOnlyList<string> ProjectReferenceAbsolutePaths =>
        _document.Value.Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .Where(include => include.Length > 0)
            .Select(include => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FullPath)!, include)))
            .ToList();
}
