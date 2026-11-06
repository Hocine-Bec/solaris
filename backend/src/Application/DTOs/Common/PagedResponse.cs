namespace Application.DTOs.Common;

/// <summary>One page of a list, with the number of items in the whole list.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
