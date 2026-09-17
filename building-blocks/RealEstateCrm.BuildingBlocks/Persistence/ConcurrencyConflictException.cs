namespace RealEstateCrm.BuildingBlocks.Persistence;

/// <summary>
/// Se lanza cuando un adapter de <see cref="IRepository{TAggregate, TId}"/> con concurrencia
/// optimista (ver <c>VersionedMongoRepository</c> en <c>RealEstateCrm.BuildingBlocks.Infrastructure</c>)
/// no encuentra la versión esperada del aggregate al actualizar: otro actor lo modificó primero.
/// </summary>
/// <remarks>
/// El Api de cada servicio la mapea a <c>409 Conflict</c> con
/// <c>RealEstateCrm.Contracts.Errors.ErrorCodes.Conflict</c> en su manejador centralizado de
/// excepciones (skill <c>aspnetcore-error-and-observability</c>).
/// </remarks>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string aggregateTypeName, object id)
        : base($"Conflicto de concurrencia optimista actualizando {aggregateTypeName} '{id}': la versión esperada ya no coincide con la persistida.")
    {
        AggregateTypeName = aggregateTypeName;
        Id = id;
    }

    public string AggregateTypeName { get; }

    public object Id { get; }
}
