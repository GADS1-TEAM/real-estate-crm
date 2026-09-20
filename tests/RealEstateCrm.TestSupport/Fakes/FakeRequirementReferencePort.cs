using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.TestSupport.Fakes;

public class FakeRequirementReferencePort : IRequirementReferencePort
{
    public HashSet<string> ExistingIds { get; } = new();

    public Task<bool> ExistsAsync(string requirementId)
    {
        return Task.FromResult(ExistingIds.Contains(requirementId));
    }
}
