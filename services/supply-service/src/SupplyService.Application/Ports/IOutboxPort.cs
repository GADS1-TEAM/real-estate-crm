using System.Threading;
using System.Threading.Tasks;

namespace SupplyService.Application.Ports;

public interface IOutboxPort
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken) where T : class;
}
