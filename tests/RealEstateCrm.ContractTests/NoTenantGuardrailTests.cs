using System.Reflection;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.Contracts.Context;

namespace RealEstateCrm.ContractTests;

/// <summary>
/// Guardrail automático de ADR-001 sección 1: ningún contrato ni puerto V2 puede declarar
/// tenantId/organizationId. Si alguien lo agrega en una PR futura, este test lo detecta sin
/// depender de una revisión manual.
/// </summary>
public class NoTenantGuardrailTests
{
    [Fact]
    public void Contracts_and_building_blocks_assemblies_declare_no_tenant_or_organization_members()
    {
        var assemblies = new[]
        {
            typeof(ExecutionContextV1).Assembly,
            typeof(IRepository<,>).Assembly,
        };

        var violations = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsPublic || type.IsNestedPublic)
            .SelectMany(DescribeForbiddenMembers)
            .ToList();

        Assert.True(violations.Count == 0, "tenantId/organizationId encontrado:\n" + string.Join("\n", violations));
    }

    private static IEnumerable<string> DescribeForbiddenMembers(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (IsForbiddenName(property.Name))
            {
                yield return $"{type.FullName}.{property.Name} (propiedad)";
            }
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            foreach (var parameter in method.GetParameters())
            {
                if (IsForbiddenName(parameter.Name ?? string.Empty))
                {
                    yield return $"{type.FullName}.{method.Name}({parameter.Name}) (parámetro)";
                }
            }
        }
    }

    private static bool IsForbiddenName(string name) =>
        name.Contains("tenant", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("organization", StringComparison.OrdinalIgnoreCase);
}
