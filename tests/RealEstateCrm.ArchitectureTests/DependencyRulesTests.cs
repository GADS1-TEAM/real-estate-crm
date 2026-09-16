using Xunit;

namespace RealEstateCrm.ArchitectureTests;

public class DependencyRulesTests
{
    private static readonly string[] ForbiddenDomainPackagePrefixes =
    {
        "MongoDB",
        "RabbitMQ",
        "Microsoft.AspNetCore",
        "Keycloak",
    };

    private static readonly string RepoRoot = RepoLocator.FindRepoRoot();

    private static IEnumerable<string> AllCsprojUnder(string relativeFolder) =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot, relativeFolder), "*.csproj", SearchOption.AllDirectories);

    [Fact]
    public void Domain_projects_do_not_reference_infrastructure_frameworks_or_other_projects()
    {
        var domainProjects = AllCsprojUnder("services")
            .Where(path => path.EndsWith(".Domain.csproj", StringComparison.Ordinal))
            .Select(path => new CsprojFile(path))
            .ToList();

        Assert.NotEmpty(domainProjects);

        var violations = new List<string>();

        foreach (var project in domainProjects)
        {
            var forbiddenPackages = project.PackageReferenceNames
                .Where(name => ForbiddenDomainPackagePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (forbiddenPackages.Count > 0)
            {
                violations.Add($"{project.FileName} referencia paquete(s) de infraestructura prohibidos: {string.Join(", ", forbiddenPackages)}");
            }

            if (project.ProjectReferenceAbsolutePaths.Count > 0)
            {
                violations.Add($"{project.FileName} tiene ProjectReference; el dominio no debe depender de ningún otro proyecto.");
            }
        }

        Assert.True(violations.Count == 0, "Violaciones de dependencia en Domain:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void Service_projects_do_not_reference_projects_of_another_service()
    {
        var servicesRoot = Path.Combine(RepoRoot, "services");
        var serviceProjects = AllCsprojUnder("services")
            .Select(path => new CsprojFile(path))
            .ToList();

        Assert.NotEmpty(serviceProjects);

        var violations = new List<string>();

        foreach (var project in serviceProjects)
        {
            var ownerService = ServiceNameOf(servicesRoot, project.FullPath);

            foreach (var referencedPath in project.ProjectReferenceAbsolutePaths)
            {
                if (!referencedPath.StartsWith(servicesRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // referencia a contracts/building-blocks: permitido, son shared kernel sin ownership de servicio.
                }

                var referencedService = ServiceNameOf(servicesRoot, referencedPath);

                if (!string.Equals(ownerService, referencedService, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(
                        $"{project.FileName} ({ownerService}) referencia un proyecto de otro servicio: " +
                        $"{Path.GetFileName(referencedPath)} ({referencedService})");
                }
            }
        }

        Assert.True(violations.Count == 0, "Violaciones de aislamiento entre servicios:\n" + string.Join("\n", violations));
    }

    private static string ServiceNameOf(string servicesRoot, string csprojFullPath)
    {
        var relative = Path.GetRelativePath(servicesRoot, csprojFullPath);
        return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    }
}
