using System.Threading.Tasks;

namespace RealEstateCrm.BuildingBlocks.References;

public interface IRequirementReferencePort
{
    Task<bool> ExistsAsync(string requirementId);
}
