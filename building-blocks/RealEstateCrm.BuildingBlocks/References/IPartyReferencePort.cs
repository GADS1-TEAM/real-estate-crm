using System;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstateCrm.BuildingBlocks.References;

public sealed record PartyReferenceV1(Guid PartyId, string Kind, string DisplayName, string IdentityStatus, string CommercialStatus);

public interface IPartyReferencePort
{
    Task<PartyReferenceV1?> GetAsync(Guid partyId, CancellationToken ct = default);
}
