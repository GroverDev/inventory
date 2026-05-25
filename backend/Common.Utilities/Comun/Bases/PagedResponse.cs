namespace Common.Utilities;

public class PagedResponse<T> : Response<T>
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
