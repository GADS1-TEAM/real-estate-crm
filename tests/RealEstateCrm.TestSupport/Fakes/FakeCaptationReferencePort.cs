using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstateCrm.BuildingBlocks.References;

namespace RealEstateCrm.TestSupport.Fakes;

public class FakeCaptationReferencePort : ICaptationReferencePort
{
    public HashSet<string> ExistingIds { get; } = new();

    public Task<bool> ExistsAsync(string captationCaseId)
    {
        return Task.FromResult(ExistingIds.Contains(captationCaseId));
    }
}
