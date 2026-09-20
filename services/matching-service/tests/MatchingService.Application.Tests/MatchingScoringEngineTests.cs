using System;
using MatchingService.Domain.Aggregates;
using MatchingService.Domain.Services;
using Xunit;

namespace MatchingService.Application.Tests;

public class MatchingScoringEngineTests
{
    [Fact]
    public void Calculate_WhenAllHardAndSoftCriteriaMatch_ReturnsEligibleWith100Score()
    {
        // Arrange
        var req = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 150000m,
            Currency: "USD",
            MinSurfaceArea: 50m,
            MinEnvironments: 2,
            Neighborhood: "Palermo Soho"
        );

        var listing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 140000m,
            Currency: "USD",
            SurfaceArea: 60m,
            Environments: 3,
            Neighborhood: "Palermo Soho"
        );

        // Act
        var result = MatchingScoringEngine.Calculate(req, listing);

        // Assert
        Assert.Equal(MatchEligibility.ELIGIBLE, result.Eligibility);
        Assert.Equal(100, result.Score);
        Assert.Equal(MatchStatus.PRESENTED, result.Status);
        Assert.All(result.HardCriteriaResults, h => Assert.True(h.Passed));
    }

    [Fact]
    public void Calculate_WhenOperationTypeMismatches_ReturnsIneligibleWithZeroScore()
    {
        // Arrange
        var req = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 150000m,
            Currency: "USD",
            MinSurfaceArea: 50m,
            MinEnvironments: 2,
            Neighborhood: "Palermo Soho"
        );

        var listing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-1",
            OperationTypeCode: "ALQUILER", // Mismatch
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 140000m,
            Currency: "USD",
            SurfaceArea: 60m,
            Environments: 3,
            Neighborhood: "Palermo Soho"
        );

        // Act
        var result = MatchingScoringEngine.Calculate(req, listing);

        // Assert
        Assert.Equal(MatchEligibility.INELIGIBLE, result.Eligibility);
        Assert.Equal(0, result.Score);
        Assert.Contains(result.Explanation, e => e.Contains("Operation type mismatch"));
    }

    [Fact]
    public void Calculate_WhenPriceExceedsBudget_ReturnsIneligibleWithZeroScore()
    {
        // Arrange
        var req = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 100000m,
            Currency: "USD",
            MinSurfaceArea: null,
            MinEnvironments: null,
            Neighborhood: ""
        );

        var listing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 120000m, // Exceeds 100000
            Currency: "USD",
            SurfaceArea: 60m,
            Environments: 3,
            Neighborhood: ""
        );

        // Act
        var result = MatchingScoringEngine.Calculate(req, listing);

        // Assert
        Assert.Equal(MatchEligibility.INELIGIBLE, result.Eligibility);
        Assert.Equal(0, result.Score);
        Assert.Contains(result.Explanation, e => e.Contains("Price exceeds maximum budget"));
    }

    [Fact]
    public void Calculate_WhenCurrencyMismatches_ReturnsIneligible()
    {
        // Arrange
        var req = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 100000m,
            Currency: "USD",
            MinSurfaceArea: null,
            MinEnvironments: null,
            Neighborhood: ""
        );

        var listing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 100000m,
            Currency: "ARS", // Mismatch
            SurfaceArea: 60m,
            Environments: 3,
            Neighborhood: ""
        );

        // Act
        var result = MatchingScoringEngine.Calculate(req, listing);

        // Assert
        Assert.Equal(MatchEligibility.INELIGIBLE, result.Eligibility);
        Assert.Equal(0, result.Score);
        Assert.Contains(result.HardCriteriaResults, h => h.CriterionName == "Budget" && !h.Passed);
    }

    [Fact]
    public void Calculate_WhenSomeSoftCriteriaMatch_ReturnsSoftMatchWithPartialScore()
    {
        // Arrange
        var req = new MatchingScoringEngine.RequirementSnapshot(
            Id: "req-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            MaxBudget: 150000m,
            Currency: "USD",
            MinSurfaceArea: 100m, // Listing has 60m -> fails soft
            MinEnvironments: 2,   // Listing has 3 -> passes soft (+25)
            Neighborhood: "Recoleta" // Listing has Palermo Soho -> fails soft
        );

        var listing = new MatchingScoringEngine.ListingSnapshot(
            Id: "lst-1",
            OperationTypeCode: "VENTA",
            PropertyTypeCode: "DEPARTAMENTO",
            Province: "Buenos Aires",
            Locality: "Palermo",
            Price: 100000m, // Below 135000 (10% lower bound) -> fails soft
            Currency: "USD",
            SurfaceArea: 60m,
            Environments: 3,
            Neighborhood: "Palermo Soho"
        );

        // Act
        var result = MatchingScoringEngine.Calculate(req, listing);

        // Assert
        Assert.Equal(MatchEligibility.SOFT_MATCH, result.Eligibility);
        Assert.Equal(25, result.Score);
    }
}
