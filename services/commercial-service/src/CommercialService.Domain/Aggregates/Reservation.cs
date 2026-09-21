using System;
using System.Collections.Generic;

namespace CommercialService.Domain.Aggregates
{
    public class Reservation
    {
        public Guid ReservationId { get; private set; }
        public Guid ListingId { get; private set; }
        public Guid? NegotiationId { get; private set; }
        public Guid? AcceptedProposalId { get; private set; }
        public List<Guid> ParticipantPartyIds { get; private set; } = new();
        public decimal ReservedValue { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public DateTime ValidFrom { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public string Status { get; private set; } = string.Empty;

        private Reservation() { }

        public Reservation(Guid listingId, Guid? negotiationId, Guid? acceptedProposalId, List<Guid> participantPartyIds, decimal reservedValue, string currency, DateTime validFrom, DateTime expiresAt, string status)
        {
            ReservationId = Guid.NewGuid();
            ListingId = listingId;
            NegotiationId = negotiationId;
            AcceptedProposalId = acceptedProposalId;
            ParticipantPartyIds = participantPartyIds ?? new List<Guid>();
            ReservedValue = reservedValue;
            Currency = currency;
            ValidFrom = validFrom;
            ExpiresAt = expiresAt;
            Status = status;
        }
    }
}
