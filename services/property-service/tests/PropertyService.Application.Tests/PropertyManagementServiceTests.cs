using Moq;
using PropertyService.Application;
using PropertyService.Application.Ports;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.BuildingBlocks.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PropertyService.Application.Tests;

public class PropertyManagementServiceTests
{
    private readonly Mock<IRepository<Property, string>> _propertyRepoMock = new();
    private readonly Mock<IRepository<PropertyInterest, string>> _interestRepoMock = new();
    private readonly Mock<IPartyReferencePort> _partyRefMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IOutbox> _outboxMock = new();
    private readonly PropertyManagementService _sut;

    public PropertyManagementServiceTests()
    {
        _uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, token) => action(token));

        var timeProvider = new Moq.Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(DateTimeOffset.UtcNow);

        _sut = new PropertyManagementService(
            _propertyRepoMock.Object,
            _interestRepoMock.Object,
            _partyRefMock.Object,
            _uowMock.Object,
            _outboxMock.Object,
            timeProvider.Object);
    }

    [Fact]
    public async Task CreateProperty_ShouldPublishEvent()
    {
        var property = await _sut.CreatePropertyAsync("prop-1", "House", new PropertyLocation(null, null, null, null, null, null, null), null, null, null, null, Guid.NewGuid());
        Assert.NotNull(property);
        _propertyRepoMock.Verify(r => r.AddAsync(It.IsAny<Property>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxMock.Verify(o => o.EnqueueAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPropertyInterest_WithInvalidParty_ThrowsException()
    {
        var property = Property.Create("prop-1", "House", new PropertyLocation(null, null, null, null, null, null, null), null, null, null, null);
        _propertyRepoMock.Setup(r => r.GetByIdAsync("prop-1", It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _partyRefMock.Setup(p => p.ExistsAsync("party-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<PropertyDomainException>(() => _sut.AddPropertyInterestAsync("prop-1", "int-1", "party-1", "Owner", 100, Guid.NewGuid()));
        Assert.Equal(422, ex.HttpStatus);
        Assert.Equal("party_reference_not_found", ex.ErrorCode);
    }
}