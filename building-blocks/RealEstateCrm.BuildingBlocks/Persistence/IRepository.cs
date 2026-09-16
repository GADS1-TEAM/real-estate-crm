namespace RealEstateCrm.BuildingBlocks.Persistence;

/// <summary>
/// Puerto de persistencia para un aggregate root propio de un servicio.
/// </summary>
/// <remarks>
/// No incluye ningún parámetro de tenant/organización (ADR-001). Cuando una query requiere
/// autorización, el servicio owner la aplica explícitamente con el actor resuelto por
/// <see cref="Authentication.IAuthenticationPort"/>, no con un filtro implícito acá.
/// </remarks>
/// <typeparam name="TAggregate">Tipo del aggregate root.</typeparam>
/// <typeparam name="TId">Tipo del identificador del aggregate.</typeparam>
public interface IRepository<TAggregate, in TId>
    where TAggregate : class
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}
