
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DemandService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events.Demand;
using RealEstateCrm.Contracts.Financial;
using RealEstateCrm.Contracts.Events;

namespace DemandService.Application.Commands;

public class CreateRequirementCommand
{
    public string RequirementId { get; set; } = Guid.NewGuid().ToString();
    public List<Guid> SeekerPartyIds { get; set; } = new();
    public string OperationTypeCode { get; set; } = "";
    public string? PropertyTypeCode { get; set; }
    public LocationCriteriaDto? LocationCriteria { get; set; }
    public FinancialCriteriaDto? FinancialCriteria { get; set; }
    public string? ResponsibleUserId { get; set; }
    public string? OriginCode { get; set; }
}

public record LocationCriteriaDto(string Province, string Locality, string? Neighborhood);
public record FinancialCriteriaDto(decimal MinAmount, decimal MaxAmount, string Currency);

public class CreateRequirementHandler
{
    private readonly IRepository<Requirement, string> _repository;
    private readonly IUnitOfWork _uow;
    private readonly IOutbox _outbox;

    public CreateRequirementHandler(IRepository<Requirement, string> repository, IUnitOfWork uow, IOutbox outbox)
    {
        _repository = repository;
        _uow = uow;
        _outbox = outbox;
    }

    public async Task HandleAsync(CreateRequirementCommand cmd, CancellationToken cancellationToken = default)
    {
        var location = cmd.LocationCriteria != null 
            ? new LocationCriteria(cmd.LocationCriteria.Province, cmd.LocationCriteria.Locality, cmd.LocationCriteria.Neighborhood) 
            : null;
            
        var financial = cmd.FinancialCriteria != null 
            ? new FinancialCriteria(new DemandService.Domain.Aggregates.Money(cmd.FinancialCriteria.MinAmount, cmd.FinancialCriteria.Currency), new DemandService.Domain.Aggregates.Money(cmd.FinancialCriteria.MaxAmount, cmd.FinancialCriteria.Currency)) 
            : null;

        var req = Requirement.Create(cmd.RequirementId, cmd.SeekerPartyIds, cmd.OperationTypeCode, cmd.PropertyTypeCode, location, financial, cmd.ResponsibleUserId, cmd.OriginCode);
        
        await _uow.ExecuteInTransactionAsync(async (ct) => 
        {
            await _repository.AddAsync(req, ct);
            var payload = new RequirementCreatedV1(
                req.RequirementId,
                req.SeekerPartyIds,
                req.OperationTypeCode,
                req.PropertyTypeCode,
                cmd.LocationCriteria != null ? new RealEstateCrm.Contracts.Events.Demand.LocationCriteriaDto(cmd.LocationCriteria.Province, cmd.LocationCriteria.Locality, cmd.LocationCriteria.Neighborhood) : null,
                cmd.FinancialCriteria != null ? new MoneyV1(cmd.FinancialCriteria.MinAmount, cmd.FinancialCriteria.Currency) : null,
                cmd.FinancialCriteria != null ? new MoneyV1(cmd.FinancialCriteria.MaxAmount, cmd.FinancialCriteria.Currency) : null,
                req.SelectedListingIds,
                req.ResponsibleUserId,
                req.OriginCode,
                new CommercialProgressDto(req.CommercialProgress.StageCode, req.CommercialProgress.StageCatalogVersion, req.CommercialProgress.ChangedAt, req.CommercialProgress.ChangedBy),
                req.Status,
                req.Version);
                
            var envelope = new EventEnvelopeV1<RequirementCreatedV1>(
                EventId: Guid.NewGuid(),
                Name: "RequirementCreated",
                Version: 1,
                OccurredAt: DateTimeOffset.UtcNow,
                ActorId: Guid.Empty,
                CorrelationId: Guid.Empty,
                CausationId: Guid.Empty,
                AggregateId: Guid.Parse(req.RequirementId),
                Payload: payload
            );
            
            await _outbox.EnqueueAsync(OutboxMessage.From(envelope, DateTimeOffset.UtcNow), ct);
        });
    }
}
