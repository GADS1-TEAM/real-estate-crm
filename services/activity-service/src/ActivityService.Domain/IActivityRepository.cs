using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ActivityService.Domain;

public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Activity>> GetByPartyIdAsync(string partyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Activity>> GetByCompanyIdAsync(string companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Activity>> GetByContactIdAsync(string contactId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Activity>> GetByPipelineItemIdAsync(string pipelineItemId, CancellationToken cancellationToken = default);
}
