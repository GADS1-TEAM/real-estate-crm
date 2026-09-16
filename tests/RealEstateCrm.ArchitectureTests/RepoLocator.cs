using System.IO;

namespace RealEstateCrm.ArchitectureTests;

internal static class RepoLocator
{
    public static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RealEstateCrm.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("No se pudo ubicar RealEstateCrm.slnx subiendo desde el directorio de test.");
        }

        return directory.FullName;
    }
}
