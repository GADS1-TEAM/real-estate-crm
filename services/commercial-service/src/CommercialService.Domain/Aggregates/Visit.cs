using System;
using System.Collections.Generic;

namespace CommercialService.Domain.Aggregates
{
    public class Visit
    {
        public Guid VisitId { get; private set; }
        public Guid PropertyId { get; private set; }
        public Guid ListingId { get; private set; }
        public Guid? RequirementId { get; private set; }
        public List<Guid> VisitorPartyIds { get; private set; } = new();
        public Guid ResponsibleUserId { get; private set; }
        public DateTime OccurredAt { get; private set; }
        public string Outcome { get; private set; } = string.Empty;
        public string Notes { get; private set; } = string.Empty;

        private Visit() { }

        public Visit(Guid propertyId, Guid listingId, Guid? requirementId, List<Guid> visitorPartyIds, Guid responsibleUserId, DateTime occurredAt, string outcome, string notes)
        {
            if (occurredAt > DateTime.UtcNow) throw new InvalidOperationException("OccurredAt must be in the past/present.");
            VisitId = Guid.NewGuid();
            PropertyId = propertyId;
            ListingId = listingId;
            RequirementId = requirementId;
            VisitorPartyIds = visitorPartyIds ?? new List<Guid>();
            ResponsibleUserId = responsibleUserId;
            OccurredAt = occurredAt;
            Outcome = outcome;
            Notes = notes;
        }
    }
}
