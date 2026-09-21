using System;
using System.Collections.Generic;
using CommercialService.Domain.Entities;

namespace CommercialService.Domain.Aggregates
{
    public class Negotiation
    {
        public Guid NegotiationId { get; private set; }
        public Guid? RequirementId { get; private set; }
        public Guid ListingId { get; private set; }
        public List<Guid> ParticipantPartyIds { get; private set; } = new();
        public string Status { get; private set; } = string.Empty;
        public List<Proposal> Proposals { get; private set; } = new();

        private Negotiation() { }

        public Negotiation(Guid? requirementId, Guid listingId, List<Guid> participantPartyIds, string status)
        {
            NegotiationId = Guid.NewGuid();
            RequirementId = requirementId;
            ListingId = listingId;
            ParticipantPartyIds = participantPartyIds ?? new List<Guid>();
            Status = status;
        }

        public void AddProposal(Proposal proposal)
        {
            Proposals.Add(proposal);
        }
    }
}
