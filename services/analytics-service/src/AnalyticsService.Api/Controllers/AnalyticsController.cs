using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using RealEstateCrm.Contracts.Analytics;

namespace AnalyticsService.Api.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    [HttpGet("dashboard/pipeline")]
    public ActionResult<DashboardPipelineResponseV1> GetDashboardPipeline()
    {
        var response = new DashboardPipelineResponseV1(
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            new Dictionary<string, int>()
        );
        return Ok(response);
    }

    [HttpGet("statistics/activities")]
    public ActionResult<ActivityStatisticsResponseV1> GetActivityStatistics()
    {
        var response = new ActivityStatisticsResponseV1(
            new Dictionary<string, int>(),
            new Dictionary<string, int>(),
            "2023-01"
        );
        return Ok(response);
    }

    [HttpGet("performance/properties")]
    public ActionResult<PropertyPerformanceResponseV1> GetPropertyPerformance()
    {
        var response = new PropertyPerformanceResponseV1(0, 0, 0, 0);
        return Ok(response);
    }

    [HttpGet("metrics/{metricCode}/explain")]
    public ActionResult<ExplainMetricResponseV1> ExplainMetric(string metricCode)
    {
        var response = new ExplainMetricResponseV1(
            "SUM(events)",
            "1.0",
            new List<string>(),
            new List<string>()
        );
        return Ok(response);
    }
}
