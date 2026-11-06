namespace Application.DTOs.Common;

/// <summary>Which page of a list to return: <c>?page=1&amp;pageSize=20</c>, at most 100 items per page.</summary>
public sealed class PageRequest
{
    public const int MaxPageSize = 100;

    /// <summary>The page number, starting at 1.</summary>
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>How many items come before this page. A method, so model binding and the API reference ignore it.</summary>
    public int Offset() => (Page - 1) * PageSize;
}
