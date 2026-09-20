using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PropertyService.Application;
using PropertyService.Application.Ports;
using PropertyService.Domain.Aggregates;
using RealEstateCrm.BuildingBlocks.Persistence;
using RealEstateCrm.BuildingBlocks.Messaging;
using System;
using System.Threading;

namespace PropertyService.Api.Tests;

public class PropertiesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PropertiesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockRepo = new Mock<IRepository<Property, string>>();
                var mockInterestRepo = new Mock<IRepository<PropertyInterest, string>>();
                var mockPartyRef = new Mock<IPartyReferencePort>();
                var mockUow = new Mock<IUnitOfWork>();
                var mockOutbox = new Mock<IOutbox>();
                var mockTime = new Mock<TimeProvider>();

                services.AddSingleton(mockRepo.Object);
                services.AddSingleton(mockInterestRepo.Object);
                services.AddSingleton(mockPartyRef.Object);
                services.AddSingleton(mockUow.Object);
                services.AddSingleton(mockOutbox.Object);
                services.AddSingleton(mockTime.Object);
                services.AddTransient<PropertyManagementService>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessAndJsonContentType()
    {
        var response = await _client.GetAsync("/api/v1/properties");
        response.EnsureSuccessStatusCode(); 
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }
}
