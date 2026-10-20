namespace WebApplication1.Models;

public record PageMeta(int Page, int PageSize, int TotalCount, int TotalPages);

public record PagedResponse<T>(IReadOnlyList<T> Data, PageMeta Meta);