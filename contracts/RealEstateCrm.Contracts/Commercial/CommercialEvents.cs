using System;
using System.Collections.Generic;

namespace RealEstateCrm.Contracts.Commercial;

public record VisitCompletedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record NegotiationOpenedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record ProposalSubmittedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record ProposalAcceptedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record ReservationConfirmedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record TransactionClosedV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
public record TransactionCancelledV1(string PipelineItemId, string SourceType, string SourceId, string StageCode, long Version);
