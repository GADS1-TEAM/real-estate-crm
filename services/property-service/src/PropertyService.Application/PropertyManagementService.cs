using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PropertyService.Domain.Aggregates;
using PropertyService.Application.Ports;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.BuildingBlocks.Messaging;
using RealEstateCrm.Contracts.Events;
using RealEstateCrm.Contracts.Properties;

namespace PropertyService.Application;

public class PropertyManagementService
{
    private readonly IRepository<Property, string> _propertyRepository;
    private readonly IRepository<PropertyInterest, string> _propertyInterestRepository;
    private readonly IPartyReferencePort _partyReferencePort;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutbox _outbox;
    private readonly TimeProvider _timeProvider;

    public PropertyManagementService(
        IRepository<Property, string> propertyRepository,
        IRepository<PropertyInterest, string> propertyInterestRepository,
        IPartyReferencePort partyReferencePort,
        IUnitOfWork unitOfWork,
        IOutbox outbox,
        TimeProvider timeProvider)
    {
        _propertyRepository = propertyRepository;
        _propertyInterestRepository = propertyInterestRepository;
        _partyReferencePort = partyReferencePort;
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _timeProvider = timeProvider;
    }

    public async Task<Property> CreatePropertyAsync(
        string propertyId,
        string propertyTypeCode,
        PropertyLocation location,
        decimal? surfaceM2,
        decimal? surfaceHa,
        PropertyPhysicalAttributes? physicalAttributes,
        PropertyRuralAttributes? ruralAttributes,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var property = Property.Create(propertyId, propertyTypeCode, location, surfaceM2, surfaceHa, physicalAttributes, ruralAttributes);

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _propertyRepository.AddAsync(property, token);
            await EnqueueEventAsync("PropertyRegistered", property.PropertyId, actorId, new PropertyRegisteredV1(property.PropertyId, property.PropertyTypeCode, property.LifecycleStatus, property.Location.Province, property.Location.Locality), token);
        }, cancellationToken);

        return property;
    }

    public async Task<Property> UpdatePropertyAsync(
        string propertyId,
        string lifecycleStatus,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property == null)
            throw new PropertyDomainException("property_not_found", 404, "Property not found");

        var updated = property.Update(lifecycleStatus);

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _propertyRepository.UpdateAsync(updated, token);
            await EnqueueEventAsync("PropertyUpdated", updated.PropertyId, actorId, new PropertyUpdatedV1(updated.PropertyId, updated.LifecycleStatus), token);
        }, cancellationToken);

        return updated;
    }

    public async Task<PropertyInterest> AddPropertyInterestAsync(
        string propertyId,
        string interestId,
        string holderPartyId,
        string rightType,
        decimal? participation,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetByIdAsync(propertyId, cancellationToken);
        if (property == null)
            throw new PropertyDomainException("property_not_found", 404, "Property not found");

        var partyExists = await _partyReferencePort.ExistsAsync(holderPartyId, cancellationToken);
        if (!partyExists)
            throw new PropertyDomainException("party_reference_not_found", 422, "Party reference not found");

        var interest = PropertyInterest.Create(interestId, propertyId, holderPartyId, rightType, participation);

        await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _propertyInterestRepository.AddAsync(interest, token);
            await EnqueueEventAsync("PropertyInterestAdded", property.PropertyId, actorId, new PropertyInterestAddedV1(property.PropertyId, interest.InterestId, interest.HolderPartyId, interest.RightType), token);
        }, cancellationToken);

        return interest;
    }

    private Task EnqueueEventAsync<TPayload>(string name, string aggregateId, Guid actorId, TPayload payload, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var envelope = new EventEnvelopeV1<TPayload>(
            EventId: Guid.NewGuid(),
            Name: name,
            Version: 1,
            OccurredAt: now,
            ActorId: actorId,
            CorrelationId: Guid.NewGuid(),
            CausationId: Guid.NewGuid(),
            AggregateId: Guid.Parse("00000000-0000-0000-0000-000000000000"), // We don't have Guid in PropertyId
            Payload: payload);

        return _outbox.EnqueueAsync(OutboxMessage.From(envelope, now), cancellationToken);
    }
}
