using System.Threading;
using System.Threading.Tasks;

namespace PropertyService.Application.Ports;

public interface IPartyReferencePort
{
    Task<bool> ExistsAsync(string partyId, CancellationToken cancellationToken = default);
}
