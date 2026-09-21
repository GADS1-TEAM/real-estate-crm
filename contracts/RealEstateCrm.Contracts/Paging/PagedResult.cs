using System;
using System.Collections.Generic;

namespace RealEstateCrm.Contracts.Paging;

public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    long Total,
    bool HasNext);
