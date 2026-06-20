namespace PartnerManagement.Web.Models;

public sealed class PartnerListPageResult
{
    public IReadOnlyList<PartnerListItem> Items { get; init; } = [];
    public bool HasMore { get; init; }
}