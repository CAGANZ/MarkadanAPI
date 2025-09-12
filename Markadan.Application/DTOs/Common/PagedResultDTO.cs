namespace Markadan.Application.DTOs.Common
{
    public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);
}
