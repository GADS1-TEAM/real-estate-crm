using System;
using System.Collections.Generic;
using MatchingService.Domain.Aggregates;

namespace MatchingService.Domain.Services;

public static class MatchingScoringEngine
{
    // Dummy snapshot classes for now
    public record RequirementSnapshot(string Id, string OperationTypeCode, string PropertyTypeCode, string Province, string Locality, decimal MaxBudget, string Currency, decimal? MinSurfaceArea, int? MinEnvironments, string Neighborhood);
    public record ListingSnapshot(string Id, string OperationTypeCode, string PropertyTypeCode, string Province, string Locality, decimal Price, string Currency, decimal? SurfaceArea, int? Environments, string Neighborhood);

    public static MatchCase Calculate(RequirementSnapshot req, ListingSnapshot listing)
    {
        var hardResults = new List<HardCriterionResult>();
        var softResults = new List<SoftCriterionResult>();
        var explanation = new List<string>();

        // 1. OperationTypeCode
        bool opPass = req.OperationTypeCode == listing.OperationTypeCode;
        hardResults.Add(new HardCriterionResult("OperationTypeCode", opPass, opPass ? "Match" : "Mismatch"));
        if (!opPass) explanation.Add("Operation type mismatch.");

        // 2. PropertyTypeCode
        bool propPass = req.PropertyTypeCode == listing.PropertyTypeCode;
        hardResults.Add(new HardCriterionResult("PropertyTypeCode", propPass, propPass ? "Match" : "Mismatch"));
        if (!propPass) explanation.Add("Property type mismatch.");

        // 3. Location
        bool locPass = req.Province == listing.Province && req.Locality == listing.Locality;
        hardResults.Add(new HardCriterionResult("Location", locPass, locPass ? "Match" : "Mismatch"));
        if (!locPass) explanation.Add("Location mismatch.");

        // 4. Budget
        bool budgetPass = false;
        if (req.Currency != listing.Currency)
        {
            hardResults.Add(new HardCriterionResult("Budget", false, "different_currency_unsupported"));
            explanation.Add("Currency mismatch.");
        }
        else
        {
            budgetPass = listing.Price <= req.MaxBudget;
            hardResults.Add(new HardCriterionResult("Budget", budgetPass, budgetPass ? "Match" : "Price exceeds budget"));
            if (!budgetPass) explanation.Add("Price exceeds maximum budget.");
        }

        bool anyHardFail = !opPass || !propPass || !locPass || !budgetPass || req.Currency != listing.Currency;
        
        int score = 0;
        if (!anyHardFail)
        {
            // Soft criteria
            // Surface area (+25)
            if (listing.SurfaceArea.HasValue && req.MinSurfaceArea.HasValue && listing.SurfaceArea >= req.MinSurfaceArea)
            {
                score += 25;
                softResults.Add(new SoftCriterionResult("SurfaceArea", 25, 25, 25, "Match"));
                explanation.Add("Surface area requirement met.");
            }
            else if (!listing.SurfaceArea.HasValue) { explanation.Add("Surface area UNKNOWN."); }

            // Environments (+25)
            if (listing.Environments.HasValue && req.MinEnvironments.HasValue && listing.Environments >= req.MinEnvironments)
            {
                score += 25;
                softResults.Add(new SoftCriterionResult("Environments", 25, 25, 25, "Match"));
                explanation.Add("Environments requirement met.");
            }
            else if (!listing.Environments.HasValue) { explanation.Add("Environments UNKNOWN."); }

            // Neighborhood (+25)
            if (listing.Neighborhood == req.Neighborhood)
            {
                score += 25;
                softResults.Add(new SoftCriterionResult("Neighborhood", 25, 25, 25, "Match"));
                explanation.Add("Neighborhood match.");
            }
            
            // Price within 10% (+25)
            decimal lowerBound = req.MaxBudget * 0.9m;
            if (listing.Price >= lowerBound && listing.Price <= req.MaxBudget)
            {
                score += 25;
                softResults.Add(new SoftCriterionResult("Price10Percent", 25, 25, 25, "Match"));
                explanation.Add("Price within 10% of budget.");
            }
        }

        MatchEligibility eligibility = MatchEligibility.INELIGIBLE;
        if (!anyHardFail)
        {
            eligibility = score >= 80 ? MatchEligibility.ELIGIBLE : MatchEligibility.SOFT_MATCH;
        }

        return new MatchCase
        {
            MatchId = Guid.NewGuid().ToString(),
            RequirementId = req.Id,
            ListingId = listing.Id,
            Eligibility = eligibility,
            Score = anyHardFail ? 0 : score,
            HardCriteriaResults = hardResults,
            SoftCriteriaResults = softResults,
            Explanation = explanation,
            CalculatedAt = DateTimeOffset.UtcNow,
            Status = MatchStatus.PRESENTED,
            Version = 1
        };
    }
}
