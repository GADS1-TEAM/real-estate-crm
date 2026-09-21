using System;
using System.Collections.Generic;

namespace CommercialService.Application.Commands
{
    public class CreateVisitCommand
    {
        public Guid PropertyId { get; set; }
        public Guid ListingId { get; set; }
        public Guid? RequirementId { get; set; }
        public List<Guid> VisitorPartyIds { get; set; } = new();
        public Guid ResponsibleUserId { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
