using System.Threading.Tasks;

namespace RealEstateCrm.BuildingBlocks.References;

public interface ICaptationReferencePort
{
    Task<bool> ExistsAsync(string captationCaseId);
}
