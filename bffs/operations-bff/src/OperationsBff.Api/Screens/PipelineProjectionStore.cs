using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace OperationsBff.Api.Screens;

public sealed class PipelineProjectionStore
{
    private readonly IMongoCollection<PipelineOpportunityDoc> _opportunities;
    private readonly IMongoCollection<PipelineStageHistoryDoc> _stageHistory;
    private readonly IMongoCollection<PipelineProposalDoc> _proposals;
    private readonly IMongoCollection<PipelineReservationDoc> _reservations;
    private readonly IMongoCollection<PipelineRelationshipDoc> _relationships;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public PipelineProjectionStore(IConfiguration configuration)
    {
        var connectionString = configuration["Mongo:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("Mongo__ConnectionString")
            ?? "mongodb://localhost:27017/?directConnection=true";
        var databaseName = configuration["Mongo:Database"] ?? "crm_pipeline_projection";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        _opportunities = database.GetCollection<PipelineOpportunityDoc>("opportunities");
        _stageHistory = database.GetCollection<PipelineStageHistoryDoc>("stage_history");
        _proposals = database.GetCollection<PipelineProposalDoc>("proposals");
        _reservations = database.GetCollection<PipelineReservationDoc>("reservations");
        _relationships = database.GetCollection<PipelineRelationshipDoc>("relationships");
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            var count = await _opportunities.CountDocumentsAsync(FilterDefinition<PipelineOpportunityDoc>.Empty, cancellationToken: cancellationToken);
            if (count == 0)
            {
                var defaults = new List<PipelineOpportunityDoc>
                {
                    new() { Id = "opp-1", Title = "Ana Suárez · Casa en Villa Crespo", SourceType = "REQUIREMENT", SourceId = "demand-1", Stage = "Visita", Owner = "Martín Quiroga", Fee = 7050000, Currency = "ARS", DaysInStage = 4, Origin = "WhatsApp histórico", CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new() { Id = "opp-2", Title = "Carla Benítez · PH en Guardia Vieja", SourceType = "REQUIREMENT", SourceId = "demand-2", Stage = "Negociación", Owner = "Martín Quiroga", Fee = 5340000, Currency = "ARS", DaysInStage = 9, Origin = "Referido", CreatedAt = DateTime.UtcNow.AddDays(-9) },
                    new() { Id = "opp-3", Title = "Captación Casa Villa Crespo", SourceType = "CAPTATION_CASE", SourceId = "captation-1", Stage = "Contacto", Owner = "Lucía Ferrari", Fee = 8700000, Currency = "ARS", DaysInStage = 3, Origin = "Referido", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new() { Id = "opp-4", Title = "Captación Casa Villa Devoto", SourceType = "CAPTATION_CASE", SourceId = "captation-2", Stage = "Nuevo", Owner = "Lucía Ferrari", Fee = 0, Currency = "USD", DaysInStage = 1, Origin = "Carga manual", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new() { Id = "opp-101", Title = "Búsqueda Dpto Palermo (Juan Pérez)", SourceType = "REQUIREMENT", SourceId = "22222222-1111-4000-8000-000000000001", Stage = "Nuevo", Owner = "Martín Quiroga", Fee = 6000, Currency = "USD", DaysInStage = 2, Origin = "Portal inmobiliario", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new() { Id = "opp-102", Title = "Venta Casa San Isidro (María Gonzalez)", SourceType = "REQUIREMENT", SourceId = "22222222-1111-4000-8000-000000000002", Stage = "Contacto", Owner = "Lucía Ferrari", Fee = 18000, Currency = "USD", DaysInStage = 4, Origin = "Portal inmobiliario", CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new() { Id = "opp-103", Title = "Alquiler Oficina Retiro (Inversiones SA)", SourceType = "REQUIREMENT", SourceId = "22222222-1111-4000-8000-000000000003", Stage = "Visita", Owner = "Rodrigo Vergara", Fee = 2500, Currency = "USD", DaysInStage = 1, Origin = "Referido", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new() { Id = "opp-104", Title = "Venta Penthouse Puerto Madero (Carlos Ruiz)", SourceType = "REQUIREMENT", SourceId = "22222222-1111-4000-8000-000000000004", Stage = "Negociación", Owner = "Martín Quiroga", Fee = 30000, Currency = "USD", DaysInStage = 6, Origin = "Carga manual", CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new() { Id = "opp-105", Title = "Venta Lote Nordelta (Ana Martínez)", SourceType = "REQUIREMENT", SourceId = "22222222-1111-4000-8000-000000000005", Stage = "Reserva", Owner = "Lucía Ferrari", Fee = 7500, Currency = "USD", DaysInStage = 3, Origin = "Portal inmobiliario", CreatedAt = DateTime.UtcNow.AddDays(-3) }
                };
                await _opportunities.InsertManyAsync(defaults, cancellationToken: cancellationToken);
            }

            var historyCount = await _stageHistory.CountDocumentsAsync(FilterDefinition<PipelineStageHistoryDoc>.Empty, cancellationToken: cancellationToken);
            if (historyCount == 0)
            {
                await _stageHistory.InsertOneAsync(new PipelineStageHistoryDoc
                {
                    Id = "history-1",
                    OpportunityId = "opp-2",
                    From = "Visita",
                    To = "Negociación",
                    Actor = "Martín Quiroga",
                    At = "Ayer, 16:30",
                    Reason = "Se confirmó interés y se inició negociación comercial.",
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                }, cancellationToken: cancellationToken);
            }

            var reservationCount = await _reservations.CountDocumentsAsync(FilterDefinition<PipelineReservationDoc>.Empty, cancellationToken: cancellationToken);
            if (reservationCount == 0)
            {
                await _reservations.InsertManyAsync(new[]
                {
                    new PipelineReservationDoc { Id = "reservation-1", OpportunityId = "opp-2", PropertyTitle = "PH en Guardia Vieja 3355", Deposit = 1500000m, Currency = "ARS", Status = "Activa" },
                    new PipelineReservationDoc { Id = "res-101", OpportunityId = "opp-105", PropertyTitle = "Lote al Lago Central en Nordelta", Deposit = 10000m, Currency = "USD", Status = "Activa" }
                }, cancellationToken: cancellationToken);
            }

            var relCount = await _relationships.CountDocumentsAsync(FilterDefinition<PipelineRelationshipDoc>.Empty, cancellationToken: cancellationToken);
            if (relCount == 0)
            {
                await _relationships.InsertOneAsync(new PipelineRelationshipDoc
                {
                    Id = "relationship-1",
                    FromPartyId = "contact-1",
                    ToPartyId = "contact-4",
                    RelationshipType = "CONTACT_OF",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    CreatedBy = "Martín Quiroga"
                }, cancellationToken: cancellationToken);
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IReadOnlyList<PipelineOpportunityDoc>> GetOpportunitiesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        return await _opportunities.Find(FilterDefinition<PipelineOpportunityDoc>.Empty)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PipelineStageHistoryDoc>> GetStageHistoryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        return await _stageHistory.Find(FilterDefinition<PipelineStageHistoryDoc>.Empty)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PipelineProposalDoc>> GetProposalsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        return await _proposals.Find(FilterDefinition<PipelineProposalDoc>.Empty)
            .SortBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PipelineReservationDoc>> GetReservationsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        return await _reservations.Find(FilterDefinition<PipelineReservationDoc>.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PipelineRelationshipDoc>> GetRelationshipsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        return await _relationships.Find(FilterDefinition<PipelineRelationshipDoc>.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task<PipelineOpportunityDoc> CreateOpportunityAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var id = GetString(payload, "id") ?? $"opp-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var title = GetString(payload, "title") ?? "Nueva Oportunidad";
        var sourceType = GetString(payload, "sourceType") ?? "REQUIREMENT";
        var sourceId = GetString(payload, "sourceId") ?? "demand-1";
        var stage = GetString(payload, "stage") ?? "Nuevo";
        var owner = GetString(payload, "owner") ?? "Martín Quiroga";
        var currency = GetString(payload, "currency") ?? "USD";
        var origin = GetString(payload, "origin") ?? "Carga manual";
        var fee = GetDecimal(payload, "fee") ?? 0m;

        var doc = new PipelineOpportunityDoc
        {
            Id = id,
            Title = title,
            SourceType = sourceType,
            SourceId = sourceId,
            Stage = stage,
            Owner = owner,
            Fee = fee,
            Currency = currency,
            DaysInStage = 0,
            Origin = origin,
            CreatedAt = DateTime.UtcNow
        };

        await _opportunities.ReplaceOneAsync(
            x => x.Id == id,
            doc,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        await _stageHistory.InsertOneAsync(new PipelineStageHistoryDoc
        {
            Id = $"history-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            OpportunityId = id,
            From = "Nuevo",
            To = stage,
            Actor = owner,
            At = "Ahora",
            Reason = "Creación de la oportunidad en el embudo",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        return doc;
    }

    public async Task<bool> ChangeStageAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var id = GetString(payload, "id") ?? GetString(payload, "opportunityId");
        var nextStage = GetString(payload, "stage") ?? GetString(payload, "newStageCode");
        var reason = GetString(payload, "reason") ?? "Actualización de etapa";
        var actor = GetString(payload, "actor") ?? "Martín Quiroga";

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(nextStage))
            return false;

        var existing = await _opportunities.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        var previousStage = existing?.Stage ?? "Nuevo";

        var update = Builders<PipelineOpportunityDoc>.Update
            .Set(x => x.Stage, nextStage)
            .Set(x => x.DaysInStage, 0);

        await _opportunities.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);

        await _stageHistory.InsertOneAsync(new PipelineStageHistoryDoc
        {
            Id = $"history-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            OpportunityId = id,
            From = previousStage,
            To = nextStage,
            Actor = actor,
            At = "Ahora",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> ReassignAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var id = GetString(payload, "id") ?? GetString(payload, "opportunityId");
        var owner = GetString(payload, "owner");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(owner))
            return false;

        var update = Builders<PipelineOpportunityDoc>.Update.Set(x => x.Owner, owner);
        await _opportunities.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<bool> CloseAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var id = GetString(payload, "id") ?? GetString(payload, "opportunityId");
        if (string.IsNullOrWhiteSpace(id)) return false;

        var outcome = GetString(payload, "outcome") ?? "won";
        var closeDate = GetString(payload, "closeDate") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var closeReason = GetString(payload, "reason");
        var finalValue = GetDecimal(payload, "finalValue");

        var existing = await _opportunities.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        var previousStage = existing?.Stage ?? "Operación";

        var update = Builders<PipelineOpportunityDoc>.Update
            .Set(x => x.Stage, "Cerrada")
            .Set(x => x.Outcome, outcome)
            .Set(x => x.ClosedAt, closeDate)
            .Set(x => x.FinalValue, finalValue)
            .Set(x => x.CloseReason, closeReason);

        await _opportunities.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);

        await _stageHistory.InsertOneAsync(new PipelineStageHistoryDoc
        {
            Id = $"history-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            OpportunityId = id,
            From = previousStage,
            To = "Cerrada",
            Actor = existing?.Owner ?? "Martín Quiroga",
            At = "Ahora",
            Reason = closeReason ?? (outcome == "won" ? "Cierre ganado" : "Cierre perdido"),
            Outcome = outcome,
            CloseDate = closeDate,
            FinalValue = finalValue,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken);

        return true;
    }

    public async Task<PipelineProposalDoc> CreateProposalAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var opportunityId = GetString(payload, "opportunityId") ?? "opp-2";
        var existingCount = await _proposals.CountDocumentsAsync(x => x.OpportunityId == opportunityId, cancellationToken: cancellationToken);
        var doc = new PipelineProposalDoc
        {
            Id = GetString(payload, "id") ?? $"proposal-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            OpportunityId = opportunityId,
            Sequence = (int)existingCount + 1,
            Kind = GetString(payload, "kind") ?? "PROPUESTA",
            ProposedBy = GetString(payload, "proposedBy") ?? "Comprador",
            ProposedTo = GetString(payload, "proposedTo") ?? "Propietario",
            Amount = GetDecimal(payload, "amount"),
            Currency = GetString(payload, "currency") ?? "USD",
            Validity = GetString(payload, "validity"),
            Conditions = GetString(payload, "conditions") ?? "",
            Outcome = "pending",
            Reason = "",
            CreatedAt = "Ahora"
        };
        await _proposals.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return doc;
    }

    public async Task<bool> RespondProposalAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var id = GetString(payload, "id");
        var outcome = GetString(payload, "outcome") ?? "accepted";
        var reason = GetString(payload, "reason") ?? "";
        if (string.IsNullOrWhiteSpace(id)) return false;

        var update = Builders<PipelineProposalDoc>.Update
            .Set(x => x.Outcome, outcome)
            .Set(x => x.Reason, reason)
            .Set(x => x.RespondedAt, "Ahora");
        await _proposals.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
        return true;
    }

    public async Task<PipelineReservationDoc> CreateReservationAsync(JsonElement payload, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        var doc = new PipelineReservationDoc
        {
            Id = GetString(payload, "id") ?? $"res-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            OpportunityId = GetString(payload, "opportunityId") ?? "opp-1",
            PropertyTitle = GetString(payload, "propertyTitle") ?? "Inmueble reservado",
            Deposit = GetDecimal(payload, "deposit"),
            Currency = GetString(payload, "currency") ?? "USD",
            Status = GetString(payload, "status") ?? "Activa",
            Conditions = GetString(payload, "conditions")
        };
        await _reservations.ReplaceOneAsync(x => x.Id == doc.Id, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        return doc;
    }

    public async Task<PipelineRelationshipDoc?> AddRelationshipAsync(string contactId, string companyId, string? relationshipType = "CONTACT_OF", CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(contactId) || string.IsNullOrWhiteSpace(companyId))
            return null;

        var existing = await _relationships.Find(x => x.FromPartyId == contactId && x.ToPartyId == companyId).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return existing;

        var doc = new PipelineRelationshipDoc
        {
            Id = $"rel-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            FromPartyId = contactId,
            ToPartyId = companyId,
            RelationshipType = relationshipType ?? "CONTACT_OF",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = "Martín Quiroga"
        };
        await _relationships.InsertOneAsync(doc, cancellationToken: cancellationToken);
        return doc;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var value = prop.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        return null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var dec))
                return dec;
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var parsed))
                return parsed;
        }
        return null;
    }
}

public sealed class PipelineOpportunityDoc
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = "REQUIREMENT";
    public string SourceId { get; set; } = string.Empty;
    public string Stage { get; set; } = "Nuevo";
    public string Owner { get; set; } = "Martín Quiroga";
    public decimal Fee { get; set; }
    public string Currency { get; set; } = "USD";
    public int DaysInStage { get; set; }
    public string? Origin { get; set; }
    public string? Outcome { get; set; }
    public string? ClosedAt { get; set; }
    public decimal? FinalValue { get; set; }
    public string? CloseReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PipelineStageHistoryDoc
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string OpportunityId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string At { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Outcome { get; set; }
    public string? CloseDate { get; set; }
    public decimal? FinalValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PipelineProposalDoc
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string OpportunityId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string Kind { get; set; } = "PROPUESTA";
    public string ProposedBy { get; set; } = string.Empty;
    public string? ProposedTo { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Validity { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string Outcome { get; set; } = "pending";
    public string Reason { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = "Ahora";
    public string? RespondedAt { get; set; }
}

public sealed class PipelineReservationDoc
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string OpportunityId { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public decimal? Deposit { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Activa";
    public string? Conditions { get; set; }
}

public sealed class PipelineRelationshipDoc
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string FromPartyId { get; set; } = string.Empty;
    public string ToPartyId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = "CONTACT_OF";
    public string CreatedAt { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
