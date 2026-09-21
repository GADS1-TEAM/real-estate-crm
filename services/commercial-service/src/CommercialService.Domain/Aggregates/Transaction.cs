using System;
using System.Collections.Generic;

namespace CommercialService.Domain.Aggregates
{
    public class Transaction
    {
        public Guid TransactionId { get; private set; }
        public string OperationTypeCode { get; private set; } = string.Empty;
        public Guid PropertyId { get; private set; }
        public Guid? ListingId { get; private set; }
        public Guid? ReservationId { get; private set; }
        public Dictionary<string, Guid> ParticipantRoles { get; private set; } = new();
        public string AgreedTerms { get; private set; } = string.Empty;
        public DateTime? ClosedAt { get; private set; }
        public decimal? FinalValue { get; private set; }
        public string? Currency { get; private set; }
        public string Outcome { get; private set; } = string.Empty;

        private Transaction() { }

        public Transaction(string operationTypeCode, Guid propertyId, Guid? listingId, Guid? reservationId, Dictionary<string, Guid> participantRoles, string agreedTerms, DateTime? closedAt, decimal? finalValue, string? currency, string outcome)
        {
            if (outcome == "Closed" && (closedAt == null || finalValue == null))
            {
                throw new InvalidOperationException("Closing requires closedAt and finalValue for monetary operations.");
            }
            
            TransactionId = Guid.NewGuid();
            OperationTypeCode = operationTypeCode;
            PropertyId = propertyId;
            ListingId = listingId;
            ReservationId = reservationId;
            ParticipantRoles = participantRoles ?? new Dictionary<string, Guid>();
            AgreedTerms = agreedTerms;
            ClosedAt = closedAt;
            FinalValue = finalValue;
            Currency = currency;
            Outcome = outcome;
        }
    }
}
