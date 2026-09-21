using System;

namespace CommercialService.Domain.Entities
{
    public class Proposal
    {
        public Guid ProposalId { get; private set; }
        public int Sequence { get; private set; }
        public Guid ProposedByPartyId { get; private set; }
        public string TermsSnapshot { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private Proposal() { }

        public Proposal(int sequence, Guid proposedByPartyId, string termsSnapshot, DateTime createdAt)
        {
            ProposalId = Guid.NewGuid();
            Sequence = sequence;
            ProposedByPartyId = proposedByPartyId;
            TermsSnapshot = termsSnapshot;
            CreatedAt = createdAt;
        }
    }
}
