namespace RealEstateCrm.Contracts.Analytics;

public record DashboardPipelineResponseV1(
    Dictionary<string, int> OpenProcessesByStage,
    Dictionary<string, int> OpenProcessesByStatus,
    Dictionary<string, int> OpenProcessesByResponsible,
    Dictionary<string, int> OpenProcessesByOrigin);

public record ActivityStatisticsResponseV1(
    Dictionary<string, int> ActivityVolumeByType,
    Dictionary<string, int> ActivityVolumeByUser,
    string Period);

public record PropertyPerformanceResponseV1(
    int ActiveListings,
    int Visits,
    int Negotiations,
    int Closed);

public record ExplainMetricResponseV1(
    string Formula,
    string Version,
    List<string> SourceEvents,
    List<string> Lineage);
